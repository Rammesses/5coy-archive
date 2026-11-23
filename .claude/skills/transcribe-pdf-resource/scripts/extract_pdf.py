#!/usr/bin/env python3
"""Extract the text of an archive PDF, OCR-ing any page with no embedded text.

Usage:
  python3 extract_pdf.py <article-reference | path/to/file.pdf> [options]

Options:
  --force-ocr        OCR every page even if it has a text layer (use when the
                     embedded text is itself a poor OCR layer)
  --pages 1-3,5      only these pages
  --psm N            Tesseract page-segmentation mode (default 3 = auto;
                     4 = single column, 6 = uniform block, 11 = sparse text)
  --min-conf N       words below this Tesseract confidence are flagged (default 60)
  --out DIR          output folder (default .transcribe/<reference>/)
  --redo             re-extract pages already done (otherwise a re-run resumes,
                     skipping finished pages - OCR takes ~20 s a page)

Writes to the output folder:
  page-NN.png        page image at 150 dpi, for checking against the text
  page-NN.txt        that page's text
  crops/pNN-*.png    the line around each low-confidence OCR word
  draft.txt          all pages, with <!-- page N --> markers
  extract.json       per-page method, mean confidence, low-confidence words + crop paths

Needs poppler-utils (pdfinfo, pdftotext, pdftoppm), tesseract and Pillow.
"""
import argparse, csv, glob, io, json, os, re, shutil, subprocess, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _archive as A

OCR_DPI, VIEW_DPI, MAX_CROPS = 300, 150, 300
TEXT_MIN_CHARS = 40  # fewer alphanumerics than this on a page => treat as scanned


def run(cmd, **kw):
    return subprocess.run(cmd, check=True, capture_output=True, **kw)


def parse_pages(spec, n):
    if not spec:
        return list(range(1, n + 1))
    out = []
    for part in spec.split(","):
        a, _, b = part.partition("-")
        out += range(int(a), int(b or a) + 1)
    return [p for p in out if 1 <= p <= n]


def page_count(pdf):
    info = run(["pdfinfo", pdf], text=True).stdout
    return int(re.search(r"^Pages:\s+(\d+)", info, re.M).group(1))


def render(pdf, page, dpi, gray=False):
    from PIL import Image
    cmd = ["pdftoppm", "-f", str(page), "-l", str(page), "-r", str(dpi), "-png"]
    if gray:
        cmd.append("-gray")
    png = run(cmd + [pdf]).stdout
    return Image.open(io.BytesIO(png))


def preprocess(img):
    """Grey, auto-contrast and gently sharpen: helps with faded photocopies."""
    from PIL import ImageOps, ImageFilter
    img = ImageOps.autocontrast(img.convert("L"), cutoff=1)
    return img.filter(ImageFilter.SHARPEN)


def ocr(img, psm):
    """Run tesseract, return (text, words) where words carry conf and boxes."""
    p = subprocess.run(["tesseract", "stdin", "stdout", "--psm", str(psm), "-l", "eng",
                        "-c", "preserve_interword_spaces=1", "tsv"],
                       input=_png_bytes(img), capture_output=True, check=True)
    rows = list(csv.DictReader(io.StringIO(p.stdout.decode("utf-8", "replace")),
                               delimiter="\t", quoting=csv.QUOTE_NONE))
    words, lines = [], {}
    for r in rows:
        if r.get("level") != "5" or not (r.get("text") or "").strip():
            continue
        key = (int(r["block_num"]), int(r["par_num"]), int(r["line_num"]))
        w = dict(text=r["text"], conf=float(r["conf"]), key=key,
                 box=[int(r["left"]), int(r["top"]), int(r["width"]), int(r["height"])])
        words.append(w)
        lines.setdefault(key, []).append(w)
    # Rebuild text: lines within a paragraph, blank line between paragraphs/blocks.
    out, prev = [], None
    for key in sorted(lines):
        if prev and key[:2] != prev[:2]:
            out.append("")
        out.append(" ".join(w["text"] for w in lines[key]))
        prev = key
    for w in words:
        w["line"] = " ".join(x["text"] for x in lines[w["key"]])
    return "\n".join(out).strip() + "\n", words, lines


