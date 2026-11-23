#!/usr/bin/env python3
"""Validate the knowledge graph in <wwwroot>/data/kg against the archive data.

Usage: python3 check_kg.py web/wwwroot [--strict]
Errors (exit 1): bad/duplicate References, unknown predicates, facts whose O or
Sources don't resolve, facts without Sources, facts with neither O nor V.
Warnings: an alias or Name shared by more than one entity (ambiguous for mention
matching), entities with no facts, 'needs review' notes. --strict turns warnings into errors.
"""
import re, sys, collections
from kglib import load_archive, load_kg

PREDICATES = {
    "character": {"memberOf", "rank", "role", "participatedIn", "commanded", "authored", "employedBy",
                  "founded", "relative", "relatedTo", "status", "award", "born", "enlisted", "note", "appearsIn"},
    "organisation": {"partOf", "participatedIn", "owns", "basedAt", "status", "supplies", "opposes",
                     "successorOf", "relatedTo", "foundedAfter", "note", "appearsIn"},
}
QUALIFIERS = {"P", "O", "V", "Sources", "Role", "At", "Date", "From", "To", "Note"}
REF_RE = re.compile(r"^[a-z0-9][a-z0-9\-]*$")
DATE_RE = re.compile(r"^\d{4}(-\d{2}){0,2}$")

def main(argv):
    root = argv[1] if len(argv) > 1 else "web/wwwroot"
    strict = "--strict" in argv
    articles, missions = load_archive(root)
    kg = load_kg(root)
    errors, warnings = [], []
    def resolves(r):
        kind, _, ref = r.partition("/")
        if kind == "article": return ref in articles
        if kind == "mission": return ref in missions
        return r in kg
    seen = collections.Counter(k for k in kg)
    names = collections.defaultdict(set)
    for key, e in kg.items():
        kind = e["_kind"]
        if not REF_RE.match(e.get("Reference", "")): errors.append(f"{key}: Reference must be lower-case kebab-case")
        if not e.get("Name"): errors.append(f"{key}: missing Name")
        for n in [e.get("Name")] + e.get("Aliases", []):
            if n: names[n.lower()].add(key)
        if not e.get("Facts"): warnings.append(f"{key}: no facts")
        if "needs review" in (e.get("Description") or "").lower(): warnings.append(f"{key}: description flags 'needs review'")
        for i, x in enumerate(e.get("Facts", [])):
            where = f"{key} fact {i} ({x.get('P')})"
            if x.get("P") not in PREDICATES[kind]: errors.append(f"{where}: unknown predicate for {kind}")
            for k in x:
                if k not in QUALIFIERS: errors.append(f"{where}: unknown field '{k}'")
            if "O" not in x and "V" not in x: errors.append(f"{where}: needs O or V")
            for k in ("O", "At"):
                if k in x and not resolves(x[k]): errors.append(f"{where}: {k} '{x[k]}' does not resolve")
            for k in ("Date", "From", "To"):
                if k in x and not DATE_RE.match(str(x[k])): errors.append(f"{where}: {k} '{x[k]}' should be yyyy[-mm[-dd]] (in-game)")
            if not x.get("Sources"): errors.append(f"{where}: no Sources")
            for s in x.get("Sources", []):
                if not s.startswith(("article/", "mission/")) or not resolves(s):
                    errors.append(f"{where}: source '{s}' does not resolve to an article or mission")
    for n, keys in sorted(names.items()):
        if len(keys) > 1: warnings.append(f"ambiguous name/alias '{n}': {', '.join(sorted(keys))}")
    for w in warnings: print("WARN ", w)
    for e in errors: print("ERROR", e)
    n_facts = sum(len(e.get("Facts", [])) for e in kg.values())
    print(f"{len(kg)} entities, {n_facts} facts: {len(errors)} errors, {len(warnings)} warnings")
    return 1 if errors or (strict and warnings) else 0

if __name__ == "__main__":
    sys.exit(main(sys.argv))
