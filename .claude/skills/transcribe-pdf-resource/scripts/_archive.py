"""Shared helpers for the transcribe-pdf-resource scripts.

Knows where the repo, the archive data and the scratch area live, how to walk the
article data, and how to find a local copy of a `/media/...` PDF.
"""
import json, os, re, urllib.parse, urllib.request

SKILL_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = os.path.abspath(os.path.join(SKILL_DIR, "..", "..", ".."))
WWWROOT = os.path.join(REPO, "web", "wwwroot")
# Scratch area: inside the repo (so page images can be viewed) but git-ignored.
WORK = os.path.join(REPO, ".transcribe")
# Local mirror of the S3 /media tree. Override with FIVECOY_MEDIA_DIR.
MEDIA_DIR = os.environ.get("FIVECOY_MEDIA_DIR") or os.path.join(WORK, "media")
# Hosts that serve /media/... (Cloudflare worker -> CloudFront origin).
MEDIA_HOSTS = ["https://www.5coy-cmc.org.uk", "https://dxa4dl52uee85.cloudfront.net"]
# Committed state (un-ignored in .gitignore), kept outside .claude so remote sessions can update it.
VERIFY_LOG = os.path.join(WORK, "verification-log.md")
LEXICON = os.path.join(WORK, "lexicon.txt")


def _lower(d):
    return {k.lower(): v for k, v in d.items()}


def load_json(rel):
    path = os.path.join(WWWROOT, rel.lstrip("/"))
    with open(path, encoding="utf-8") as f:
        text = f.read()
    return json.loads(text) if text.strip() else []


def articles():
    """Every article (children included) as a flat list of dicts."""
    out = []

    def walk(lst, group, parent):
        for a in lst or []:
            a = _lower(a)
            out.append(dict(kind="article", group=group, parent=parent,
                            reference=a.get("reference") or "",
                            title=a.get("title") or "",
                            content_url=a.get("contenturl") or "",
                            pdf_url=a.get("pdfurl") or "",
                            mission_ref=a.get("missionref") or ""))
            walk(a.get("children"), group, a.get("reference"))

    for g in load_json("data/articles.json"):
        g = _lower(g)
        walk(g.get("articles"), g.get("reference"), None)
        if g.get("data"):
            walk(load_json(g["data"]), g.get("reference"), None)
    return out


def resources():
    """Articles with a PdfUrl, plus personnel.json PDFs that no article carries."""
    arts = [a for a in articles() if a["pdf_url"]]
    seen = {a["pdf_url"] for a in arts}
    for p in load_json("data/personnel.json"):
        p = _lower(p)
        url = p.get("pdfurl") or ""
        if url and url not in seen:
            arts.append(dict(kind="personnel", group="personnel", parent=None,
                             reference=p.get("articleref") or slug(p.get("name", "")),
                             title=p.get("name", ""), content_url="", pdf_url=url,
                             mission_ref=""))
    return arts


def find(ref):
    for r in resources():
        if r["reference"].lower() == ref.lower():
            return r
    return None


def slug(s):
    return re.sub(r"[^a-z0-9]+", "-", s.lower()).strip("-")


def content_path(content_url):
    """Absolute path of a ContentUrl ('' if none)."""
    return os.path.join(WWWROOT, content_url.lstrip("/")) if content_url else ""


def media_candidates(pdf_url):
    """Local paths a /media/... URL might be mirrored at (raw and URL-decoded names)."""
    rel = pdf_url.split("/media/", 1)[-1]
    names = {rel, urllib.parse.unquote(rel), urllib.parse.unquote_plus(rel)}
    return [os.path.join(MEDIA_DIR, n) for n in sorted(names)]


def local_pdf(pdf_url, download=True):
    """Return a local path for the PDF, downloading into MEDIA_DIR if possible.

    Returns (path, None) or (None, message explaining where to put the file).
    """
    if os.path.isfile(pdf_url):
        return pdf_url, None
    for c in media_candidates(pdf_url):
        if os.path.isfile(c):
            return c, None
    target = media_candidates(pdf_url)[0]
    errors = []
    if download and pdf_url.startswith("/media/"):
        for host in MEDIA_HOSTS:
            try:
                req = urllib.request.Request(host + pdf_url, headers={"User-Agent": "5coy-transcribe"})
                with urllib.request.urlopen(req, timeout=30) as r:
                    data = r.read()
                if not data.startswith(b"%PDF"):
                    raise ValueError("response is not a PDF")
                os.makedirs(os.path.dirname(target), exist_ok=True)
                with open(target, "wb") as f:
                    f.write(data)
                return target, None
            except Exception as e:  # network blocked, 404, ...
                errors.append(f"{host}: {e}")
    msg = (f"No local copy of {pdf_url}.\n"
           + "".join(f"  download failed - {e}\n" for e in errors)
           + f"  Put the PDF at: {target}\n"
           + "  (or set FIVECOY_MEDIA_DIR to a folder mirroring the /media tree,"
             " or pass the PDF path directly)")
    return None, msg


def last_verified():
    """{reference: (date, result)} from the verification log."""
    out = {}
    if os.path.isfile(VERIFY_LOG):
        for line in open(VERIFY_LOG, encoding="utf-8"):
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) >= 3 and re.match(r"\d{4}-\d\d-\d\d", cells[0]):
                out[cells[1]] = (cells[0], cells[2])
    return out
