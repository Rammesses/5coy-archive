#!/usr/bin/env python3
"""Check (and with --fix, rewrite) the text of every markdown link to a mission page.

Usage:
  python3 mission_links.py web/wwwroot          # report links whose text isn't standard
  python3 mission_links.py web/wwwroot --fix    # rewrite them in place

Every link to /mission/<ref> must read:
    [Mission '{name}' - {Month} {in-game year} ({planet})](/mission/<ref>)
built from missions.json: Name as-is (it already carries "Mission '...'", "Operation '...'",
or "Mission Unknown"), EndDate + 500 years as the full month name and year, and Planet in
brackets (left out when Planet is empty, "None" or "?"). This mirrors Mission.LinkText in
web/Models/Mission.cs, which the Razor templates use, so static and generated links match.

Exit status 1 when links are wrong (without --fix) or point at an unknown mission.
"""
import datetime, json, os, re, sys

LINK = re.compile(r'\[([^\]]*)\]\(/mission/([^)\s]+)\)')


def link_text(m):
    d = datetime.date.fromisoformat(m["EndDate"][:10])
    planet = (m.get("Planet") or "").strip()
    planet = "" if planet in ("", "None", "?") else f" ({planet})"
    return f"{m['Name']} - {d.strftime('%B')} {d.year + 500}{planet}"


def main():
    root = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith("-") else "web/wwwroot"
    fix = "--fix" in sys.argv
    missions = {}
    for m in json.load(open(os.path.join(root, "data", "missions.json"), encoding="utf-8")):
        m = {k[0].upper() + k[1:]: v for k, v in m.items()}
        if m.get("Reference"):
            missions[m["Reference"]] = m
    bad = unknown = fixed = 0
    for dirpath, _, files in os.walk(os.path.join(root, "content")):
        for f in sorted(files):
            if not f.endswith(".md"):
                continue
            path = os.path.join(dirpath, f)
            text = open(path, encoding="utf-8").read()

            def repl(mo):
                nonlocal bad, unknown, fixed
                ref = mo.group(2)
                if ref not in missions:
                    unknown += 1
                    print(f"UNKNOWN  {os.path.relpath(path, root)}: /mission/{ref}")
                    return mo.group(0)
                want = link_text(missions[ref])
                if mo.group(1) == want:
                    return mo.group(0)
                bad += 1
                if not fix:
                    print(f"TEXT     {os.path.relpath(path, root)}: [{mo.group(1)}] -> [{want}]")
                    return mo.group(0)
                fixed += 1
                return f"[{want}](/mission/{ref})"

            new = LINK.sub(repl, text)
            if fix and new != text:
                open(path, "w", encoding="utf-8").write(new)
    print(f"{bad} non-standard link(s){f', {fixed} fixed' if fix else ''}, {unknown} unknown mission ref(s)")
    sys.exit(1 if unknown or (bad and not fix) else 0)


if __name__ == "__main__":
    main()
