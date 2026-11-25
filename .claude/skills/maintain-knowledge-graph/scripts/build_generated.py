#!/usr/bin/env python3
"""Regenerate <wwwroot>/data/kg/generated/: never hand-edit these files.

  mentions.json - which entities each article/mission/video page mentions (alias matching), both ways
  graph.jsonl   - one JSON object per line: nodes (entities, missions, articles, videos) then edges (facts),
                  for loading into a graph DB or as RAG chunk metadata

Usage: python3 build_generated.py web/wwwroot
"""
import json, os, re, sys, collections
from kglib import load_archive, load_kg, load_videos, content_ref_map

# Single-word aliases that are also ordinary words (or too generic) are not used for matching.
COMMON = set("""tech joe head reed drake hawk silver saint bear doe peace jack ruby trouble chaos zeus warlock
quark rouge sparky fugazi beck jester soldier the senator station commander sparks wings finder
sarge maniac shooty wolfman knight rider amy john sara luke james kenneth christopher harry viper
holly gina lucifer havoc nim nix angel cardinal trader darling miller sabre burke jarvis bean
adder stryker striker winston gibson apone finn walker willis jones west crouch smith hunter
franz simson parnell interstella diplos zealots amish grants cs sf rrb uw cmc cms pi bp ifc law
ss fff hfa sff cia sib ced irb dea ue gd ep gpp fp icg haf www jes bf""".split())

def usable(alias):
    a = alias.strip(' "\'')
    if len(a) < 3: return False
    if " " in a or "-" in a or "." in a: return True
    return a.lower() not in COMMON and len(a) >= 5

def main(root):
    kg = load_kg(root); articles, missions = load_archive(root)
    names = collections.defaultdict(set)
    for key, e in kg.items():
        for n in [e["Name"]] + e.get("Aliases", []):
            if n and usable(n): names[n.strip(' "\'')].add(key)
    names = {n: next(iter(k)) for n, k in names.items() if len(k) == 1}
    pats = sorted(names, key=len, reverse=True)
    rx = re.compile(r"(?<![\w'’])(" + "|".join(re.escape(p) for p in pats) + r")(?![\w'’])")
    by_doc = {}
    for path, refs in content_ref_map(root).items():
        full = os.path.join(root, path)
        if not os.path.exists(full): continue
        text = open(full, encoding="utf-8", errors="replace").read()
        found = collections.Counter(names[m.group(1)] for m in rx.finditer(text))
        # curated facts sourced from this document count as mentions too
        for key, e in kg.items():
            for x in e.get("Facts", []):
                if any(s in refs for s in x.get("Sources", [])): found[key] += 0
        for r in refs:
            by_doc[r] = sorted(found)
    by_ent = collections.defaultdict(list)
    for doc, ents in sorted(by_doc.items()):
        for k in ents: by_ent[k].append(doc)
    out = os.path.join(root, "data", "kg", "generated"); os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, "mentions.json"), "w", encoding="utf-8") as fh:
        json.dump({"ByDocument": by_doc, "ByEntity": dict(sorted(by_ent.items()))}, fh, indent=1, ensure_ascii=False); fh.write("\n")
    with open(os.path.join(out, "graph.jsonl"), "w", encoding="utf-8") as fh:
        w = lambda o: fh.write(json.dumps(o, ensure_ascii=False) + "\n")
        for key, e in sorted(kg.items()):
            w({"type": "node", "id": key, "kind": e["_kind"], "name": e["Name"], "aliases": e.get("Aliases", []),
               "description": e.get("Description"), "props": {k: e[k] for k in ("Type", "Species", "ServiceNumber") if k in e}})
        for ref, m in sorted(missions.items()):
            w({"type": "node", "id": "mission/" + ref, "kind": "mission", "name": m.get("Name"), "props": {k: m[k] for k in ("StartDate", "EndDate", "Planet", "Location", "ContentUrl") if m.get(k)}})
        for ref, a in sorted(articles.items()):
            w({"type": "node", "id": "article/" + ref, "kind": "article", "name": a.get("Title"), "props": {k: a[k] for k in ("ContentUrl", "PdfUrl", "MissionRef") if a.get(k)}})
        for ref, v in sorted(load_videos(root).items()):
            w({"type": "node", "id": "video/" + ref, "kind": "video", "name": v.get("Title"), "props": {k: v[k] for k in ("Series", "YouTubeVideoId", "ContentUrl", "MissionRef") if v.get(k)}})
        for key, e in sorted(kg.items()):
            for x in e.get("Facts", []):
                edge = {"type": "edge", "from": key, "p": x["P"]}
                edge.update({k.lower(): v for k, v in x.items() if k != "P"})
                w(edge)
        for doc, ents in sorted(by_doc.items()):
            for k in ents: w({"type": "edge", "from": doc, "p": "mentions", "to": k, "generated": True})
    print(f"mentions: {len(by_doc)} documents, {len(by_ent)} entities mentioned; graph.jsonl written")

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "web/wwwroot")
