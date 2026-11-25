#!/usr/bin/env python3
"""Check web/wwwroot/change-history.md: the format the home page's "What's New" parser relies on.

Usage: python3 check_change_history.py web/wwwroot

Checks every `## ` entry heading reads `## {d MMMM yyyy | MMMM yyyy} - {Title} {#id[ .whats-new]}`,
has a summary paragraph, a unique id and a date of its own (one entry per day), and that
entries run newest first; that there are at
least three `.whats-new` highlights; and that internal links (/section, /mission, /article,
/video and the top-level pages) point at something that exists. Exit code 1 on any problem.
"""
import json
import re
import sys
from datetime import datetime
from pathlib import Path

HEADING = re.compile(r"^## (?P<text>.+?)\s*\{(?P<attrs>[^}]*)\}\s*$")
LINK = re.compile(r"\]\((?P<url>/[^)\s]*)\)")
PAGES = {"/", "/missions", "/videos", "/sections", "/timeline", "/change-history"}
MIN_HIGHLIGHTS = 3


def ref(item, key="Reference"):
    """JSON keys are matched case-insensitively, as the app's deserialiser does."""
    return next((v for k, v in item.items() if k.lower() == key.lower()), None)


def parse_date(text):
    for fmt, precision in (("%d %B %Y", 3), ("%B %Y", 2)):
        try:
            return datetime.strptime(text, fmt), precision
        except ValueError:
            pass
    return None, 0


def section_refs(sections):
    for s in sections:
        yield ref(s)
        yield from section_refs(ref(s, "Children") or [])


def article_refs(root):
    refs = set()
    groups = json.loads((root / "data" / "articles.json").read_text())

    def walk(articles):
        for a in articles:
            refs.add(ref(a))
            walk(ref(a, "Children") or [])

    def walk_groups(gs):
        for g in gs:
            walk(ref(g, "Articles") or [])
            if ref(g, "Data"):
                walk(json.loads((root / ref(g, "Data").lstrip("/")).read_text()))
            walk_groups(ref(g, "Children") or [])

    walk_groups(groups)
    return refs


def main(root):
    root = Path(root)
    md = (root / "change-history.md").read_text(encoding="utf-8")
    lines = md.splitlines()
    problems, entries = [], []

    sections = set(section_refs(json.loads((root / "data" / "sections.json").read_text())))
    missions = {ref(m) for m in json.loads((root / "data" / "missions.json").read_text())}
    articles = article_refs(root)
    videos = set()
    for g in json.loads((root / "data" / "videos.json").read_text()):
        vs = ref(g, "Videos") or (json.loads((root / ref(g, "Data").lstrip("/")).read_text()) if ref(g, "Data") else [])
        videos |= {ref(v) for v in vs}

    in_comment = False
    for i, line in enumerate(lines):
        if "<!--" in line:
            in_comment = True
        if in_comment:
            in_comment = "-->" not in line
            continue
        if not line.startswith("## "):
            continue
        m = HEADING.match(line)
        where = f"line {i + 1}"
        if not m:
            problems.append(f"{where}: heading has no {{#id}}: {line}")
            continue
        attrs = m["attrs"].split()
        ids = [a[1:] for a in attrs if a.startswith("#")]
        text = m["text"]
        if " - " not in text:
            problems.append(f"{where}: heading should be '{{date}} - {{title}}': {text}")
            continue
        date_text, title = text.split(" - ", 1)
        date, precision = parse_date(date_text)
        if not date:
            problems.append(f"{where}: date '{date_text}' isn't 'd MMMM yyyy' or 'MMMM yyyy'")
        if not ids:
            problems.append(f"{where}: no #id in {{{m['attrs']}}}")
        elif date and not ids[0].startswith(date.strftime("%Y-%m")):
            problems.append(f"{where}: id '{ids[0]}' should start with the date ({date.strftime('%Y-%m')}...)")

        # Summary: the first non-blank line after the heading must be paragraph text.
        nxt = next((l for l in lines[i + 1:] if l.strip()), "")
        if not nxt or nxt.startswith("#"):
            problems.append(f"{where}: '{title}' has no summary paragraph")

        entries.append({"where": where, "id": ids[0] if ids else None, "title": title, "date_text": date_text,
                        "date": date, "precision": precision, "highlight": ".whats-new" in attrs})

    days = {}
    for e in entries:
        if e["date_text"] in days:
            problems.append(f"{e['where']}: second entry for {e['date_text']} - merge it into "
                            f"'{days[e['date_text']]}' (one entry per day)")
        days.setdefault(e["date_text"], e["title"])

    seen = set()
    for e in entries:
        if e["id"] in seen:
            problems.append(f"{e['where']}: duplicate id '{e['id']}'")
        seen.add(e["id"])

    dated = [e for e in entries if e["date"]]
    for newer, older in zip(dated, dated[1:]):
        p = min(newer["precision"], older["precision"])
        key = (lambda d: (d.year, d.month, d.day)[:p])
        if key(newer["date"]) < key(older["date"]):
            problems.append(f"{older['where']}: '{older['title']}' is newer than the entry above it - keep newest first")

    highlights = [e["title"] for e in entries if e["highlight"]]
    if len(highlights) < MIN_HIGHLIGHTS:
        problems.append(f"only {len(highlights)} .whats-new entries; the home page needs {MIN_HIGHLIGHTS}")

    for i, line in enumerate(lines):
        for m in LINK.finditer(line):
            url = m["url"].split("#")[0]
            parts = url.strip("/").split("/")
            ok = (url in PAGES
                  or (parts[0] == "section" and len(parts) == 2 and parts[1] in sections)
                  or (parts[0] == "mission" and len(parts) == 2 and parts[1] in missions)
                  or (parts[0] == "video" and len(parts) == 2 and parts[1] in videos)
                  or (parts[0] == "article" and len(parts) == 3 and parts[1] in sections and parts[2] in articles)
                  or (parts[0] in ("content", "images") and (root / url.lstrip("/")).exists()))
            if not ok:
                problems.append(f"line {i + 1}: link {m['url']} doesn't point at anything in the archive")

    for p in problems:
        print(p)
    print(f"{len(entries)} entries, {len(highlights)} highlights "
          f"(home page shows: {' | '.join(highlights[:MIN_HIGHLIGHTS]) or 'none'}); {len(problems)} problem(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "web/wwwroot"))
