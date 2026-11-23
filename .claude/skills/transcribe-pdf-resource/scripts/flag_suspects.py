#!/usr/bin/env python3
"""Flag words in a transcription that are probably OCR (or typing) errors.

Usage:
  python3 flag_suspects.py <file.md|file.txt> [--extract .transcribe/<ref>/extract.json]
                           [--no-unknown] [--summary]

Each line of output is  LINE:COL  CODE  token  -> suggestion / note  [crop]

Codes, roughly most to least likely to be a real error:
  JUNK    stray symbol or lone character typical of scan noise (| ~ ¢ § » ...)
  MIXED   digits inside a word or letters inside a number (C0rporal, 24O5, l992)
  OCR     unknown word that becomes a known word with one classic OCR swap
          (rn/m, cl/d, vv/w, li/h, 0/o, 1/l, 5/s, c/e ...): suggestion given
  SPLIT   two fragments that form a known word when joined (invest igation)
  JOINED  unknown word that splits cleanly into two known words (themarines)
  HYPHEN  line-end hyphen; check whether it is a broken word or a real compound
  DOUBLE  repeated word (the the)
  LOWCONF a valid word Tesseract was very unsure of (conf < 50; needs --extract
          and only works on the raw draft, before lines are reformatted)
  UNKNOWN not in the dictionary or the archive lexicon: a misread, a name, or
          the original author's own spelling. Check against the page image.

The archive lexicon = .transcribe/lexicon.txt + every word used in at least
two different content/*.md files + titles/names in the data JSON. pyspellchecker,
if installed, supplies the English dictionary (pip install --user pyspellchecker).

Nothing here is a verdict: the page image decides. Period spellings that really are
on the page (COPYS, SARGEANT) are kept as-is.
"""
import argparse, collections, glob, json, os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _archive as A

LOWCONF_FLAG = 50  # flag otherwise-valid words only when Tesseract was this unsure
WORD = re.compile(r"[A-Za-z0-9À-ɏ][A-Za-z0-9À-ɏ'’]*")
JUNK_CHARS = set("|~^¢§»«¬¦¤±°¥£€�") - set("£")  # £ is legitimate in prices
SWAPS = [("rn", "m"), ("m", "rn"), ("cl", "d"), ("d", "cl"), ("vv", "w"), ("w", "vv"),
         ("li", "h"), ("h", "li"), ("ii", "u"), ("u", "ii"), ("in", "m"), ("m", "in"),
         ("0", "o"), ("o", "0"), ("1", "l"), ("l", "1"), ("1", "i"), ("i", "1"),
         ("l", "i"), ("i", "l"), ("I", "l"), ("l", "I"), ("5", "s"), ("s", "5"),
         ("8", "b"), ("6", "b"), ("c", "e"), ("e", "c"), ("h", "b"), ("b", "h"),
         ("n", "u"), ("u", "n"), ("f", "t"), ("t", "f"), ("a", "o"), ("o", "a"),
         ("e", "o"), ("o", "e"), ("j", "i"), ("r", "n"), ("n", "r"), ("fi", "f"), ("ri", "n"),
         ("g", "q"), ("q", "g"), ("g", "9"), ("9", "g"), ("g", "o"), ("o", "g"), ("t", "l"), ("l", "t"),
         ("3", "8"), ("8", "3"), ("6", "5"), ("5", "6"), ("7", "1"), ("1", "7")]


def load_dictionary():
    try:
        from spellchecker import SpellChecker
        return SpellChecker()
    except ImportError:
        print("note: pyspellchecker not installed; using the archive lexicon only "
              "(pip install --user pyspellchecker)", file=sys.stderr)
        return None


def build_lexicon(exclude=None):
    words = set()
    lex = A.LEXICON
    if os.path.isfile(lex):
        for line in open(lex, encoding="utf-8"):
            line = line.split("#", 1)[0].strip()
            if line:
                words.update(w.lower() for w in WORD.findall(line))
    seen = collections.Counter()
    for f in glob.glob(os.path.join(A.WWWROOT, "content", "**", "*.md"), recursive=True):
        if exclude and os.path.abspath(f) == os.path.abspath(exclude):
            continue  # don't let the file under test vouch for its own errors
        seen.update({w.lower() for w in WORD.findall(open(f, encoding="utf-8", errors="replace").read())})
    words.update(w for w, c in seen.items() if c >= 2)
    for f in glob.glob(os.path.join(A.WWWROOT, "data", "**", "*.json"), recursive=True):
        text = open(f, encoding="utf-8", errors="replace").read()
        for m in re.finditer(r'"(?:Title|Name|Planet|Location)"\s*:\s*"([^"]*)"', text, re.I):
            words.update(w.lower() for w in WORD.findall(m.group(1)))
    return words


