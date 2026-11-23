#!/usr/bin/env python3
"""Record that a transcription was created or verified, in verification-log.md.

Usage: python3 log_verification.py <reference> <RESULT> "<notes>"

RESULT is one of:
  TRANSCRIBED  new markdown written from the PDF
  VERIFIED     existing markdown checked, no changes needed
  FIXED        existing markdown checked and corrected
  PARTIAL      checked, but some pages/passages still need Joel (say which in notes)

list_pdfs.py reads this log to show what has and hasn't been checked.
"""
import datetime, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _archive as A

RESULTS = {"TRANSCRIBED", "VERIFIED", "FIXED", "PARTIAL"}
if len(sys.argv) < 3 or sys.argv[2].upper() not in RESULTS:
    sys.exit(__doc__)
ref, result = sys.argv[1], sys.argv[2].upper()
notes = " ".join(sys.argv[3:]).replace("|", "/").replace("\n", " ")
if not os.path.isfile(A.VERIFY_LOG):
    with open(A.VERIFY_LOG, "w", encoding="utf-8") as f:
        f.write("# Transcription verification log\n\n"
                "Written by `scripts/log_verification.py`; newest last. "
                "`list_pdfs.py` shows the latest entry per reference.\n\n"
                "| Date | Reference | Result | Notes |\n|---|---|---|---|\n")
with open(A.VERIFY_LOG, "a", encoding="utf-8") as f:
    f.write(f"| {datetime.date.today().isoformat()} | {ref} | {result} | {notes} |\n")
print(f"logged {ref}: {result}")
