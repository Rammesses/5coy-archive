#!/usr/bin/env python3
"""Validate cross-references between the 5coy-archive JSON data and content files.

Usage: python3 check_crossrefs.py [path/to/web/wwwroot]
Exits 1 if any ERROR is found; WARN lines are for human judgement.
"""
import collections, datetime, glob, json, os, re, sys

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
    for rel in a.get("Related") or []:
        if rel not in mref_set and rel not in art_refs:
            report("ERROR", f"{where}: Related '{rel}' is neither an article nor a mission Reference")
        if rel == mr:
            report("WARN", f"{where}: Related '{rel}' duplicates its MissionRef")
    if mr and d > 0 and not any(x[1] == r and x[3] == 0 for x in arts):
        report("WARN", f"{where}: child MissionRef is ignored by GetItemsByMissionRef (top-level only)")

dups = [r for r, n in collections.Counter(r for _, r, _, _ in arts).items() if n > 1]
for r in dups:
    locs = [(g, d) for g, x, _, d in arts if x == r]
    report("WARN", f"duplicate article Reference '{r}' at {locs}")

# videos (data/videos.json groups, inline Videos or a Data file holding Video[])
videos = []
if os.path.exists("data/videos.json"):
    for g in load("data/videos.json"):
        vs = list(g.get("Videos") or [])
        if g.get("Data"):
            if not os.path.exists("." + g["Data"]): report("ERROR", f"video group data file missing: {g['Data']}")
            else: vs += load(g["Data"])
        videos += [(g.get("Reference"), v) for v in vs]
video_refs = [v.get("Reference") for _, v in videos]
for r, n in collections.Counter(video_refs).items():
    if n > 1: report("ERROR", f"duplicate video Reference '{r}'")
for g, v in videos:
    where = f"[video {g}] {v.get('Reference')}"
    if not v.get("Reference"): report("ERROR", f"[video {g}] video without Reference: {v.get('Title')}")
    if not v.get("YouTubeVideoId"): report("ERROR", f"{where}: no YouTubeVideoId")
    cu = v.get("ContentUrl")
    if cu:
        used_content.add(cu)
        if not os.path.exists("." + cu): report("ERROR", f"{where}: ContentUrl not found {cu}")
    mr = v.get("MissionRef")
    if mr and mr not in mref_set: report("ERROR", f"{where}: MissionRef '{mr}' not in missions.json")
    for rel in v.get("Related") or []:
        if rel not in mref_set and rel not in art_refs:
            report("ERROR", f"{where}: Related '{rel}' is neither an article nor a mission Reference")
        if rel == mr: report("WARN", f"{where}: Related '{rel}' duplicates its MissionRef")

# mission Related lists: every entry must resolve, and every /mission/ or /article/ link in a
# mission summary must be recorded (in Related, or by the article's own MissionRef)
art_missionrefs = collections.defaultdict(set)
for _, r, a, _ in arts:
    art_missionrefs[r].add(a.get("MissionRef") or "")
summary_link_re = re.compile(r"\]\(/(mission|article)/([^)\s#?]*)\)")
for m in missions:
    mref = m.get("Reference")
    related = m.get("Related") or []
    for rel in related:
        if rel not in mref_set and rel not in art_refs:
            report("ERROR", f"mission {mref}: Related '{rel}' is neither an article nor a mission Reference")
        elif rel in art_refs and mref in art_missionrefs[rel]:
            report("WARN", f"mission {mref}: Related '{rel}' already has this mission as its MissionRef")
    cu = m.get("ContentUrl")
    if not cu or not os.path.exists("." + cu):
        continue
    with open("." + cu, encoding="utf-8") as fh:
        for kind, path in summary_link_re.findall(fh.read()):
            ref = path.strip("/").split("/")[-1]
            if ref == mref or ref in related:
                continue
            if kind == "article" and mref in art_missionrefs[ref]:
                continue
            report("ERROR", f"mission {mref}: summary links {kind} '{ref}' but it isn't in the mission's Related (or the article's MissionRef)")

# mission summaries (missions.json ContentUrl) count as references too
used_content.update(m["ContentUrl"] for m in missions if m.get("ContentUrl"))

# 3. content files not referenced anywhere
for f in sorted(glob.glob("content/**/*.md", recursive=True)):
    u = "/" + f
    if u in used_content or u in section_content or "template" in f: continue
    if u in ("/content/missions.md", "/content/videos.md"): continue  # Missions.razor / Videos.razor headers
    size = os.path.getsize(f)
    report("WARN" if size == 0 else "ERROR", f"unreferenced content {u}" + (" (empty file)" if size == 0 else ""))

# 3b. internal links in markdown: /mission/<ref>, /article/<section>/<ref>, /section/<ref>
art_keys = {(g, r) for g, r, _, _ in arts}
link_re = re.compile(r"\]\((/(?:mission|article|section|video)/[^)\s#?]*)")
video_ref_set = set(video_refs)
for f in sorted(glob.glob("content/**/*.md", recursive=True)):
    with open(f, encoding="utf-8") as fh:
        for u in link_re.findall(fh.read()):
            parts = u.strip("/").split("/")
            ok = ((parts[0] == "mission" and len(parts) == 2 and parts[1] in mref_set) or
                  (parts[0] == "section" and len(parts) == 2 and parts[1] in section_refs) or
                  (parts[0] == "video" and len(parts) == 2 and parts[1] in video_ref_set) or
                  (parts[0] == "article" and len(parts) == 3 and tuple(parts[1:]) in art_keys))
            if not ok: report("ERROR", f"/{f}: broken link {u}")

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
    try:
        sd, ed = datetime.date.fromisoformat(s), datetime.date.fromisoformat(e)
        if sd.year < 2000 and (sd.weekday() != 4 or ed.weekday() != 6 or (ed - sd).days != 2):
            report("WARN", f"mission {r or m['Name']}: {s}..{e} is not a Fri-Sun weekend")
    except ValueError:
        report("ERROR", f"mission {r or m['Name']}: unparseable dates {s}..{e}")
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
print(f"\n{len(arts)} articles, {len(missions)} missions, {len(videos)} videos, {len(personnel)} personnel, {errors} error(s)")
sys.exit(1 if errors else 0)
