#!/usr/bin/env python3
"""List every source in the archive that bears on one mission.

Usage:
  python3 gather_sources.py <mission-reference> [-k KEYWORD ...] [--root path/to/web/wwwroot]

Prints:
  1. the mission's missions.json entry (and whether a summary already exists)
  2. articles (any depth) and videos whose MissionRef is this mission, with ContentUrl and word count
  3. other content files that mention the mission by keyword - the follow-ups
     (In the Line of Fire, honours lists, interim history, letters) that aren't linked by MissionRef

Keywords default to the distinctive words of the mission Name, plus its Planet. Add spelling variants with -k
(e.g. -k "Marie Celeste" -k Garavaglia) - the archive is inconsistent about names.
"""
import argparse, glob, json, os, re

ap = argparse.ArgumentParser()
ap.add_argument("mission")
ap.add_argument("-k", "--keyword", action="append", default=[])
ap.add_argument("--root", default=os.path.join(os.path.dirname(__file__), *[".."] * 4, "web", "wwwroot"))
args = ap.parse_args()
os.chdir(os.path.abspath(args.root))

def load(path):
    p = "." + path if path.startswith("/") else path
    with open(p, encoding="utf-8") as f:
        t = f.read()
    return json.loads(t) if t.strip() else []

def g(d, k):  # data files aren't consistent about key case
    return d.get(k) if k in d else d.get(k[0].lower() + k[1:])

def words(cu):
    p = "." + cu
    if not os.path.exists(p): return "MISSING"
    with open(p, encoding="utf-8") as f: return f"{len(f.read().split())} words"

missions = load("data/missions.json")
m = next((x for x in missions if x.get("Reference") == args.mission), None)
if not m:
    print(f"No mission '{args.mission}' in missions.json. Known references:")
    for x in sorted(missions, key=lambda x: x["StartDate"]): print(f"  {x['StartDate']}  {x['Reference']}  {x['Name']}")
    raise SystemExit(1)

print("== Mission")
print(json.dumps(m, indent=2))
print(f"Summary: {m['ContentUrl'] + ' (' + words(m['ContentUrl']) + ')' if m.get('ContentUrl') else 'none yet'}")

def walk(lst, path):
    for a in lst:
        yield a, path
        yield from walk(g(a, "Children") or [], path + [g(a, "Title")])

linked = set()
print("\n== Linked by MissionRef")
for kind, index in (("article", "data/articles.json"), ("video", "data/videos.json")):
    for grp in load(index):
        items = (g(grp, "Articles") or []) + (load(grp["Data"]) if grp.get("Data") else [])
        for a, path in walk(items, []):
            if g(a, "MissionRef") != args.mission: continue
            cu = g(a, "ContentUrl")
            if cu: linked.add(cu)
            parent = f" (child of {' > '.join(path)})" if path else ""
            print(f"  [{kind}:{grp['Reference']}] {g(a, 'Reference')} - {g(a, 'Title')}{parent}")
            print(f"      {cu or '(no ContentUrl)'}{' - ' + words(cu) if cu else ''}  pdf: {g(a, 'PdfUrl') or '-'}")

stop = {"mission", "operation", "boarding", "action", "the", "of", "a", "and", "to"}
kws = args.keyword or [w for w in re.findall(r"[A-Za-z][A-Za-z']+", m["Name"].replace("'", " ")) if w.lower() not in stop]
if not args.keyword and m.get("Planet"): kws.append(m["Planet"])
print(f"\n== Other content mentioning {kws}")
pat = re.compile(r"\b(?:" + "|".join(re.escape(k) for k in kws) + r")\b", re.I)  # whole words, so "Hal" skips "half" - pass each spelling variant
for f in sorted(glob.glob("content/**/*.md", recursive=True)):
    u = "/" + f
    if u in linked or u == m.get("ContentUrl"): continue
    with open(f, encoding="utf-8") as fh: hits = [l.strip() for l in fh if pat.search(l)]
    if hits:
        print(f"  {u} ({len(hits)} lines)  e.g. {hits[0][:90]}")
