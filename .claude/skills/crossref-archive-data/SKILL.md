---
name: crossref-archive-data
description: Audit and fix cross-references between the 5coy-archive JSON data (articles.json and data/articles/*.json, missions.json, personnel.json) and the markdown under wwwroot/content. Use when new content is added, when asked to check/fix cross-references, or before reviewing data changes.
---

# Cross-reference the archive data

Keeps `web/wwwroot/data` consistent with `web/wwwroot/content`. Follow the steps in order.
**Never commit** — leave every change in the working tree for Joel to review (see CLAUDE.md).

## The schema (source of truth = `web/Models/*.cs`)

| File | Shape | Key rules |
|---|---|---|
| `data/sections.json` | `Section`: Reference, Title, Glyph, ContentUrl, NavigateUrl, Children | Section `ContentUrl`s are landing pages, not articles |
| `data/articles.json` | `ArticleGroup[]`: Reference, `Articles` (inline) and/or `Data` (path to `data/articles/<group>.json`) | Group Reference must match a section Reference |
| `data/articles/*.json` | `Article[]`: Reference, Title, ContentUrl, PdfUrl, MissionRef, Related, Children | Reference unique (except deliberate duplicates, below); ContentUrl must exist; MissionRef must be a **mission** Reference and only for the mission the article reports on; `Related` (array of article or mission References) for background/build-up/follow-ups |
| `data/missions.json` | `Mission[]`: Reference, Name, StartDate, EndDate, Location, Planet, Summary, ContentUrl, Related | Reference = `<in-game yyyy-mm>_<kebab-name>`, or `<yyyy-mm>_mission-unknown` (Name "Mission Unknown") when the codename isn't known; `Related` must list every mission/article the summary (`ContentUrl`) links to, except articles whose MissionRef is this mission |
| `data/personnel.json` | Name (`"Surname, I"`), PdfUrl, ArticleRef | Not read by the app yet; ArticleRef → the personnel-jacket article |

How the app behaves (`web/Services/ArticlesService.cs`, `MissionTableRow.razor`):
- JSON is deserialised case-insensitively, so `reference` and `Reference` both work. Match the casing of the file you're editing.
- `GetItemsByMissionRef` only scans **top-level** articles. A child article only appears on a mission page if a top-level copy exists. That's why In-Character has duplicate copies of some Mission Reports children. Those duplicates are intentional: keep them, and give both copies the same MissionRef.
- Every (pre-1997) mission was a weekend event, **Friday night to Sunday afternoon**: `StartDate` is a Friday and `EndDate` the Sunday after it. Mission reports are canon for which weekend. Extrapolate from any single date: a date inside the weekend gives that weekend, a letter or report dated in the days after (Mexal's letters are usually dated the Monday after) gives the weekend before, and a briefing sent midweek gives the weekend after. The checker warns about pre-2000 missions that aren't Fri-Sun.
- Mission dates are displayed as `EndDate.AddYears(500)`. After the in-game 2496 → 2501 jump, store dates as **in-game year − 500** (e.g. Operation Dawn, Jan 2502 → `2002-01-29`), not the real-world date.
- PDFs (`/media/...`) are served from S3, so they can't be verified locally. Don't invent PdfUrls: use `""` when unknown.

## Steps

1. **Run the checker** from the repo root (in device_bash, the repo is at `$HOME/mnt/5coy-archive`):
   `python3 .claude/skills/crossref-archive-data/scripts/check_crossrefs.py web/wwwroot`
   ERROR lines must be fixed. WARN lines need judgement.
2. **Unreferenced content**: read the head of each file and add an Article to the right group file:
   - `content/in-character/**` → `in-character.json`, or the matching sub-group (marines-handbook, mexals-letters, in-the-line-of-fire, interstella)
   - `content/mission-reports/**` → `mission-reports.json`, as a child of the mission's report if it's an attachment
   - `content/out-of-character/**` → `out-of-character.json`; `content/scenarios/**` → `scenarios.json`
   - Reference = file stem (kebab-case). Title follows the group's existing style, e.g. `"2495-07 - Mission 'Omaha' (Calvin)"` or `"Comms Message - yyyy-mm-dd - A to B (Mission 'X')"`.
3. **Missing ContentUrl targets**: look for a near-miss filename first (`find content -iname '*keyword*'`) and correct the URL. If there's no file, set `ContentUrl: ""` (the article stays PDF-only) and report it.
4. **MissionRefs**: link an article to a mission when there's a clear match from its date plus planet/name. Use the mission's Planet and Name, Mexal's letters' planet names, and mission names in titles and filenames. Common mistake: pointing at an *article* reference (e.g. `…boarding-report…`) instead of the mission (`…boarding-action…`). If the match is uncertain, leave it blank and list it for Joel.
5. **Missions**: add a mission when content clearly describes an event that isn't listed (joining instructions, notices, newsletters, in-character orders), following the date convention above. Fix obvious typos (EndDate before StartDate, or in a different year), and snap dates to their Fri-Sun weekend using the rule above. Don't rename existing mission References without flagging it, because they're URLs (`/mission/<ref>`).
6. **Personnel**: every `personnel-jacket_*` article needs a `personnel.json` entry with `ArticleRef`. A jacket PDF listed in personnel.json with no article should get a PDF-only article in `in-character.json`.
7. **Mission link text**: run `python3 .claude/skills/crossref-archive-data/scripts/mission_links.py web/wwwroot`.
   Every `/mission/` link must read `Mission '{name}' - {month} {year} ({planet})` (see CLAUDE.md); `--fix`
   rewrites them from missions.json.
8. **Personal data and links**: run `python3 .claude/skills/crossref-archive-data/scripts/check_privacy_links.py web/wwwroot`
   and fix everything it reports as CLAUDE.md describes (redact OOC contact details; check each link and
   add a dated "link checked/dead" marker).
9. **Edit surgically**: use exact-string replacement (python with `assert s.count(old) == 1`), *not* `json.dump`, to keep the diff reviewable. Then re-run the checker until it reports 0 errors, and run `git diff --stat`.
10. **Report back**: list what changed, grouped (fixed / linked / added), plus everything left for Joel's judgement (uncertain MissionRefs, ref/date mismatches, empty files, renames that would change URLs). Don't commit.
