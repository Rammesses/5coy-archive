#!/usr/bin/env python3
"""Validate cross-references between the 5coy-archive JSON data and content files.

Usage: python3 check_crossrefs.py [path/to/web/wwwroot]
Exits 1 if any ERROR is found; WARN lines are for human judgement.
"""
import collections, glob, json, os, sys

root = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(__file__), *[".."] * 4, "web", "wwwroot")
os.chdir(os.path.abspath(root))
errors = 0

def report(level, msg):
    global errors
    errors += level == "ERROR"
    print(f"{level:5} {msg}")

def load(path):
    p = "." + path if path.startswith("/") else path
    with open(p, encoding="utf-8") as f:
        text = f.read()
    if not text.strip():
        report("WARN", f"empty JSON file: {path}")
        return []
    return json.loads(text)

# 1. every data file parses
for f in sorted(glob.glob("data/**/*.json", recursive=True)):
    try: load(f)
    except Exception as e: report("ERROR", f"invalid JSON {f}: {e}")

sections = load("data/sections.json")
groups = load("data/articles.json")
missions = load("data/missions.json")
personnel = load("data/personnel.json")

def flat_sections(lst):
    for s in lst:
        yield s; yield from flat_sections(s.get("Children", []))
section_refs = {s["Reference"] for s in flat_sections(sections)}
section_content = {s.get("ContentUrl") for s in flat_sections(sections) if s.get("ContentUrl")}

# 2. articles
arts = []  # (group, ref, article, depth)
def walk(lst, g, d=0):
    for a in lst:
        arts.append((g, a.get("Reference") or a.get("reference"), a, d))
        walk(a.get("Children", []), g, d + 1)
for g in groups:
    if g["Reference"] not in section_refs:
        report("WARN", f"article group '{g['Reference']}' has no section in sections.json")
    walk(g.get("Articles", []), g["Reference"])
    if g.get("Data"):
        if not os.path.exists("." + g["Data"]): report("ERROR", f"group data file missing: {g['Data']}")
        else: walk(load(g["Data"]), g["Reference"])

mission_refs = [m.get("Reference") for m in missions if m.get("Reference")]
mref_set = set(mission_refs)
art_refs = {r for _, r, _, _ in arts}
used_content = set()
for g, r, a, d in arts:
    where = f"[{g}] {r}"
    if not r: report("ERROR", f"[{g}] article without Reference: {a.get('Title')}")
    cu = a.get("ContentUrl")
    if cu:
        used_content.add(cu)
        if not os.path.exists("." + cu): report("ERROR", f"{where}: ContentUrl not found {cu}")
    if not cu and not a.get("PdfUrl") and not a.get("Children"):
        report("WARN", f"{where}: no ContentUrl, PdfUrl or Children")
    mr = a.get("MissionRef")
    if mr and mr not in mref_set:
        hint = " (that is an ARTICLE reference)" if mr in art_refs else ""
        report("ERROR", f"{where}: MissionRef '{mr}' not in missions.json{hint}")
    if mr and d > 0 and not any(x[1] == r and x[3] == 0 for x in arts):
        report("WARN", f"{where}: child MissionRef is ignored by GetItemsByMissionRef (top-level only)")

dups = [r for r, n in collections.Counter(r for _, r, _, _ in arts).items() if n > 1]
for r in dups:
    locs = [(g, d) for g, x, _, d in arts if x == r]
    report("WARN", f"duplicate article Reference '{r}' at {locs}")

# 3. content files not referenced anywhere
for f in sorted(glob.glob("content/**/*.md", recursive=True)):
    u = "/" + f
    if u in used_content or u in section_content or "template" in f: continue
    if u == "/content/missions.md": continue  # Missions.razor header
    size = os.path.getsize(f)
    report("WARN" if size == 0 else "ERROR", f"unreferenced content {u}" + (" (empty file)" if size == 0 else ""))

# 4. missions
for r, n in collections.Counter(mission_refs).items():
    if n > 1: report("ERROR", f"duplicate mission Reference '{r}'")
for m in missions:
    s, e, r = m.get("StartDate", ""), m.get("EndDate", ""), m.get("Reference")
    if e < s: report("ERROR", f"mission {m['Name']} EndDate {e} before StartDate {s}")
    if s[:4] != e[:4]: report("WARN", f"mission {m['Name']} spans years {s}..{e}")
    if r and r[:2] in ("24", "25") and len(r) > 7 and r[4] == "-":
        ingame = f"{int(r[:4]) - 500:04d}-{r[5:7]}"
        if not s.startswith(ingame):
            report("WARN", f"mission ref '{r}' doesn't match StartDate {s} (expected {ingame}-..)")
    if m.get("ContentUrl") and not os.path.exists("." + m["ContentUrl"]):
        report("ERROR", f"mission {r}: ContentUrl not found {m['ContentUrl']}")

# 5. personnel
for p in personnel:
    ar = p.get("ArticleRef")
    if ar and ar not in art_refs: report("ERROR", f"personnel {p['Name']}: ArticleRef '{ar}' not found")
jackets = [(r, a) for _, r, a, d in arts if r and r.startswith("personnel-jacket_")]
linked = {p.get("ArticleRef") for p in personnel}
for r, a in jackets:
    if r not in linked: report("ERROR", f"personnel jacket article '{r}' has no personnel.json entry")

# 6. summary: related-article counts per mission
print("\nMission coverage (top-level articles with MissionRef):")
cnt = collections.Counter(a["MissionRef"] for _, _, a, d in arts if d == 0 and a.get("MissionRef"))
for m in sorted(missions, key=lambda m: m["StartDate"]):
    if m.get("Reference"): print(f"  {m['StartDate']}  {m['Reference']:40} {cnt.get(m['Reference'], 0)}")
print(f"\n{len(arts)} articles, {len(missions)} missions, {len(personnel)} personnel, {errors} error(s)")
sys.exit(1 if errors else 0)
