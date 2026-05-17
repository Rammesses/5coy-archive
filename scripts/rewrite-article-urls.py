#!/usr/bin/env python3
"""
Rewrite web/wwwroot/data/articles/*.json (and articles.json) for the corpus
cleanup (#51). Applies the rename map from scripts/rename-map.json plus the
structural changes specified in .kiro/specs/corpus-cleanup/{requirements,design}.md.

Operations:
  1. Apply rename map to every ContentUrl and PdfUrl across all data files.
  2. Normalise field keys to canonical casing (Reference, Title, ContentUrl,
     PdfUrl, MissionRef, Children, Articles, Data).
  3. Fix Title typo: "Grant\"s World" → "Grant's World".
  4. Fix Reference + ContentUrl for the mision→mission-vortex typo.
  5. Remove the 3 duplicate entries from in-character.json (kept only in
     mission-reports.json as Children).
  6. Split articles.json's inline 'miscellanea' Articles array into
     articles/miscellanea.json and replace with a Data reference.
  7. Add stub entries for the 6 orphan content files per OpenSpec §10.3.

Single-use. Run once during #51 implementation. Outputs replace the inputs in-place.
"""

import json
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
ARTICLES_DIR = REPO_ROOT / "web" / "wwwroot" / "data" / "articles"
ARTICLES_INDEX = REPO_ROOT / "web" / "wwwroot" / "data" / "articles.json"
CONTENT_ROOT = REPO_ROOT / "web" / "wwwroot" / "content"
RENAME_MAP_PATH = REPO_ROOT / "scripts" / "rename-map.json"

# Canonical JSON field-key casing.
KEY_CANONICAL = {
    "reference": "Reference",
    "title": "Title",
    "contenturl": "ContentUrl",
    "pdfurl": "PdfUrl",
    "missionref": "MissionRef",
    "children": "Children",
    "articles": "Articles",
    "data": "Data",
}

# Duplicate entries to drop from in-character.json (kept in mission-reports.json).
DUPLICATES_TO_DROP_FROM_IN_CHARACTER = {
    "2492-10_mission-procyon-intel",
    "2496-02_mission-nova_comms-001",
    "2496-02_mission-intruder_comms-002",
}

# Orphan content files: (relative path under web/wwwroot/, target area file, proposed Reference).
ORPHAN_STUBS = [
    ("/content/in-character/general-orders_2602-05-16.md",
     "in-character.json", "general-orders_2602-05-16"),
    ("/content/out-of-character/cmc_newsletter_1999-02.md",
     "out-of-character.json", "cmc-newsletter_1999-02"),
    ("/content/out-of-character/honours_1995-08-23.md",
     "out-of-character.json", "honours_1995-08-23"),
    ("/content/out-of-character/interim-history_2496-to-2501.md",
     "out-of-character.json", "interim-history_2496-to-2501"),
    ("/content/out-of-character/mission-briefing_operation-hammerhead_2501-10.md",
     "out-of-character.json", "mission-briefing_operation-hammerhead_2501-10"),
    ("/content/out-of-character/mission-notice_operation-dawn_1999-01.md",
     "out-of-character.json", "mission-notice_operation-dawn_1999-01"),
]


def load_rename_map() -> dict:
    """Returns a {legacy_url: canonical_url} dict for both PDFs and content."""
    data = json.loads(RENAME_MAP_PATH.read_text())
    return {m["legacy"]: m["canonical"] for m in data["moves"]}


def canonical_keys(node):
    """Recursively rewrite dict keys to canonical casing."""
    if isinstance(node, list):
        return [canonical_keys(x) for x in node]
    if isinstance(node, dict):
        out = {}
        for k, v in node.items():
            new_k = KEY_CANONICAL.get(k.lower(), k)
            out[new_k] = canonical_keys(v)
        return out
    return node


def apply_urls(node, rename_map):
    """Recursively rewrite ContentUrl and PdfUrl using the rename map."""
    if isinstance(node, list):
        return [apply_urls(x, rename_map) for x in node]
    if isinstance(node, dict):
        out = {}
        for k, v in node.items():
            if k in ("ContentUrl", "PdfUrl") and isinstance(v, str) and v in rename_map:
                out[k] = rename_map[v]
            elif isinstance(v, (list, dict)):
                out[k] = apply_urls(v, rename_map)
            else:
                out[k] = v
        return out
    return node


def fix_grants_world_title(node):
    """The mexals-letters_2495-01_grants-world entry has Title using a stray
    double-quote where an apostrophe should be."""
    if isinstance(node, list):
        return [fix_grants_world_title(x) for x in node]
    if isinstance(node, dict):
        out = dict(node)
        if out.get("Reference") == "mexals-letters_2495-01_grants-world":
            out["Title"] = "2495-01 - Grant's World"
        for k, v in out.items():
            if isinstance(v, (list, dict)):
                out[k] = fix_grants_world_title(v)
        return out
    return node


def fix_planetary_database(node):
    """The planetary-Database entry uses non-canonical Reference casing AND
    references a markdown file that doesn't exist (PDF-only entry)."""
    if isinstance(node, list):
        return [fix_planetary_database(x) for x in node]
    if isinstance(node, dict):
        out = dict(node)
        if out.get("Reference") in ("planetary-Database", "planetary-database"):
            out["Reference"] = "planetary-database"
            # ContentUrl points to a file that doesn't exist; this is a
            # PDF-only entry. Clear it so consumers don't 404.
            if "ContentUrl" in out and out["ContentUrl"] == "/content/out-of-character/planetary-database.md":
                out["ContentUrl"] = ""
        for k, v in out.items():
            if isinstance(v, (list, dict)):
                out[k] = fix_planetary_database(v)
        return out
    return node


