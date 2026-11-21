# CLAUDE.md — 5coy-archive

## Git rules (mandatory)

- **Do NOT commit, amend, stash, rebase, merge or push any changes until Joel has reviewed them.**
  Make changes in the working tree only, summarise the diff, and wait for explicit approval
  before running any `git commit` (or any other command that writes history or touches a remote).

## Skills

- `.claude/skills/crossref-archive-data` — audit/fix cross-references between `wwwroot/data` JSON and `wwwroot/content`.
  Run its checker: `python3 .claude/skills/crossref-archive-data/scripts/check_crossrefs.py web/wwwroot`

## Data layout notes

- `web/wwwroot/data/articles.json` lists article groups; each group either inlines `Articles`
  or points at a `Data` file under `data/articles/`. Group `Reference` must match a section
  in `data/sections.json`.
- Articles (`Models/Article.cs`): `Reference`, `Title`, `ContentUrl`, `PdfUrl`, `MissionRef`, `Children`.
  `ContentUrl` must point at an existing file under `wwwroot/content/`; PDFs live in S3
  (`/media/*`), so they can't be checked locally.
- `MissionRef` must match a `Reference` in `data/missions.json` (not an article reference).
  `ArticlesService.GetItemsByMissionRef` only searches **top-level** articles, not `Children`.
- Missions are displayed as `EndDate.AddYears(500)`, so post-1996 events (after the in-game
  2496→2501 jump) are stored as in-game year minus 500 (e.g. Strongarm 2502 → `2002-05-01`).
- `data/personnel.json` (`Name`, `PdfUrl`, `ArticleRef`) is not read by the app yet.
