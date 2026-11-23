#!/usr/bin/env python3
"""Verify an existing markdown transcription against the text extracted from its PDF.

Usage:
  python3 compare_md.py <article-reference> [--md path.md] [--extract-dir DIR]
                        [--case] [--all] [--context N]

Run extract_pdf.py <reference> first; this reads .transcribe/<reference>/page-NN.txt.
Words are compared case-insensitively and without punctuation (use --case to
compare case too), so only wording differences show up.

Each difference is classified and sorted most-worrying first:
  CHECK     the PDF word is confident and a real word, the MD differs:
            probably a typing/transcription error in the MD
  OMITTED   text in the PDF that is not in the MD (dropped line or paragraph?)
  ADDED     text in the MD that is not in the PDF (editorial heading? or
            text the OCR missed - look at the page)
  OCR?      the PDF word is low-confidence or not a word, the MD word is a
            real word: probably an OCR misread, MD is likely right
  MOVED     same run of words appears elsewhere: column/reading-order change
  NOISE     PDF-only fragments that look like scan specks read as text (single
            characters, low-confidence or non-words)
  SPACING   identical once spaces are removed (hyphenation, split words) -
            hidden unless --all

Exit status 0 always; the report is for judgement against the page images.
"""
import argparse, difflib, glob, json, os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _archive as A
from flag_suspects import WORD, Known, strip_markdown, is_swap

ORDER = ["CHECK", "OMITTED", "ADDED", "OCR?", "MOVED", "NOISE", "SPACING"]


def md_tokens(path, case):
    toks = []
    in_comment = False
    for n, raw in enumerate(open(path, encoding="utf-8").read().split("\n"), 1):
        if "<!--" in raw and "-->" not in raw:
            in_comment = True
        if in_comment:
            in_comment = "-->" not in raw
            continue
        for m in WORD.finditer(strip_markdown(raw)):
            t = m.group().replace("’", "'").strip("'")
            if t:
                toks.append(dict(w=t if case else t.lower(), raw=t, line=n))
    return toks


def src_tokens(extract_dir, case):
    toks = []
    for f in sorted(glob.glob(os.path.join(extract_dir, "page-*.txt"))):
        page = int(re.search(r"page-(\d+)", f).group(1))
        for n, raw in enumerate(open(f, encoding="utf-8").read().split("\n"), 1):
            for m in WORD.finditer(raw):
                t = m.group().replace("’", "'").strip("'")
                if t:
                    toks.append(dict(w=t if case else t.lower(), raw=t, page=page, line=n))
    return toks


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("reference")
    ap.add_argument("--md")
    ap.add_argument("--extract-dir")
    ap.add_argument("--case", action="store_true")
    ap.add_argument("--all", action="store_true")
    ap.add_argument("--context", type=int, default=4)
    args = ap.parse_args()

    res = A.find(args.reference)
    md = args.md or (A.content_path(res["content_url"]) if res else "")
    if not md or not os.path.isfile(md):
        sys.exit(f"no markdown for {args.reference} (pass --md)")
    xdir = args.extract_dir or os.path.join(A.WORK, res["reference"] if res else args.reference)
    if not glob.glob(os.path.join(xdir, "page-*.txt")):
        sys.exit(f"no extraction in {xdir}: run extract_pdf.py {args.reference} first")

    low = {}
    xj = os.path.join(xdir, "extract.json")
    if os.path.isfile(xj):
        for p in json.load(open(xj, encoding="utf-8"))["pages"]:
            for w in p.get("low_conf", []):
                for t in WORD.findall(w["word"]):
                    low[(p["page"], t.lower())] = w

    known = Known(exclude=md)
    S, M = src_tokens(xdir, args.case), md_tokens(md, args.case)
    sm = difflib.SequenceMatcher(None, [t["w"] for t in S], [t["w"] for t in M], autojunk=False)
    ops = [o for o in sm.get_opcodes() if o[0] != "equal"]
    deleted = " ".join(" ".join(t["w"] for t in S[i1:i2]) for tag, i1, i2, j1, j2 in ops if i2 > i1)
    inserted = " ".join(" ".join(t["w"] for t in M[j1:j2]) for tag, i1, i2, j1, j2 in ops if j2 > j1)

    def ctx(tokens, a, b):
        pre = " ".join(t["raw"] for t in tokens[max(0, a - args.context):a])
        post = " ".join(t["raw"] for t in tokens[b:b + args.context])
        mid = " ".join(t["raw"] for t in tokens[a:b])
        return f"…{pre} [{mid}] {post}…"

    findings = []
    for tag, i1, i2, j1, j2 in ops:
        s, m = S[i1:i2], M[j1:j2]
        sw, mw = "".join(t["w"] for t in s), "".join(t["w"] for t in m)
        crop = ""
        for t in s:
            lc = low.get((t["page"], t["w"].lower()))
            if lc and lc.get("crop"):
                crop = lc["crop"]; break
        conf_low = any((t["page"], t["w"].lower()) in low for t in s)
        if s and m and sw == mw:
            code = "SPACING"
        elif tag == "delete":
            if len(s) >= 4 and " ".join(t["w"] for t in s) in inserted:
                code = "MOVED"
            elif all(len(t["w"]) == 1 or (t["page"], t["w"].lower()) in low or not known(t["raw"]) for t in s):
                code = "NOISE"
            else:
                code = "OMITTED"
        elif tag == "insert":
            code = "MOVED" if len(m) >= 4 and " ".join(t["w"] for t in m) in deleted else "ADDED"
        else:
            src_ok = all(known(t["raw"]) for t in s) and not conf_low
            md_ok = all(known(t["raw"]) for t in m)
            swap = len(s) == len(m) and all(is_swap(a["raw"], b["raw"]) for a, b in zip(s, m))
            code = "OCR?" if (md_ok and (not src_ok or swap)) else "CHECK"
        if code == "SPACING" and not args.all:
            continue
        where_md = f"md L{m[0]['line']}" if m else (f"md ~L{M[j1]['line'] if j1 < len(M) else M[-1]['line']}" if M else "md -")
        where_pdf = f"p{s[0]['page']}" if s else (f"p{S[i1]['page'] if i1 < len(S) else S[-1]['page']}" if S else "-")
        findings.append((ORDER.index(code), code, where_md, where_pdf,
                         ctx(S, i1, i2) if s else "", ctx(M, j1, j2) if m else "", crop))

    findings.sort(key=lambda f: (f[0], int(re.sub(r"\D", "", f[2]) or 0)))
    ratio = sm.ratio()
    print(f"{args.reference}: {os.path.relpath(md, A.REPO)} vs {os.path.relpath(xdir, A.REPO)}")
    print(f"word agreement {ratio:.1%}  ({len(S)} PDF words, {len(M)} MD words)")
    if ratio < 0.6:
        print("LOW AGREEMENT: wrong PDF/MD pairing, a very poor scan (look at the pages),"
              " heavy reordering (columns), or the MD is a partial transcription.")
    counts = {c: sum(1 for f in findings if f[1] == c) for c in ORDER}
    print(", ".join(f"{k} {v}" for k, v in counts.items() if v) or "no differences")
    print()
    for _, code, wm, wp, sc, mc, crop in findings:
        print(f"{code:8} {wm:8} {wp:4}")
        if sc: print(f"   PDF: {sc}")
        if mc: print(f"   MD : {mc}")
        if crop: print(f"   crop: {crop}")


if __name__ == "__main__":
    main()