def fix_mision_vortex(node):
    """The briefing-notes_mision-vortex Reference + ContentUrl should be mission-vortex."""
    if isinstance(node, list):
        return [fix_mision_vortex(x) for x in node]
    if isinstance(node, dict):
        out = dict(node)
        if out.get("Reference") == "briefing-notes_mision-vortex":
            out["Reference"] = "briefing-notes_mission-vortex"
            if "ContentUrl" in out:
                out["ContentUrl"] = out["ContentUrl"].replace("mision-vortex", "mission-vortex")
        for k, v in out.items():
            if isinstance(v, (list, dict)):
                out[k] = fix_mision_vortex(v)
        return out
    return node


def drop_duplicates_in_character(entries):
    """Drop the three top-level duplicates from in-character.json's array."""
    return [e for e in entries
            if not (isinstance(e, dict)
                    and e.get("Reference") in DUPLICATES_TO_DROP_FROM_IN_CHARACTER)]


def extract_h1(content_url: str) -> str | None:
    """Read the H1 (first '# ' line) from a content file. Return None if not found."""
    rel = content_url.lstrip("/")
    candidate = REPO_ROOT / "web" / "wwwroot" / rel
    if not candidate.exists():
        return None
    for line in candidate.read_text(errors="replace").splitlines()[:30]:
        m = re.match(r"#\s+(.+?)\s*$", line)
        if m:
            return m.group(1).strip()
    return None


def make_orphan_stub(content_url: str, reference: str) -> dict:
    title = extract_h1(content_url)
    if not title:
        # Fallback: derive from filename.
        stem = Path(content_url).stem
        title = stem.replace("_", " ").replace("-", " ").title()
    return {
        "Reference": reference,
        "Title": title,
        "ContentUrl": content_url,
        "PdfUrl": "",
    }


def write_json(path: Path, data):
    text = json.dumps(data, indent=2, ensure_ascii=False)
    path.write_text(text + "\n")


def rewrite_index_file(path: Path, rename_map: dict, is_in_character: bool = False):
    data = json.loads(path.read_text())
    data = canonical_keys(data)
    data = apply_urls(data, rename_map)
    data = fix_grants_world_title(data)
    data = fix_mision_vortex(data)
    data = fix_planetary_database(data)
    if is_in_character and isinstance(data, list):
        data = drop_duplicates_in_character(data)
    return data


def split_out_miscellanea(articles_index_data):
    """Find the miscellanea row in articles.json, extract its inline Articles
    into a new file, and replace with a Data reference."""
    miscellanea_articles = None
    new_index = []
    for row in articles_index_data:
        if not isinstance(row, dict):
            new_index.append(row)
            continue
        ref = row.get("Reference") or row.get("reference")
        if ref == "miscellanea" and "Articles" in row:
            miscellanea_articles = row["Articles"]
            new_index.append({
                "Reference": "miscellanea",
                "Data": "/data/articles/miscellanea.json",
            })
        else:
            new_index.append(row)
    return new_index, miscellanea_articles


def add_orphan_stubs(per_file_data):
    """Mutate per_file_data (dict of {filename: list}) to append orphan stubs.
    Idempotent: skips any orphan whose Reference is already present in the target file."""
    for content_url, target_file, reference in ORPHAN_STUBS:
        if target_file not in per_file_data:
            continue
        existing_refs = {
            e.get("Reference") for e in per_file_data[target_file]
            if isinstance(e, dict)
        }
        if reference in existing_refs:
            continue
        stub = make_orphan_stub(content_url, reference)
        per_file_data[target_file].append(stub)


def main() -> int:
    rename_map = load_rename_map()

    # Pre-flight: also apply rename map to content URLs that move under in-character
    # tree (mission-reports → in-character/mission-reports, etc.) These were
    # written into the rename map by generate-rename-map.py.
    print(f"Loaded rename map: {len(rename_map)} entries")

    # Rewrite each area's data file.
    per_file_data: dict[str, list] = {}
    for path in sorted(ARTICLES_DIR.glob("*.json")):
        is_in_character = path.name == "in-character.json"
        per_file_data[path.name] = rewrite_index_file(path, rename_map, is_in_character)

    # Handle the orphan stubs.
    add_orphan_stubs(per_file_data)

    # Rewrite articles.json (the top-level index).
    index_data = json.loads(ARTICLES_INDEX.read_text())
    index_data = canonical_keys(index_data)
    index_data, miscellanea_articles = split_out_miscellanea(index_data)

    # Write miscellanea.json if we extracted articles.
    miscellanea_path = ARTICLES_DIR / "miscellanea.json"
    if miscellanea_articles is not None:
        # Apply rename map and key normalisation to the extracted entries.
        mis = canonical_keys(miscellanea_articles)
        mis = apply_urls(mis, rename_map)
        write_json(miscellanea_path, mis)
        print(f"Wrote {miscellanea_path.relative_to(REPO_ROOT)}: "
              f"{len(mis)} entries")
    elif miscellanea_path.exists():
        # Idempotent run: miscellanea.json already exists, leave it.
        pass

    # Persist all data files.
    for filename, data in per_file_data.items():
        write_json(ARTICLES_DIR / filename, data)
        print(f"Wrote {(ARTICLES_DIR / filename).relative_to(REPO_ROOT)}: "
              f"{len(data)} top-level entries")

    write_json(ARTICLES_INDEX, index_data)
    print(f"Wrote {ARTICLES_INDEX.relative_to(REPO_ROOT)}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
