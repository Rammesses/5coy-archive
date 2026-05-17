#!/usr/bin/env python3
"""
Generate the legacy→canonical rename map that drives the corpus cleanup (#51).

Reads every PdfUrl and ContentUrl across web/wwwroot/data/articles/*.json and
articles.json, applies the canonical-path rules defined in
docs/specs/51-corpus-cleanup.md, and writes scripts/rename-map.json.

Optional scripts/rename-overrides.json provides per-file canonical overrides
for cases the deterministic rules get wrong.

Run once during the #51 implementation. The map is checked in so it is the
source of truth for both scripts/migrate-media-paths.sh and the worker's
build-redirects.mjs.
"""

import json
import re
import sys
import urllib.parse
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
ARTICLES_DIR = REPO_ROOT / "web" / "wwwroot" / "data" / "articles"
ARTICLES_INDEX = REPO_ROOT / "web" / "wwwroot" / "data" / "articles.json"
OUTPUT_PATH = REPO_ROOT / "scripts" / "rename-map.json"
OVERRIDES_PATH = REPO_ROOT / "scripts" / "rename-overrides.json"


# Media-path remapping: legacy folder prefix → canonical area prefix.
# Order matters: longer prefixes must come first so prefix-match selects the
# most-specific rule.
MEDIA_FOLDER_REMAP = [
    ("/media/In-Character/InTheLineOfFire/", "/media/in-character/in-the-line-of-fire/"),
    ("/media/In-Character/", "/media/in-character/"),
    ("/media/Marine's Handbook/", "/media/in-character/marines-handbook/"),
    ("/media/Mexal's Letters/", "/media/in-character/mexals-letters/"),
    ("/media/Mission Reports/", "/media/in-character/mission-reports/"),
    ("/media/Out-Of-Character/", "/media/out-of-character/"),
    # /media/Miscellanea/ items split based on what they actually are.
    # Default rule: anything currently at /media/Miscellanea/ goes to
    # /media/out-of-character/scenarios/ unless an override redirects it
    # to /media/out-of-character/miscellanea/. Only prop-designs is
    # genuinely miscellanea.
    ("/media/Miscellanea/", "/media/out-of-character/scenarios/"),
]

# Root-level legacy PDFs (no folder), keyed by source filename → target area.
# These are the PDFs at /media/<Filename>.pdf that need an area assignment.
ROOT_PDF_AREA = {
    # out-of-character core docs
    "a-brief-guide-to-history": "/media/out-of-character/",
    "companies-and-corporations": "/media/out-of-character/",
    "groups-and-organisations": "/media/out-of-character/",
    "planetary-database": "/media/out-of-character/",
    "writers-guide": "/media/out-of-character/",
    # scenarios at root
    "drive-wars": "/media/out-of-character/scenarios/",
    "drive-wars-ii": "/media/out-of-character/scenarios/",
}


# Content-path remapping: legacy directory → canonical directory.
CONTENT_FOLDER_REMAP = [
    ("/content/mission-reports/", "/content/in-character/mission-reports/"),
    ("/content/scenarios/", "/content/out-of-character/scenarios/"),
]


def normalise_path_segment(segment: str) -> str:
    """Lowercase ASCII, kebab-case. Strips apostrophes, replaces spaces/+/special with -.
    Splits on the final '.' so the extension is preserved untouched and trailing
    dashes before the extension are removed."""
    if segment == "":
        return ""
    s = urllib.parse.unquote_plus(segment)
    s = s.lower()
    s = s.replace("'", "").replace('"', "")
    s = s.replace("&", "and")

    # Separate stem and extension so the dot isn't normalised into a dash.
    if "." in s:
        stem, _, ext = s.rpartition(".")
    else:
        stem, ext = s, ""

    stem = stem.replace("+", "-").replace(" ", "-")
    stem = re.sub(r"[^a-z0-9-]", "-", stem)
    stem = re.sub(r"-+", "-", stem).strip("-")

    return f"{stem}.{ext}" if ext else stem


CANONICAL_MEDIA_PREFIXES = (
    "/media/in-character/",
    "/media/out-of-character/",
)


def canonicalise_media_url(legacy: str) -> str | None:
    """Apply folder remap + per-segment normalisation to a legacy media URL.
    Idempotent: returns None if the input is already at a canonical prefix."""
    if not legacy or not legacy.startswith("/media/"):
        return None

    decoded = urllib.parse.unquote_plus(legacy)

    # Idempotency: already canonical, nothing to do.
    if any(decoded.startswith(p) for p in CANONICAL_MEDIA_PREFIXES):
        return None

    # Try folder-prefix match first.
    new_prefix = None
    old_prefix = None
    for legacy_prefix, canonical_prefix in MEDIA_FOLDER_REMAP:
        if decoded.startswith(legacy_prefix):
            new_prefix = canonical_prefix
            old_prefix = legacy_prefix
            break

    if new_prefix is None:
        # Root-level PDF at /media/<Filename>.pdf
        remainder = decoded[len("/media/"):]
        stem_normalised = normalise_path_segment(Path(remainder).stem)
        # Try to find an area for this stem.
        if stem_normalised in ROOT_PDF_AREA:
            new_prefix = ROOT_PDF_AREA[stem_normalised]
            old_prefix = "/media/"
        else:
            # Unknown root PDF — emit a warning entry; reviewer overrides if needed.
            new_prefix = "/media/_UNCLASSIFIED/"
            old_prefix = "/media/"

    # Re-normalise the remainder after the matched prefix.
    remainder = decoded[len(old_prefix):]
    parts = remainder.split("/")
    norm_parts = [normalise_path_segment(p) for p in parts]
    canonical = new_prefix + "/".join(norm_parts)
    # Collapse any accidental double slashes.
    canonical = re.sub(r"/+", "/", canonical)
    return canonical


