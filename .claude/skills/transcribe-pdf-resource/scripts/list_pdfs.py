#!/usr/bin/env python3
"""Inventory every PDF resource in the archive and its markdown status.

Usage: python3 list_pdfs.py [--todo | --unverified] [--group GROUP]

Status:
  NO-MD     PdfUrl set, ContentUrl empty         -> needs a transcription
  MD-GONE   ContentUrl set but the file is missing -> needs a transcription (or a URL fix)
  MD        markdown exists                       -> candidate for verification
Columns also show whether a local copy of the PDF is available, and when the
markdown was last verified (from verification-log.md).
"""
import argparse, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _archive as A

ap = argparse.ArgumentParser()
ap.add_argument("--todo", action="store_true", help="only resources with no markdown")
ap.add_argument("--unverified", action="store_true", help="only MD resources never verified")
ap.add_argument("--group")
args = ap.parse_args()

log = A.last_verified()
rows = []
for r in A.resources():
    if args.group and r["group"] != args.group:
        continue
    md = A.content_path(r["content_url"])
    status = "NO-MD" if not md else ("MD" if os.path.isfile(md) else "MD-GONE")
    if args.todo and status == "MD":
        continue
    if args.unverified and (status != "MD" or r["reference"] in log):
        continue
    have = any(os.path.isfile(c) for c in A.media_candidates(r["pdf_url"]))
    v = log.get(r["reference"])
    rows.append((status, r["group"], r["reference"], "yes" if have else "-",
                 f"{v[0]} {v[1]}" if v else "-", r["pdf_url"]))

order = {"NO-MD": 0, "MD-GONE": 1, "MD": 2}
rows.sort(key=lambda x: (order[x[0]], x[1], x[2]))
print(f"{'STATUS':8} {'GROUP':20} {'REFERENCE':55} {'PDF':4} {'VERIFIED':24} PDFURL")
for row in rows:
    print(f"{row[0]:8} {row[1]:20} {row[2]:55} {row[3]:4} {row[4]:24} {row[5]}")
counts = {k: sum(1 for r in rows if r[0] == k) for k in order}
print(f"\n{len(rows)} resources: " + ", ".join(f"{v} {k}" for k, v in counts.items())
      + f"; local PDFs in {A.MEDIA_DIR}")
