#!/usr/bin/env python3
"""Straighten a scanned picture that was scanned at a slight angle, using its frame.

Usage:
  python3 deskew_image.py <in.png> [<out.png>] [--crop] [--margin N] [--max-angle D] [--width W]

Finds the outline of the largest dark shape in the image (a panel's border, a printed frame),
measures how far it's tilted, and rotates the whole image back by that much, filling the new
corners with white. With --crop it then trims to that frame plus --margin pixels (default 8),
which also removes the scan's slanted edges. --width W scales the result down to W pixels wide
(never up). Angles over --max-angle degrees (default 5) are treated as "not a skew" and the image
is left alone, so a deliberately rotated picture isn't "corrected".

Prints the angle found and what was done. Needs OpenCV (cv2), numpy and Pillow.
"""
import argparse, sys
import cv2
import numpy as np
from PIL import Image


def frame_angle(gray):
    """Tilt in degrees of the largest dark shape, and its contour."""
    _, bw = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    bw = cv2.morphologyEx(bw, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    contours, _ = cv2.findContours(bw, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    if not contours:
        return 0.0, None
    big = max(contours, key=cv2.contourArea)
    (_, _), (w, h), ang = cv2.minAreaRect(big)
    # OpenCV reports the angle of a rectangle edge in [0, 90); bring it to the nearest axis
    while ang > 45:
        ang -= 90
    while ang < -45:
        ang += 90
    return float(ang), big


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("src"); ap.add_argument("dst", nargs="?")
    ap.add_argument("--crop", action="store_true"); ap.add_argument("--margin", type=int, default=8)
    ap.add_argument("--max-angle", type=float, default=5.0); ap.add_argument("--width", type=int)
    a = ap.parse_args()
    dst = a.dst or a.src

    im = Image.open(a.src)
    mode = "L" if im.mode in ("L", "1", "P", "LA") else "RGB"
    im = im.convert(mode)
    arr = np.array(im)
    gray = arr if mode == "L" else cv2.cvtColor(arr, cv2.COLOR_RGB2GRAY)

    ang, _ = frame_angle(gray)
    note = []
    if abs(ang) > a.max_angle:
        note.append(f"angle {ang:.2f} deg is over --max-angle, not rotated")
    elif abs(ang) >= 0.05:
        h, w = gray.shape
        M = cv2.getRotationMatrix2D((w / 2, h / 2), ang, 1.0)
        cos, sin = abs(M[0, 0]), abs(M[0, 1])
        nw, nh = int(h * sin + w * cos), int(h * cos + w * sin)
        M[0, 2] += nw / 2 - w / 2
        M[1, 2] += nh / 2 - h / 2
        fill = 255 if mode == "L" else (255, 255, 255)
        arr = cv2.warpAffine(arr, M, (nw, nh), flags=cv2.INTER_CUBIC, borderValue=fill)
        gray = arr if mode == "L" else cv2.cvtColor(arr, cv2.COLOR_RGB2GRAY)
        note.append(f"rotated {ang:+.2f} deg")
    else:
        note.append(f"already straight ({ang:+.2f} deg)")

    if a.crop:
        _, big = frame_angle(gray)
        if big is not None:
            # union of every shape at least 5% the size of the largest, so a page with
            # two side-by-side panels keeps both
            _, bw = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
            bw = cv2.morphologyEx(bw, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
            cs, _ = cv2.findContours(bw, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
            keep = [c for c in cs if cv2.contourArea(c) >= 0.05 * cv2.contourArea(big)]
            x, y, w, h = cv2.boundingRect(np.vstack(keep))
            m = a.margin
            x0, y0 = max(x - m, 0), max(y - m, 0)
            x1, y1 = min(x + w + m, gray.shape[1]), min(y + h + m, gray.shape[0])
            arr = arr[y0:y1, x0:x1]
            note.append(f"cropped to frame {x1 - x0}x{y1 - y0}")

    out = Image.fromarray(arr)
    if a.width and out.width > a.width:
        out = out.resize((a.width, round(out.height * a.width / out.width)), Image.LANCZOS)
        note.append(f"scaled to {out.width}x{out.height}")
    out.save(dst, optimize=True)
    print(f"{dst}: " + ", ".join(note))


if __name__ == "__main__":
    main()
