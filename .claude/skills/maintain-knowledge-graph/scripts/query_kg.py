#!/usr/bin/env python3
"""Look things up in the knowledge graph.

  query_kg.py web/wwwroot <name-or-alias-or-ref>   entity card: facts out, facts pointing in, mentions
  query_kg.py web/wwwroot --mission <mission-ref>  who/what took part in a mission
  query_kg.py web/wwwroot --search <text>          entities whose name/alias/description contains text
"""
import json, os, sys
from kglib import load_kg, load_archive

def fmt(x):
    q = ", ".join(f"{k}={x[k]}" for k in ("Role", "At", "Date", "From", "To", "Note") if k in x)
    return f"{x['P']}: {x.get('O') or ''}{' ' if x.get('O') and x.get('V') else ''}{x.get('V') or ''}" + (f"  [{q}]" if q else "") + f"  <- {', '.join(x['Sources'])}"

def find(kg, q):
    ql = q.lower()
    hits = [k for k, e in kg.items() if k == q or e["Reference"] == q]
    hits = hits or [k for k, e in kg.items() if ql in [n.lower() for n in [e["Name"]] + e.get("Aliases", [])]]
    return hits

def main(a):
    root, args = a[1], a[2:]
    kg = load_kg(root)
    gen = os.path.join(root, "data", "kg", "generated", "mentions.json")
    mentions = json.load(open(gen)) if os.path.exists(gen) else {"ByEntity": {}}
    if args[0] == "--search":
        t = args[1].lower()
        for k, e in kg.items():
            if t in (e["Name"] + " " + " ".join(e.get("Aliases", [])) + " " + (e.get("Description") or "")).lower():
                print(f"{k}: {e['Name']} - {e.get('Description','')}")
        return
    if args[0] == "--mission":
        m = "mission/" + args[1].removeprefix("mission/")
        for k, e in sorted(kg.items()):
            for x in e.get("Facts", []):
                if x.get("O") == m or x.get("At") == m: print(f"{e['Name']} ({k}) - {fmt(x)}")
        return
    for k in find(kg, " ".join(args)) or []:
        e = kg[k]
        print(f"== {e['Name']} ({k})"); print("aliases:", ", ".join(e.get("Aliases", [])))
        for f in ("Type", "Species", "ServiceNumber", "Description"):
            if e.get(f): print(f"{f}: {e[f]}")
        for x in e.get("Facts", []): print("  ", fmt(x))
        inc = [(k2, x) for k2, e2 in kg.items() for x in e2.get("Facts", []) if x.get("O") == k]
        if inc: print("  -- pointing here:")
        for k2, x in inc: print(f"   {kg[k2]['Name']} {fmt(x)}")
        if mentions["ByEntity"].get(k): print("  mentioned in:", ", ".join(mentions["ByEntity"][k]))

if __name__ == "__main__":
    main(sys.argv)