def canonicalise_content_url(legacy: str) -> str | None:
    """Apply content-folder remap. Existing content filenames are preserved
    (already kebab-case-ish); only their parent directory may shift.
    Returns None if no change required."""
    if not legacy or not legacy.startswith("/content/"):
        return None
    # Fix mision-vortex typo (FR-10).
    fixed = legacy.replace("mision-vortex", "mission-vortex")
    for legacy_prefix, canonical_prefix in CONTENT_FOLDER_REMAP:
        if fixed.startswith(legacy_prefix):
            new = canonical_prefix + fixed[len(legacy_prefix):]
            return new if new != legacy else None
    return fixed if fixed != legacy else None


def collect_urls() -> tuple[set[str], set[str]]:
    """Walk articles.json + articles/*.json. Return (pdf_urls, content_urls)."""
    pdf_urls: set[str] = set()
    content_urls: set[str] = set()

    def walk(node):
        if isinstance(node, list):
            for x in node:
                walk(x)
        elif isinstance(node, dict):
            # Normalise both 'Reference' and 'reference' field-key cases for URL fields.
            for key in ("PdfUrl", "pdfUrl"):
                if key in node and isinstance(node[key], str) and node[key]:
                    pdf_urls.add(node[key])
            for key in ("ContentUrl", "contentUrl"):
                if key in node and isinstance(node[key], str) and node[key]:
                    content_urls.add(node[key])
            for key in ("Articles", "Children"):
                if key in node:
                    walk(node[key])

    for data_file in [ARTICLES_INDEX] + sorted(ARTICLES_DIR.glob("*.json")):
        with data_file.open() as f:
            walk(json.load(f))

    return pdf_urls, content_urls


def apply_overrides(moves: list[dict]) -> list[dict]:
    if not OVERRIDES_PATH.exists():
        return moves
    with OVERRIDES_PATH.open() as f:
        ov = json.load(f)
    overrides = ov.get("overrides", {})
    extras = ov.get("extras", [])

    result = []
    for move in moves:
        legacy = move["legacy"]
        if legacy in overrides:
            new = dict(move)
            new["canonical"] = overrides[legacy]
            new["override"] = True
            result.append(new)
        else:
            result.append(move)

    # Append extras as new moves (not collected from the data files but
    # required by spec, e.g., FR-06 message files moving into mission-reports/).
    seen_legacy = {m["legacy"] for m in result}
    for extra in extras:
        if extra["legacy"] in seen_legacy:
            continue
        result.append({
            "legacy": extra["legacy"],
            "canonical": extra["canonical"],
            "kind": extra.get("kind", "extra"),
            "extra": True,
        })
    return result


def main() -> int:
    pdf_urls, content_urls = collect_urls()

    pdf_moves = []
    already_canonical_count = 0
    for legacy in sorted(pdf_urls):
        canonical = canonicalise_media_url(legacy)
        if canonical is None:
            # Already at a canonical prefix or unmappable; either way, no move needed.
            already_canonical_count += 1
            continue
        if canonical == legacy:
            continue
        pdf_moves.append({"legacy": legacy, "canonical": canonical, "kind": "pdf"})
    if already_canonical_count:
        print(f"({already_canonical_count} PDF URLs already at canonical paths — no move needed)",
              file=sys.stderr)

    content_moves = []
    for legacy in sorted(content_urls):
        canonical = canonicalise_content_url(legacy)
        if canonical is None or canonical == legacy:
            continue
        content_moves.append({"legacy": legacy, "canonical": canonical, "kind": "content"})

    all_moves = pdf_moves + content_moves
    all_moves = apply_overrides(all_moves)

    output = {
        "version": 1,
        "generated_at": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "source": "docs/specs/51-corpus-cleanup.md §7 + web/wwwroot/data/articles/*.json crawl",
        "counts": {
            "pdf_moves": len([m for m in all_moves if m["kind"] == "pdf"]),
            "content_moves": len([m for m in all_moves if m["kind"] == "content"]),
            "total": len(all_moves),
        },
        "moves": all_moves,
    }

    with OUTPUT_PATH.open("w") as f:
        json.dump(output, f, indent=2)
        f.write("\n")

    print(f"Wrote {OUTPUT_PATH.relative_to(REPO_ROOT)}: "
          f"{output['counts']['pdf_moves']} PDF moves, "
          f"{output['counts']['content_moves']} content moves.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
