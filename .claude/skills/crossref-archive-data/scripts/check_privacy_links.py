#!/usr/bin/env python3
"""Flag out-of-character personal data and web links in the archive markdown.

Usage:
  python3 check_privacy_links.py web/wwwroot            # report
  python3 check_privacy_links.py web/wwwroot --links    # also list every web link and its marker

Reports, per file and line:
  EMAIL     an email address (must be replaced with [email redacted])
  PHONE     something that looks like a UK phone number (must be [phone redacted])
  POSTCODE  something that looks like a UK postcode (the address it belongs to must be [address redacted])
  LINK      a web link without a "link checked" marker (must be checked; see CLAUDE.md)

In-character text can contain things that look like these (ident codes, grid references, box
numbers): judge each hit, and add the file:line to ALLOW below if it's genuinely in-game.
Exit status 1 when anything is reported.
"""
import os, re, sys

EMAIL = re.compile(r'[\w.+-]+@[\w-]+(?:\.[\w-]+)+')
PHONE = re.compile(r'(?<![\w/.-])(?:\+44\s?|0)(?:\d\s?){9,10}(?![\w/-])')
POSTCODE = re.compile(r'\b[A-Z]{1,2}\d[A-Z\d]?\s+\d[A-Z]{2}\b')
LINK = re.compile(r'\b(?:https?://|www\.)[^\s)\]>\'"]+', re.I)
MARK = re.compile(r'link (?:checked|dead|archived)', re.I)
ALLOW = {
    "content/mission-reports/2492-10_mission-procyon-intel.md:153",  # in-game comms log timestamp, not a phone number
}


def main():
    root = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith('-') else 'web/wwwroot'
    show_links = '--links' in sys.argv
    hits = 0
    for dirpath, _, files in os.walk(os.path.join(root, 'content')):
        for f in sorted(files):
            if not f.endswith('.md'):
                continue
            path = os.path.join(dirpath, f)
            rel = os.path.relpath(path, root)
            for n, line in enumerate(open(path, encoding='utf-8'), 1):
                if f'{rel}:{n}' in ALLOW:
                    continue
                for kind, rx in (('EMAIL', EMAIL), ('PHONE', PHONE), ('POSTCODE', POSTCODE)):
                    for m in rx.finditer(line):
                        hits += 1
                        print(f'{kind:9}{rel}:{n}: {m.group(0)}')
                for m in LINK.finditer(line):
                    marked = bool(MARK.search(line))
                    if not marked:
                        hits += 1
                        print(f'LINK     {rel}:{n}: {m.group(0)}  (no "link checked/dead" marker)')
                    elif show_links:
                        print(f'link ok  {rel}:{n}: {m.group(0)}')
    print(f'{hits} item(s) to review')
    sys.exit(1 if hits else 0)


if __name__ == '__main__':
    main()
