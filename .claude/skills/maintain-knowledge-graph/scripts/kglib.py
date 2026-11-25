"""Shared loading helpers for the 5coy-archive knowledge graph (web/wwwroot/data/kg)."""
import json, os

KG_FILES = {"character": "characters.json", "organisation": "organisations.json"}

def load_json(path):
    with open(path, encoding="utf-8") as fh:
        return json.load(fh)

def iter_articles(arts):
    for a in arts or []:
        yield a
        yield from iter_articles(a.get("Children"))

def load_archive(wwwroot):
    """Return (articles, missions): dicts of Reference -> record."""
    data = os.path.join(wwwroot, "data")
    articles = {}
    for g in load_json(os.path.join(data, "articles.json")):
        arts = g.get("Articles") or []
        if g.get("Data"):
            x = load_json(os.path.join(wwwroot, g["Data"].lstrip("/")))
            arts = x if isinstance(x, list) else x.get("Articles", [])
        for a in iter_articles(arts):
            ref = a.get("Reference") or a.get("reference")
            if ref: articles[ref] = a
    missions = {m["Reference"]: m for m in load_json(os.path.join(data, "missions.json")) if m.get("Reference")}
    return articles, missions

def load_videos(wwwroot):
    """Return dict of Reference -> video record from data/videos.json and its group Data files."""
    data = os.path.join(wwwroot, "data")
    videos = {}
    p = os.path.join(data, "videos.json")
    if not os.path.exists(p): return videos
    for g in load_json(p):
        vids = list(g.get("Videos") or [])
        if g.get("Data"):
            f = os.path.join(wwwroot, g["Data"].lstrip("/"))
            if os.path.exists(f) and open(f, encoding="utf-8").read().strip():
                vids += load_json(f)
        for v in vids:
            if v.get("Reference"): videos[v["Reference"]] = v
    return videos

def load_kg(wwwroot):
    """Return dict of 'kind/Reference' -> entity (with '_kind' added)."""
    kgdir = os.path.join(wwwroot, "data", "kg")
    out = {}
    for kind, fn in KG_FILES.items():
        p = os.path.join(kgdir, fn)
        if not os.path.exists(p): continue
        for e in load_json(p):
            e["_kind"] = kind
            out[f"{kind}/{e['Reference']}"] = e
    return out

def content_ref_map(wwwroot):
    """Map 'content/...md' (relative to wwwroot) -> list of 'article/..' / 'mission/..' / 'video/..' refs."""
    articles, missions = load_archive(wwwroot)
    m = {}
    for ref, a in articles.items():
        cu = (a.get("ContentUrl") or "").lstrip("/")
        if cu: m.setdefault(cu, []).append("article/" + ref)
    for ref, x in missions.items():
        cu = (x.get("ContentUrl") or "").lstrip("/")
        if cu: m.setdefault(cu, []).append("mission/" + ref)
    for ref, v in load_videos(wwwroot).items():
        cu = (v.get("ContentUrl") or "").lstrip("/")
        if cu: m.setdefault(cu, []).append("video/" + ref)
    return m
