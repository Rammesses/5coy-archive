# CLAUDE.md — 5coy-archive

## Git rules (mandatory)

- **Do NOT commit, amend, stash, rebase, merge or push any changes until Joel has reviewed them.**
  Make changes in the working tree only, summarise the diff, and wait for explicit approval
  before running any `git commit` (or any other command that writes history or touches a remote).

## Skills

- `.claude/skills/crossref-archive-data` — audit/fix cross-references between `wwwroot/data` JSON and `wwwroot/content`.
  Run its checker: `python3 .claude/skills/crossref-archive-data/scripts/check_crossrefs.py web/wwwroot`
- `.claude/skills/write-mission-summary` - draft the Mission Summary markdown shown on a mission page
  (third person, Joel's voice, character names only, with links) from the archive's articles, then link it via `ContentUrl` in `missions.json`.
- `.claude/skills/transcribe-pdf-resource` - transcribe a PDF with no markdown (local Tesseract OCR for scans),
  find and fix transcription errors, and re-verify existing markdown against its PDF. Scratch and state live in `.transcribe/`.

## Data layout notes

- `web/wwwroot/data/articles.json` lists article groups; each group either inlines `Articles`
  or points at a `Data` file under `data/articles/`. Group `Reference` must match a section
  in `data/sections.json`.
- Articles (`Models/Article.cs`): `Reference`, `Title`, `ContentUrl`, `PdfUrl`, `MissionRef`, `Children`.
  `ContentUrl` must point at an existing file under `wwwroot/content/`; PDFs live in S3
  (`/media/*`), so they can't be checked locally.
- `MissionRef` must match a `Reference` in `data/missions.json` (not an article reference).
  `ArticlesService.GetItemsByMissionRef` only searches **top-level** articles, not `Children`.
- Every mission was a weekend event: it **started on a Friday night and finished on Sunday afternoon**.
  So `StartDate` is a Friday and `EndDate` the Sunday after it. When a source gives only one date, or
  dates that don't fall Fri-Sun, extrapolate to the weekend that contains it. Mission reports are canon
  for which weekend. Dates in the days after (e.g. Mexal's letters, typically dated the Monday after)
  point to the weekend just before; a briefing dated midweek points to the weekend just after. This
  applies to real-world dates (pre-1997). Post-jump missions are stored as in-game dates, so the rule
  doesn't apply to them.
- Missions are displayed as `EndDate.AddYears(500)`, so post-1996 events (after the in-game
  2496→2501 jump) are stored as in-game year minus 500 (e.g. Strongarm 2502 → `2002-05-01`).
- `data/personnel.json` (`Name`, `PdfUrl`, `ArticleRef`) is not read by the app yet.