class Known:
    def __init__(self, exclude=None):
        self.spell = load_dictionary()
        self.lex = build_lexicon(exclude)

    def __call__(self, w):
        w = w.lower().strip("'’")
        if not w or w.isdigit() or w in self.lex:
            return True
        if re.fullmatch(r"\d+(st|nd|rd|th|s)", w):  # 1st, 5th, 1990s
            return True
        if w.endswith(("'s", "’s")):
            return self(w[:-2])
        return bool(self.spell) and not self.spell.unknown([w])


def strip_markdown(line):
    """Blank out markdown syntax (keeping columns) so only the transcribed words remain."""
    if re.match(r"\s*(:::|<!--|\|?\s*:?-{2,})", line) or re.match(r"\s*!\[", line):
        return ""
    line = re.sub(r"\]\([^)]*\)", lambda m: " " * len(m.group()), line)  # link targets
    line = re.sub(r"<[^>]+>", lambda m: " " * len(m.group()), line)      # html tags
    line = re.sub(r"https?://\S+", lambda m: " " * len(m.group()), line)
    return re.sub(r"^\s*(#+|[-*+])\s", lambda m: " " * len(m.group()), line)


def one_swap(w, known):
    for a, b in SWAPS:
        for i in [m.start() for m in re.finditer(re.escape(a), w)]:
            cand = w[:i] + b + w[i + len(a):]
            if cand != w and re.search(r"[a-zA-Z]", cand) and known(cand):
                return cand
    return None


def is_swap(a, b):
    """True if one classic OCR confusion turns a into b (case-insensitive)."""
    a, b = a.lower(), b.lower()
    for x, y in SWAPS:
        i = a.find(x)
        while i >= 0:
            if a[:i] + y + a[i + len(x):] == b:
                return True
            i = a.find(x, i + 1)
    return False


SUFFIXES = {"ing", "ings", "ed", "er", "ers", "est", "ly", "ness", "ment", "ments", "able", "ion", "ions"}


