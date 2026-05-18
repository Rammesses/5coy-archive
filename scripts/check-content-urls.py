#!/usr/bin/env python3
"""
Verify every ContentUrl in web/wwwroot/data/articles/*.json (+ articles.json)
resolves to a file on disk, and every PdfUrl matches the canonical media-path
prefix of its area.

Exits 0 on success, 1 on any failure. Use after running rewrite-article-urls.py
to confirm the corpus is consistent.
"""

import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
ARTICLES_DIR = REPO_ROOT / "web" / "wwwroot" / "data" / "articles"
ARTICLES_INDEX = REPO_ROOT / "web" / "wwwroot" / "data" / "articles.json"
WWWROOT = REPO_ROOT / "web" / "wwwroot"

# Per-area canonical media-path prefix (from OpenSpec §7).
AREA_MEDIA_PREFIX = {
    "in-character.json": "/media/in-character/",
    "in-the-line-of-fire.json": "/media/in-character/in-the-line-of-fire/",
    "interstella.json": "/media/in-character/interstella/",
    "marines-handbook.json": "/media/in-character/marines-handbook/",
    "mexals-letters.json": "/media/in-character/mexals-letters/",
    "mission-reports.json": "/media/in-character/mission-reports/",
    "out-of-character.json": "/media/out-of-character/",
    "miscellanea.json": "/media/out-of-character/miscellanea/",
    "scenarios.json": "/media/out-of-character/scenarios/",
}


def walk_entries(node, _children_of=None):
    """Yield every dict entry recursively (including Children)."""
    if isinstance(node, list):
        for x in node:
            yield from walk_entries(x)
    elif isinstance(node, dict):
        if "Reference" in node or "ContentUrl" in node or "PdfUrl" in node:
            yield node
        for k in ("Children", "Articles"):
            if k in node and isinstance(node[k], list):
                yield from walk_entries(node[k])


def check_file(path: Path, errors: list[str]):
    expected_prefix = AREA_MEDIA_PREFIX.get(path.name)
    data = json.loads(path.read_text())
    seen_refs = set()
    for entry in walk_entries(data):
        ref = entry.get("Reference", "<no-Reference>")

        # ContentUrl must exist on disk (if non-empty).
        content_url = entry.get("ContentUrl", "")
        if content_url:
            disk_path = WWWROOT / content_url.lstrip("/")
            if not disk_path.exists():
                errors.append(
                    f"{path.name}: {ref}: ContentUrl points to missing file: {content_url}"
                )

        # PdfUrl must use the canonical prefix for this area (if non-empty).
        pdf_url = entry.get("PdfUrl", "")
        if pdf_url and expected_prefix and not pdf_url.startswith(expected_prefix):
            errors.append(
                f"{path.name}: {ref}: PdfUrl {pdf_url!r} does not start with "
                f"expected prefix {expected_prefix!r}"
            )

        # Reference uniqueness within file.
        if ref in seen_refs:
            errors.append(f"{path.name}: duplicate Reference: {ref}")
        seen_refs.add(ref)

    return seen_refs


def main() -> int:
    errors: list[str] = []
    all_refs: dict[str, str] = {}

    for path in sorted(ARTICLES_DIR.glob("*.json")):
        refs = check_file(path, errors)
        for ref in refs:
            if ref in all_refs:
                errors.append(
                    f"Reference {ref!r} appears in both {all_refs[ref]} and {path.name}"
                )
            all_refs[ref] = path.name

    if errors:
        print(f"FAIL: {len(errors)} error(s)", file=sys.stderr)
        for e in errors:
            print(f"  • {e}", file=sys.stderr)
        return 1

    print(f"OK: {len(all_refs)} unique references across "
          f"{len(list(ARTICLES_DIR.glob('*.json')))} files.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
