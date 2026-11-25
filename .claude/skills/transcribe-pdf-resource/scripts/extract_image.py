#!/usr/bin/env python3
"""Cut an illustration or photo out of an archive PDF page and save it as a PNG.

Usage:
  python3 extract_image.py <article-reference | path/to/file.pdf> <page> <x0,y0,x1,y1> <out.png>
                           [--colour] [--dpi N]

<page> is 1-based. The box is in pixels of the 150 dpi page image that extract_pdf.py
writes (.transcribe/<reference>/page-NN.png), so you can read the coordinates straight
off the image you looked at. They are scaled to the source resolution before cropping.

The crop comes from the best source available:
  1. the page's embedded image (`pdfimages`), when the page is a single full-page scan:
     this is the original pixels, nothing re-rendered (turned to match the page's /Rotate,
     so landscape pages come out the way they're displayed);
  2. otherwise the page rendered with `pdftoppm` at --dpi (default 300).

Output is greyscale unless --colour is given (the scans are black and white or greyscale;
greyscale roughly halves the file size). The PNG is optimised.
After saving, it prints the output path, its size in pixels and bytes, and the source used.

Needs poppler-utils (pdfimages, pdftoppm, pdfinfo) and Pillow.
"""
import argparse, glob, os, subprocess, sys, tempfile
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _archive as A
from PIL import Image

VIEW_DPI = 150


def page_rotation(pdf, page):
    """The page's /Rotate in degrees clockwise (0, 90, 180, 270)."""
    out = subprocess.run(["pdfinfo", "-f", str(page), "-l", str(page), pdf],
                         capture_output=True, text=True, check=True).stdout
    for line in out.splitlines():
        if "rot:" in line:
            return int(float(line.split("rot:")[1].split()[0])) % 360
    return 0


def page_size_pts(pdf, page):
    out = subprocess.run(["pdfinfo", "-f", str(page), "-l", str(page), pdf],
                         capture_output=True, text=True, check=True).stdout
    for line in out.splitlines():
        if line.startswith(f"Page {page:4d} size:") or (line.startswith("Page") and "size:" in line):
            parts = line.split("size:")[1].split()
            return float(parts[0]), float(parts[2])
    for line in out.splitlines():
        if line.startswith("Page size:"):
            parts = line.split(":")[1].split()
            return float(parts[0]), float(parts[2])
    raise SystemExit("could not read page size from pdfinfo")


def embedded_scan(pdf, page, tmp):
    """The page's single embedded image if it covers the page, else None."""
    lst = subprocess.run(["pdfimages", "-list", "-f", str(page), "-l", str(page), pdf],
                         capture_output=True, text=True, check=True).stdout.splitlines()[2:]
    rows = [l.split() for l in lst if l.strip()]
    if len(rows) != 1 or rows[0][2] != "image":
        return None
    subprocess.run(["pdfimages", "-png", "-f", str(page), "-l", str(page), pdf,
                    os.path.join(tmp, "img")], check=True)
    files = sorted(glob.glob(os.path.join(tmp, "img-*.png")))
    return Image.open(files[0]) if files else None


def rendered(pdf, page, dpi, tmp):
    base = os.path.join(tmp, "page")
    subprocess.run(["pdftoppm", "-r", str(dpi), "-f", str(page), "-l", str(page), "-png",
                    pdf, base], check=True)
    return Image.open(sorted(glob.glob(base + "*.png"))[0])


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("target"); ap.add_argument("page", type=int); ap.add_argument("box"); ap.add_argument("out")
    ap.add_argument("--colour", action="store_true"); ap.add_argument("--dpi", type=int, default=300)
    a = ap.parse_args()

    res = A.find(a.target)
    if res:
        pdf, msg = A.local_pdf(res["pdf_url"])
    elif a.target.lower().endswith(".pdf") and os.path.isfile(a.target):
        pdf, msg = a.target, None
    else:
        pdf, msg = None, f"'{a.target}' is neither an article reference with a PdfUrl nor a .pdf path"
    if not pdf:
        sys.exit(msg)

    x0, y0, x1, y1 = (float(v) for v in a.box.split(","))
    w_pt, h_pt = page_size_pts(pdf, a.page)
    with tempfile.TemporaryDirectory() as tmp:
        im, src = embedded_scan(pdf, a.page, tmp), "embedded scan (pdfimages)"
        rot = page_rotation(pdf, a.page)
        if im is not None and rot:
            # pdfimages returns the image as stored; the page shows it turned by /Rotate
            # (clockwise), and the 150 dpi page images are rendered the turned way up.
            im = im.rotate(-rot, expand=True)
            src += f", turned {rot} degrees to match the page"
        if rot in (90, 270):
            w_pt, h_pt = h_pt, w_pt
        if im is None:
            im, src = rendered(pdf, a.page, a.dpi, tmp), f"rendered at {a.dpi} dpi (pdftoppm)"
        # scale from 150 dpi view pixels to this image's pixels
        sx = im.width / (w_pt / 72 * VIEW_DPI)
        sy = im.height / (h_pt / 72 * VIEW_DPI)
        box = (round(x0 * sx), round(y0 * sy), round(x1 * sx), round(y1 * sy))
        crop = im.crop(box)
        crop = crop.convert("RGB") if a.colour else crop.convert("L")
        os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
        crop.save(a.out, optimize=True)
    print(f"{a.out}: {crop.width}x{crop.height} px, {os.path.getsize(a.out)} bytes, from {src}")


if __name__ == "__main__":
    main()