def split_join(w, known):
    lw = w.lower()
    for i in range(2, len(lw) - 1):
        a, b = lw[:i], lw[i:]
        if b in SUFFIXES:
            continue
        if len(a) > 2 and len(b) > 2 and known(a) and known(b):
            return f"{w[:i]} {w[i:]}"
    return None


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("file")
    ap.add_argument("--extract", help="extract.json from extract_pdf.py, to add LOWCONF + crops")
    ap.add_argument("--no-unknown", action="store_true", help="hide UNKNOWN (show only likelier errors)")
    ap.add_argument("--summary", action="store_true")
    args = ap.parse_args()

    known = Known(exclude=args.file)
    low = collections.defaultdict(list)   # word -> low-confidence OCR entries
    if args.extract:
        for p in json.load(open(args.extract, encoding="utf-8"))["pages"]:
            for w in p.get("low_conf", []):
                for t in WORD.findall(w["word"]):
                    low[t.lower()].append(w)
    squash = lambda x: re.sub(r"\s+", " ", x).strip()

    def low_entry(tok, raw_line):
        """The low-confidence entry for this token on this line, if any."""
        for e in low.get(tok.lower(), []):
            if squash(e["line"]) and squash(e["line"]) in squash(raw_line):
                return e
        return None

    lines = open(args.file, encoding="utf-8").read().split("\n")
    hits = []

    def hit(n, col, code, tok, note=""):
        lc = low_entry(tok, lines[n - 1]) or next(iter(low.get(tok.lower(), [])), None)
        crop = f"  [{lc['crop']}]" if lc and lc.get("crop") else ""
        hits.append((n, col, code, tok, note, crop))

    in_comment = False
    for n, raw in enumerate(lines, 1):
        if "<!--" in raw and "-->" not in raw:
            in_comment = True
        if in_comment:
            in_comment = "-->" not in raw
            continue
        line = strip_markdown(raw)
        is_table = raw.lstrip().startswith("|")
        has_lower = bool(re.search(r"[a-z]", line))
        for m in re.finditer(r"\S+", line):
            t = m.group()
            junk = [c for c in t if c in JUNK_CHARS]
            if re.fullmatch(r"\|+", t):
                if not is_table:
                    hit(n, m.start() + 1, "JUNK", t, "-> I?  (OCR often reads I as |)")
            elif "|" in t and not is_table:
                hit(n, m.start() + 1, "JUNK", t, "-> " + t.replace("|", "I") + "?")
            elif junk and not (is_table and set(junk) == {"|"}):
                hit(n, m.start() + 1, "JUNK", t, "stray " + "".join(sorted(set(junk))))
            elif re.fullmatch(r"[^\w\s&\-–—.,:;'\"()£$%/*_\[\]?!+=@#<>©®]", t):
                hit(n, m.start() + 1, "JUNK", t, "lone symbol")
        toks = list(WORD.finditer(line))
        for i, m in enumerate(toks):
            t, col = m.group(), m.start() + 1
            bare = t.strip("'’")
            if re.search(r"[A-Za-z]\d|\d[A-Za-z]", bare) and not re.fullmatch(
                    r"(\d+(st|nd|rd|th|s|am|pm|hrs?|mm|kg|km|m|k)|[A-Z]{1,4}\d{2,}[A-Z]?|\d+[A-Z]{1,3}|[A-Z]+\d+[A-Z]*\d*)",
                    bare, re.I):
                hit(n, col, "MIXED", t, f"-> {one_swap(bare, known)}" if one_swap(bare, known) else "")
                continue
            if re.fullmatch(r"\d*[lOoIS]\d+|\d+[lOoIS]\d*", bare) and len(bare) >= 3:
                hit(n, col, "MIXED", t, "letter inside a number?")
                continue
            if len(bare) == 1 and (line[m.end():m.end() + 1] == "." or
                                   (bare.isupper() and line[m.end():m.end() + 1] in (",", ":", "*"))):
                continue  # initial/abbreviation: R.V., A.T., "Turner, J,", "Q:", censored F***
            if len(bare) == 1 and bare.lower() not in "aio" and not bare.isdigit() and bare not in "IAVX":
                hit(n, col, "JUNK", t, "lone letter")
                continue
            if i and toks[i - 1].group().lower() == t.lower() and t.lower() not in ("had", "that"):
                hit(n, col, "DOUBLE", t)
            if bare.isdigit() or known(bare):
                e = low_entry(t, raw)
                if e and e["conf"] < LOWCONF_FLAG:
                    hit(n, col, "LOWCONF", t, f"conf {e['conf']:.0f}")
                continue
            if has_lower and re.fullmatch(r"[A-Z]{2,5}s?", bare):
                continue  # acronym in mixed-case text (SFF, HAF, OPs)
            nxt = toks[i + 1].group() if i + 1 < len(toks) else ""
            if nxt and known(bare + nxt) and not known(nxt):
                hit(n, col, "SPLIT", t, f"-> {bare}{nxt}")
            elif (s := one_swap(bare, known)):
                hit(n, col, "OCR", t, f"-> {s}")
            elif len(bare) > 5 and not bare[0].isupper() and (s := split_join(bare, known)):
                hit(n, col, "JOINED", t, f"-> {s}")
            elif not args.no_unknown:
                hit(n, col, "UNKNOWN", t)
        if re.search(r"[A-Za-z]-\s*$", line) and n < len(lines) and re.match(r"\s*[a-z]", lines[n]):
            hit(n, len(line.rstrip()), "HYPHEN", line.rstrip().split()[-1],
                f"+ '{lines[n].split()[0]}'")

    order = ["JUNK", "MIXED", "OCR", "SPLIT", "JOINED", "HYPHEN", "DOUBLE", "LOWCONF", "UNKNOWN"]
    if args.summary:
        c = collections.Counter(h[2] for h in hits)
        print(", ".join(f"{k} {c[k]}" for k in order if c[k]) or "no suspects")
        return
    for n, col, code, tok, note, crop in hits:
        print(f"{n}:{col}\t{code:7} {tok}  {note}{crop}")
    c = collections.Counter(h[2] for h in hits)
    print(f"\n{len(hits)} suspects: " + ", ".join(f"{k} {c[k]}" for k in order if c[k]), file=sys.stderr)


if __name__ == "__main__":
    main()