def _png_bytes(img):
    b = io.BytesIO()
    img.save(b, format="PNG")
    return b.getvalue()


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("target")
    ap.add_argument("--force-ocr", action="store_true")
    ap.add_argument("--pages")
    ap.add_argument("--psm", type=int, default=3)
    ap.add_argument("--min-conf", type=float, default=60)
    ap.add_argument("--out")
    ap.add_argument("--redo", action="store_true")
    args = ap.parse_args()

    for tool in ("pdfinfo", "pdftotext", "pdftoppm", "tesseract"):
        if not shutil.which(tool):
            sys.exit(f"missing tool: {tool} (apt install poppler-utils tesseract-ocr)")

    res = A.find(args.target)
    if res:
        pdf, msg = A.local_pdf(res["pdf_url"])
        ref = res["reference"]
    elif args.target.lower().endswith(".pdf"):
        pdf, msg = (args.target, None) if os.path.isfile(args.target) else (None, f"not found: {args.target}")
        ref = A.slug(os.path.splitext(os.path.basename(args.target))[0])
    else:
        sys.exit(f"'{args.target}' is neither an article reference with a PdfUrl nor a .pdf path"
                 " (run list_pdfs.py to see references)")
    if not pdf:
        sys.exit(msg)

    out = os.path.abspath(args.out or os.path.join(A.WORK, ref))
    os.makedirs(os.path.join(out, "crops"), exist_ok=True)
    n = page_count(pdf)
    report = dict(reference=ref, pdf=pdf, page_count=n, pages=[], min_conf=args.min_conf)
    crops = len(os.listdir(os.path.join(out, "crops")))
    print(f"{ref}: {pdf} ({n} pages)", flush=True)

    for page in parse_pages(args.pages, n):
        tag = f"{page:02d}"
        pj = os.path.join(out, f"page-{tag}.json")
        if os.path.isfile(pj) and not args.redo:
            continue
        for old in glob.glob(os.path.join(out, "crops", f"p{tag}-*.png")):
            try: os.remove(old)
            except OSError: pass
        view = render(pdf, page, VIEW_DPI)
        view_path = os.path.join(out, f"page-{tag}.png")
        view.save(view_path)

        text = run(["pdftotext", "-f", str(page), "-l", str(page), "-layout", pdf, "-"],
                   text=True).stdout
        info = dict(page=page, image=os.path.relpath(view_path, A.REPO), low_conf=[])
        if not args.force_ocr and len(re.findall(r"[A-Za-z0-9]", text)) >= TEXT_MIN_CHARS:
            info.update(method="text-layer", mean_conf=None)
        else:
            hi = preprocess(render(pdf, page, OCR_DPI, gray=True))
            text, words, lines = ocr(hi, args.psm)
            confs = [w["conf"] for w in words if w["conf"] >= 0]
            info.update(method="ocr", mean_conf=round(sum(confs) / len(confs), 1) if confs else 0,
                        words=len(words))
            for w in words:
                if 0 <= w["conf"] < args.min_conf and re.search(r"\w", w["text"]):
                    entry = dict(word=w["text"], conf=w["conf"], line=w["line"])
                    if crops < MAX_CROPS:
                        lw = lines[w["key"]]
                        x0 = min(x["box"][0] for x in lw); y0 = min(x["box"][1] for x in lw)
                        x1 = max(x["box"][0] + x["box"][2] for x in lw)
                        y1 = max(x["box"][1] + x["box"][3] for x in lw)
                        pad = 12
                        crop = hi.crop((max(0, x0 - pad), max(0, y0 - pad), x1 + pad, y1 + pad))
                        cp = os.path.join(out, "crops", f"p{tag}-{crops:03d}-{A.slug(w['text'])[:20] or 'x'}.png")
                        crop.save(cp)
                        entry["crop"] = os.path.relpath(cp, A.REPO)
                        crops += 1
                    info["low_conf"].append(entry)
            if not words:
                info["note"] = "OCR found no words: blank page, image/plan, or try another --psm"
        with open(os.path.join(out, f"page-{tag}.txt"), "w", encoding="utf-8") as f:
            f.write(text)
        with open(pj, "w", encoding="utf-8") as f:
            json.dump(info, f, indent=1)
        extra = f", mean conf {info['mean_conf']}, {len(info['low_conf'])} low-confidence words" if info["method"] == "ocr" else ""
        print(f"  page {page:>3}: {info['method']}{extra}" + (f"  ({info['note']})" if info.get("note") else ""), flush=True)

    # Assemble from every finished page (this run and earlier ones).
    draft = []
    for pj in sorted(glob.glob(os.path.join(out, "page-*.json"))):
        info = json.load(open(pj, encoding="utf-8"))
        text = open(pj[:-5] + ".txt", encoding="utf-8").read()
        report["pages"].append(info)
        draft.append(f"<!-- page {info['page']} ({info['method']}"
                     + (f", mean conf {info['mean_conf']}" if info["mean_conf"] is not None else "")
                     + f") -->\n{text.rstrip()}\n")
    done = {p["page"] for p in report["pages"]}
    missing = [p for p in range(1, n + 1) if p not in done]

    with open(os.path.join(out, "draft.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(draft))
    with open(os.path.join(out, "extract.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, indent=1)

    if missing:
        print(f"NOT YET EXTRACTED: pages {','.join(map(str, missing))} - run the same command again to continue")
    else:
        print(f"all {n} pages extracted")
    print(f"output: {os.path.relpath(out, A.REPO)}/  (draft.txt, page-NN.png, extract.json, crops/)")


if __name__ == "__main__":
    main()
