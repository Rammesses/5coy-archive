# OpenSpec: Corpus Cleanup — Canonical Area↔Path Mapping

| Field | Value |
|---|---|
| Issue | [#51](https://github.com/Rammesses/5coy-archive/issues/51) |
| Branch | `feature/51-corpus-cleanup` |
| Status | Draft |
| Authors | Rammesses |
| Created | 2026-05-17 |
| Keywords | RFC 2119 (SHALL / SHOULD / MAY / SHALL NOT / SHOULD NOT) |
| Blocks | [#50](https://github.com/Rammesses/5coy-archive/issues/50) production deployment |

---

## 1. Background

The 5coy-archive corpus grew organically across years of contributions. Article index files (`web/wwwroot/data/articles/*.json`) reference content under `web/wwwroot/content/` and PDFs under the media S3 bucket served via Cloudflare. The relationship between an "area" (one index file) and the content / media path prefixes it uses is not 1:1: areas overlap, root-level media paths mix with subfolder paths, folder names use a mixture of spaces, apostrophes and mixed-case spellings.

This is tolerable while every entry is hand-authored. It is incompatible with the ingestion pipeline specified in `docs/specs/50-document-ingestion.md`, which requires each ingest-bucket subfolder to map deterministically to a single content path and a single media path so that an automated commit lands the new article in exactly the right place.

---

## 2. Problem Statement

`#51`'s issue body inventories the path-prefix sprawl. The headline problems:

1. **Overlapping ownership.** `in-character.json` and `mission-reports.json` both reference `/content/mission-reports/` and `/media/Mission Reports/`. Three entries (`2492-10_mission-procyon-intel`, `2496-02_mission-nova_comms-001`, `2496-02_mission-intruder_comms-002`) appear *verbatim* in both files.
2. **Mixed canonical media paths.** `out-of-character.json` uses both `/media/` (root) and `/media/Out-Of-Character/`. `scenarios.json` uses both `/media/` (root) and `/media/Miscellanea/`.
3. **Display-name folders.** Media folder names include spaces and apostrophes (`Mission Reports`, `Marine's Handbook`, `Mexal's Letters`), forcing every `PdfUrl` to be percent-encoded and complicating automated routing.
4. **Hierarchical inconsistency.** `sections.json` nests `mission-reports` under `in-character`, but its content lives at the top-level `/content/mission-reports/`. Conversely the `out-of-character` landing page lives at `/content/out-of-character/out-of-character.md` while the `in-character` landing page lives at `/content/in-character.md`.
5. **Undeclared area.** `interstella` has no canonical media path because no Interstella! issue has ever been uploaded as PDF.
6. **Confused area.** `miscellanea` is a navigation child of `out-of-character` (per `sections.json`) with one inline article in `articles.json` and no separate index file, yet `/media/Miscellanea/` is where most `scenarios` PDFs live.
7. **Orphan content files.** Six markdown files exist under `web/wwwroot/content/` with no entry in any index file (listed in §10.3).
8. **Data-quality bugs** in the index files: title typo (`Grant"s World`), filename typo (`mision-vortex`), inconsistent `Reference`/`reference` JSON casing, PDF filename mismatching its entry's date (`mission-procyon-intel` dated 2492-10 references a `2496-00` PDF).

---

## 3. Goals

- **G1.** Each area in `web/wwwroot/data/articles/` SHALL have exactly one canonical content path and exactly one canonical media path.
- **G2.** A single markdown table (§7) SHALL define the area↔path mapping, suitable for `#50`'s routing-table population (`AreaTable.cs`).
- **G3.** Every change SHALL be reversible without data loss: legacy URLs continue to resolve via a Cloudflare worker 301 redirect to the new canonical URL.
- **G4.** The corpus SHALL be ingestible after this work: a contributor uploading `mission-report-foo.pdf` to `s3://<ingest>/in-character/mission-reports/` produces an article at the canonical mission-reports path, not at a legacy or ambiguous path.

## 4. Non-Goals

- This spec does not introduce new article content. Orphan content files (§10.3) are added to indexes only.
- This spec does not change the site's navigation structure (`sections.json` hierarchy is preserved).
- This spec does not change the Cloudflare worker's authentication, caching, or CORS behaviour; only adds 301-redirect rules for legacy `/media/` paths.
- This spec does not move the `web/wwwroot/data/` non-articles files (`missions.json`, `personnel.json`, `videos/`, `sections.json`).

---

## 5. Stakeholders

| Role | Interest |
|---|---|
| Project maintainer | Routing table is unambiguous; #50 can ship to production |
| #50 implementer (Haiku) | Has a single source of truth for area↔path mapping |
| Site readers | No broken bookmarks — old `/media/...` URLs continue to resolve |
| Contributors | Future PDF uploads land in predictable, kebab-case paths |

---

## 6. Decisions

The following decisions were made during the 2026-05-17 planning session and are recorded here so they are not re-litigated during implementation.

| # | Decision | Rationale |
|---|---|---|
| D-01 | **Media folder names normalised to kebab-case ASCII.** | Matches the area `Reference` field exactly. Eliminates URL-encoding. Simplifies #50's routing table. |
| D-02 | **PDF filenames also renamed to kebab-case ASCII.** | Clean URLs end-to-end. No percent-encoding anywhere. Matches the markdown-filename naming convention already in use under `web/wwwroot/content/`. |
| D-03 | **Media folders mirror content nesting.** | `/media/in-character/in-the-line-of-fire/` mirrors `/content/in-character/in-the-line-of-fire/`. Makes hierarchy visible in URLs. #50's ingest-bucket prefix scheme inherits the same nesting. |
| D-04 | **Content tree fixed to fully mirror `sections.json`.** | `mission-reports` content moves under `/content/in-character/mission-reports/`. `scenarios` content moves under `/content/out-of-character/scenarios/`. Everything is consistent end-to-end. |
| D-05 | **Area landing pages live at `/content/<area>.md` (content root).** | `/content/out-of-character/out-of-character.md` moves up to `/content/out-of-character.md` for consistency with `/content/in-character.md`. |
| D-06 | **De-duplicate `in-character` ↔ `mission-reports` overlap; cross-link by ID.** | Each document lives in exactly one index. The 2 comms messages and the intel briefing keep their `mission-reports.json` placement (as `Children:` of the relevant mission). `in-character.json` drops them. Cross-area references use the `MissionRef` field. |
| D-07 | **Keep `miscellanea` as a distinct area with its own canonical paths.** | Miscellanea gets `/content/out-of-character/miscellanea/` and `/media/out-of-character/miscellanea/`. Scenarios PDFs currently under `/media/Miscellanea/` move out to `/media/out-of-character/scenarios/`. The single legitimately-miscellanea entry (`prop-designs`) stays in miscellanea. |
| D-08 | **Legacy URL preservation via Cloudflare worker 301 Moved Permanently.** | The worker matches legacy paths and returns `301` with the canonical `Location`. Indefinite — no sunset date. Browsers and search engines update their references; existing bookmarks keep working forever. |
| D-09 | **S3 moves executed via generated `aws s3 mv` script.** | Single shell script under `scripts/` committed alongside the data changes. Idempotent. Defaults to `--dryrun`. User executes against live bucket after PR review. |
| D-10 | **Orphan content files assigned to their most-likely area; PR review adjudicates.** | The 6 orphan markdown files (§10.3) get stub entries in the index file closest to their content; PR review may move, retitle, or reject. |

---

## 7. Canonical Routing Table

This is the central deliverable. `#50`'s `AreaTable.cs` SHALL be populated from this table verbatim.

| Reference | Title | Parent | Data file | Content path | Media path | Ingest prefix | Landing page |
|---|---|---|---|---|---|---|---|
| `in-character` | In-Character | _(root)_ | `articles/in-character.json` | `/content/in-character/` | `/media/in-character/` | `in-character/` | `/content/in-character.md` |
| `in-the-line-of-fire` | In The Line Of Fire | `in-character` | `articles/in-the-line-of-fire.json` | `/content/in-character/in-the-line-of-fire/` | `/media/in-character/in-the-line-of-fire/` | `in-character/in-the-line-of-fire/` | `/content/in-the-line-of-fire.md` |
| `interstella` | Interstella! | `in-character` | `articles/interstella.json` | `/content/in-character/interstella/` | `/media/in-character/interstella/` | `in-character/interstella/` | `/content/interstella.md` |
| `marines-handbook` | Marine's Handbook | `in-character` | `articles/marines-handbook.json` | `/content/in-character/marines-handbook/` | `/media/in-character/marines-handbook/` | `in-character/marines-handbook/` | `/content/marines-handbook.md` |
| `mexals-letters` | Mexal's Letters Home | `in-character` | `articles/mexals-letters.json` | `/content/in-character/mexals-letters/` | `/media/in-character/mexals-letters/` | `in-character/mexals-letters/` | `/content/mexals-letters.md` |
| `mission-reports` | Mission Reports | `in-character` | `articles/mission-reports.json` | `/content/in-character/mission-reports/` | `/media/in-character/mission-reports/` | `in-character/mission-reports/` | `/content/mission-reports.md` |
| `out-of-character` | Out of Character | _(root)_ | `articles/out-of-character.json` | `/content/out-of-character/` | `/media/out-of-character/` | `out-of-character/` | `/content/out-of-character.md` |
| `miscellanea` | Miscellanea | `out-of-character` | `articles/miscellanea.json` | `/content/out-of-character/miscellanea/` | `/media/out-of-character/miscellanea/` | `out-of-character/miscellanea/` | `/content/miscellanea.md` |
| `scenarios` | Scenarios | `out-of-character` | `articles/scenarios.json` | `/content/out-of-character/scenarios/` | `/media/out-of-character/scenarios/` | `out-of-character/scenarios/` | `/content/scenarios.md` |

**Reading rules:**
- The **content path** is the directory under `web/wwwroot/` where the area's article markdown files live. All `ContentUrl` strings in that area's data file SHALL begin with this prefix.
- The **media path** is the URL prefix served by the Cloudflare worker. All `PdfUrl` strings in that area's data file SHALL begin with this prefix.
- The **ingest prefix** is the S3 key prefix in the `#50` ingest bucket that routes uploads to this area. Longest-prefix-match SHALL be used (an upload to `in-character/mission-reports/foo.pdf` routes to `mission-reports`, not `in-character`).
- The **landing page** is the markdown file rendered for the area's nav entry per `sections.json`.

---

## 8. Functional Requirements

### 8.1 Path Normalisation

**FR-01** Every area listed in §7 SHALL have its content files moved to the canonical content path. `git mv` SHALL be used so move history is preserved.

**FR-02** Every area listed in §7 SHALL have its media files moved to the canonical media path. The generated migration script (FR-13) SHALL perform these moves on the S3 media bucket.

**FR-03** Folder and filename normalisation SHALL apply kebab-case ASCII: lowercase letters, digits, hyphens. Apostrophes, spaces, ampersands and other non-ASCII characters SHALL be stripped or replaced with hyphens. Underscores in existing filenames SHALL be preserved only where they separate metadata segments (e.g., `mexals-letters_2495-05_mariecelesta.md`).

**FR-04** Every `ContentUrl` and `PdfUrl` string in `web/wwwroot/data/articles/*.json` SHALL be rewritten to reference the new canonical paths.

### 8.2 De-Duplication

**FR-05** The following entries SHALL be removed from `in-character.json`, retaining only their occurrence in `mission-reports.json`:
- `2492-10_mission-procyon-intel` (currently top-level in `in-character.json`, also a `Children:` entry under `2492-10_mission-procyon` in `mission-reports.json`)
- `2496-02_mission-nova_comms-001` (currently top-level in `in-character.json`, also a `Children:` entry under `2495-11_mission-nova` in `mission-reports.json`)
- `2496-02_mission-intruder_comms-002` (currently top-level in `in-character.json`, also a `Children:` entry under `2496-02_mission-intruder` in `mission-reports.json`)

**FR-06** Where a child entry under a mission-reports parent uses a content file that lives outside the mission-reports content path (e.g., a comms message under `/content/in-character/`), the content file SHALL be moved into the parent area (`in-character/mission-reports/`) and its `ContentUrl` updated. The duplicate is dropped from `in-character.json`. No content file SHALL be referenced from two areas after this work.

### 8.3 Index Files

**FR-07** A new file `web/wwwroot/data/articles/miscellanea.json` SHALL be created containing the `prop-designs` entry currently inlined in `articles.json`. `articles.json` SHALL be updated to reference the new data file (`"Data": "/data/articles/miscellanea.json"`) instead of holding the inline `Articles` array.

**FR-08** All article entries SHALL use `"Reference"` (capital R) as the JSON field key. Existing `"reference"` (lowercase) keys in `scenarios.json` SHALL be normalised.

**FR-09** The orphan content files listed in §10.3 SHALL each receive a stub entry in the index file proposed in §10.3. The entry SHALL have empty `PdfUrl` if no PDF exists. PR review SHALL adjudicate area assignment.

### 8.4 Data-Quality Fixes

**FR-10** The following data-quality bugs SHALL be corrected:
- `mexals-letters.json` entry `mexals-letters_2495-01_grants-world`: Title `2495-01 - Grant"s World` → `2495-01 - Grant's World`.
- `in-character.json` entry `briefing-notes_mision-vortex`: Reference and ContentUrl filename `mision-vortex` → `mission-vortex`. The content file SHALL be renamed accordingly.
- `mission-reports.json` entry `2492-10_mission-procyon-intel`: PdfUrl currently references `2496-00+Mission+%22Procyon%22+-+Intel.pdf`; the canonical filename SHALL match the entry's date (`2492-10-mission-procyon-intel.pdf`).
- `scenarios.json`: typo `Welcome-to-the-Marie-Celeste.pdf` (Celeste) SHALL match the markdown-filename spelling `marie-celesta` (Celesta).

### 8.5 Legacy URL Compatibility

**FR-11** The Cloudflare worker fronting the media bucket SHALL respond to requests for any pre-cleanup `/media/...` path with HTTP `301 Moved Permanently` and a `Location` header pointing to the canonical post-cleanup URL.

**FR-12** The legacy→canonical mapping used by FR-11 SHALL be table-driven: a single data structure inside the worker (or fetched at worker init) derived from §7 and the per-file rename map produced during implementation. There SHALL NOT be hand-written `if`/`else` chains per legacy path.

### 8.6 Migration Script

**FR-13** A single shell script `scripts/migrate-media-paths.sh` SHALL be committed alongside the data changes. It SHALL:
- Accept the S3 media-bucket name as a positional argument.
- Default to `--dry-run` mode; require an explicit `--execute` flag for live moves.
- Use `aws s3 mv` (not `cp` + `rm`) so the operation is atomic per object.
- Be idempotent: running it twice (in `--execute` mode) against a partially-migrated bucket SHALL complete without error and SHALL NOT duplicate or delete already-migrated objects.
- Print a final verification summary listing any objects still at legacy paths after the run.

---

## 9. Non-Functional Requirements

**NFR-01** No data loss. Every existing PDF in the media bucket SHALL be accessible at *some* URL (legacy via 301, canonical directly) at every point during and after migration.

**NFR-02** PR-reviewable. All local file changes (content moves, JSON rewrites, script, worker changes) SHALL fit in a single PR small enough to review by hand. The PR description SHALL link to §7 for reviewers to validate the routing table against the diff.

**NFR-03** No build regression. The .NET 10 web project SHALL build cleanly after the changes; existing tests (if any) SHALL pass; existing pages SHALL render the same articles with the new canonical paths.

**NFR-04** Backwards compatibility window: indefinite. The 301 redirects SHALL NOT be sunset on a fixed date.

---

## 10. Inventory

### 10.1 Path migration summary

| From | To | Notes |
|---|---|---|
| `/content/mission-reports/` | `/content/in-character/mission-reports/` | 13 markdown files |
| `/content/scenarios/` | `/content/out-of-character/scenarios/` | 14 markdown files |
| `/content/out-of-character/out-of-character.md` | `/content/out-of-character.md` | Landing page to root |
| `/media/Mission Reports/` | `/media/in-character/mission-reports/` | All PDFs |
| `/media/In-Character/` | `/media/in-character/` | All PDFs |
| `/media/In-Character/InTheLineOfFire/` | `/media/in-character/in-the-line-of-fire/` | All PDFs |
| `/media/Marine's Handbook/` | `/media/in-character/marines-handbook/` | All PDFs |
| `/media/Mexal's Letters/` | `/media/in-character/mexals-letters/` | All PDFs |
| `/media/Out-Of-Character/` | `/media/out-of-character/` | All PDFs |
| `/media/Miscellanea/Prop-Designs.pdf` | `/media/out-of-character/miscellanea/prop-designs.pdf` | Single file |
| `/media/Miscellanea/*` (others) | `/media/out-of-character/scenarios/...` | The marie-celesta set, drones, planet, scenario-summaries |
| `/media/Drive-Wars.pdf`, `/media/Drive-Wars-II.pdf` | `/media/out-of-character/scenarios/drive-wars.pdf`, `.../drive-wars-2.pdf` | Root → canonical |
| `/media/A+Brief+Guide+to+History.pdf`, etc. | `/media/out-of-character/...` | OOC PDFs at root |

The complete per-file rename map is generated by the migration script (FR-13) and embedded in the worker (FR-12).

### 10.2 De-duplication targets

| Reference | Currently in | Authoritative location after |
|---|---|---|
| `2492-10_mission-procyon-intel` | `in-character.json` (top-level) + `mission-reports.json` (child) | `mission-reports.json` only |
| `2496-02_mission-nova_comms-001` | `in-character.json` (top-level) + `mission-reports.json` (child) | `mission-reports.json` only |
| `2496-02_mission-intruder_comms-002` | `in-character.json` (top-level) + `mission-reports.json` (child) | `mission-reports.json` only |

### 10.3 Orphan content files

Six markdown files exist in `web/wwwroot/content/` with no entry in any `articles/*.json`. Proposed area assignments (subject to PR review per FR-09):

| File | Proposed area | Notes |
|---|---|---|
| `in-character/general-orders_2602-05-16.md` | `in-character` | In-character document; date suggests modern era continuity |
| `out-of-character/cmc_newsletter_1999-02.md` | `out-of-character` | OOC historical newsletter |
| `out-of-character/honours_1995-08-23.md` | `out-of-character` | OOC historical |
| `out-of-character/interim-history_2496-to-2501.md` | `out-of-character` | OOC historical summary |
| `out-of-character/mission-briefing_operation-hammerhead_2501-10.md` | `out-of-character` | OOC mission notice (real-world event), not in-character mission report |
| `out-of-character/mission-notice_operation-dawn_1999-01.md` | `out-of-character` | OOC mission notice (real-world event) |

---

## 11. Open Questions

None at planning checkpoint. Decisions D-01 through D-10 close every open question identified during inventory.

Issues that may surface during implementation and are explicitly deferred to PR review:

- **OQ-implementation-01.** Per-file canonical PDF filename. The migration script generates these from the source filename by lowercasing, replacing `+`/space/`%XX`/apostrophe with `-`, and collapsing repeats. PR review may rename specific files if the generated name is unreadable.
- **OQ-implementation-02.** Orphan area assignments per FR-09 / §10.3. PR review may move entries to different areas.

---

## 12. Acceptance Criteria

The work is complete when:

- **AC-01** Every row of §7's routing table is satisfied by the repository state on `feature/51-corpus-cleanup`.
- **AC-02** `grep -RE '"ContentUrl"|"PdfUrl"' web/wwwroot/data/articles/` returns no path beginning with a legacy prefix (`/media/Mission Reports/`, `/media/In-Character/`, `/media/Marine's Handbook/`, `/media/Mexal's Letters/`, `/media/Out-Of-Character/`, `/media/Miscellanea/`, or any path beginning `/media/<capital-letter>` or `/content/mission-reports/` or `/content/scenarios/` at top level).
- **AC-03** Every PR review for the orphans (§10.3) has been actioned (entry accepted, moved, or rejected).
- **AC-04** The migration script runs successfully in `--dry-run` mode against the live media bucket and reports the expected number of moves; no unexpected residue.
- **AC-05** The Cloudflare worker, deployed in a staging configuration, returns `301` for every legacy URL listed in §10.1 with a `Location` matching the canonical URL.
- **AC-06** The .NET web project builds cleanly; a manual smoke-test of one article per area renders the markdown and serves the PDF via the canonical URL.
- **AC-07** `#50`'s `AreaTable.cs` (when written) is populatable from §7 without any data not present in §7.

---

## 13. References

- Companion Kiro spec: `.kiro/specs/corpus-cleanup/{requirements,design,tasks}.md`
- Blocked work: `docs/specs/50-document-ingestion.md`
- GitHub issue: <https://github.com/Rammesses/5coy-archive/issues/51>
