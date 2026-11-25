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

- `.claude/skills/maintain-knowledge-graph` - keep the people/organisations knowledge graph in `web/wwwroot/data/kg`
  current when content changes, and use it to answer questions about the archive.
  Validate with `python3 .claude/skills/maintain-knowledge-graph/scripts/check_kg.py web/wwwroot`,
  then regenerate with `build_generated.py`. Run it after transcribing or summarising anything.

## Mission links (mandatory)

- **ALWAYS** write the text of a link to a mission page as
  `Mission '{mission_name}' - {month} {year} ({planet})`, e.g.
  `[Mission 'Nova' - November 2495 (Liahn)](/mission/2495-11_mission-nova)`. This applies everywhere:
  mission summaries, transcriptions (including personnel-jacket "Missions Attended" lists), notes, and
  Razor templates.
  - Build it from `missions.json`, never from the wording of the source: `Name` as-is (it already reads
    `Mission '...'`, `Operation '...'` or `Mission Unknown`), `EndDate` + 500 years as the full month
    name and in-game year, and `Planet` in brackets (left out when Planet is empty, `None` or `?`).
  - In Razor use `Mission.LinkText` (`web/Models/Mission.cs`); don't format mission names or dates by hand.
  - In markdown, check with `python3 .claude/skills/crossref-archive-data/scripts/mission_links.py web/wwwroot`
    and rewrite with `--fix`. Run it after any edit to content or to a mission's Name, EndDate or Planet.
  - The mission table on /missions (`MissionTableRow.razor`) is the one exception: it shows date, name and
    planet in separate columns, so its link text is just the Name.

## Data layout notes

- `web/wwwroot/data/articles.json` lists article groups; each group either inlines `Articles`
  or points at a `Data` file under `data/articles/`. Group `Reference` must match a section
  in `data/sections.json`.
- Articles (`Models/Article.cs`): `Reference`, `Title`, `ContentUrl`, `PdfUrl`, `MissionRef`, `Related`, `Children`.
  `MissionRef` is only for the mission an article actually reports on (it lists the article on the mission page).
  Background, build-up or follow-up (e.g. a newsletter previewing the next mission) goes in `Related`: an array
  of article and/or mission References, shown under "Related Articles" at the bottom of the article page
  (both directions for articles).
- Missions (`Models/Mission.cs`) also have `Related`: every mission and article that the mission's summary
  (`ContentUrl`) links to must be listed there (except articles whose `MissionRef` is already that mission),
  so cross-references show on both pages, not only in the summary text. The checker enforces this.
- Missions whose codename isn't known are named "Mission Unknown" with Reference `<yyyy-mm>_mission-unknown`;
  the date prefix de-conflicts them.
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
- `data/kg/` (characters.json, organisations.json) is the curated knowledge graph; `data/kg/generated/` is
  rebuilt by script. Graph IDs are type-prefixed (`character/`, `organisation/`, `mission/`, `article/`) because
  article and mission References can collide. Not read by the app yet.
