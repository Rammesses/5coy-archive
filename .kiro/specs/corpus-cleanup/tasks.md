# Tasks: Corpus Cleanup

**Feature:** Corpus Cleanup — Canonical Area↔Path Mapping
**Issue:** #51
**Spec:** `docs/specs/51-corpus-cleanup.md`
**Requirements:** `.kiro/specs/corpus-cleanup/requirements.md`
**Design:** `.kiro/specs/corpus-cleanup/design.md`

---

## Phase 1 — Generators

Build the tooling that derives the rename map and the rewritten data files.

### T-01 — Write `scripts/generate-rename-map.py`

- Input: `web/wwwroot/data/articles/*.json` (current state), the routing table from OpenSpec §7, optional `scripts/rename-overrides.json`.
- Output: `scripts/rename-map.json` per design §2.2.
- Implements the algorithm in design §2.1.
- Round-trip safe: re-running over already-canonical inputs produces an empty `moves` array.

**Satisfies:** Requirement 2.

### T-02 — Write `scripts/rename-overrides.json` (initial entries)

- Single entry initially: marie-celeste → marie-celesta correction (design §3.1).
- Reviewers may add entries during PR review.

**Satisfies:** Requirement 7 AC-5.

### T-03 — Generate `scripts/rename-map.json`

- Run T-01; commit the output.
- Sanity-check: row count matches the union of all current `PdfUrl` values (minus empty strings).

**Satisfies:** evidence for Requirements 2, 8.

### T-04 — Write `scripts/rewrite-article-urls.py`

- Input: `scripts/rename-map.json`, all `articles/*.json`.
- Operations per design §4:
  - Apply rename map to every `ContentUrl` and `PdfUrl`.
  - Move content-path prefixes to canonical (e.g., `/content/mission-reports/` → `/content/in-character/mission-reports/`).
  - Normalise field keys to `Reference`, `Title`, `ContentUrl`, `PdfUrl`, `MissionRef`, `Children`.
  - Apply title typo fix for `mexals-letters_2495-01_grants-world`.
  - Remove the three duplicate entries from `in-character.json` (FR-05).
  - Output stable formatting per design §4.1.

**Satisfies:** Requirements 2 AC-2, 5 AC-1/AC-2, 7 AC-1/AC-2.

---

## Phase 2 — Local file moves

Use `git mv` to preserve history.

### T-05 — Move `mission-reports` content under `in-character/`

```bash
git mv web/wwwroot/content/mission-reports web/wwwroot/content/in-character/mission-reports
```

**Satisfies:** Requirement 3 AC-1.

### T-06 — Move `scenarios` content under `out-of-character/`

```bash
git mv web/wwwroot/content/scenarios web/wwwroot/content/out-of-character/scenarios
```

**Satisfies:** Requirement 3 AC-2.

### T-07 — Move OOC landing page to content root

```bash
git mv web/wwwroot/content/out-of-character/out-of-character.md web/wwwroot/content/out-of-character.md
```

**Satisfies:** Requirement 3 AC-4.

### T-08 — Fix `mision-vortex` → `mission-vortex` filename

```bash
git mv web/wwwroot/content/in-character/briefing-notes_mision-vortex.md \
       web/wwwroot/content/in-character/briefing-notes_mission-vortex.md
```

**Satisfies:** Requirement 7 AC-3.

### T-09 — Create empty `miscellanea` content directory

```bash
mkdir -p web/wwwroot/content/out-of-character/miscellanea
touch web/wwwroot/content/out-of-character/miscellanea/.gitkeep
```

(Currently miscellanea has no markdown content — only one inline PDF reference. The directory exists so future ingestion has a target.)

**Satisfies:** Requirement 4 AC-2.

---

## Phase 3 — Data file rewrites

### T-10 — Run `scripts/rewrite-article-urls.py`

- Apply the script committed in T-04 against all `articles/*.json`.
- Verify the diff is the expected mix of formatting + URL changes.

**Satisfies:** Requirements 2 AC-2, 5, 7 AC-1/AC-2.

### T-11 — Create `web/wwwroot/data/articles/miscellanea.json`

- Move the inline `prop-designs` entry from `articles.json` into this new file.
- Update `articles.json`'s `miscellanea` row to `{ "Reference": "miscellanea", "Data": "/data/articles/miscellanea.json" }`.

**Satisfies:** Requirement 4 AC-1, AC-3.

### T-12 — Fix `mission-procyon-intel` PdfUrl filename

- Currently `2496-00+Mission+%22Procyon%22+-+Intel.pdf`; should match date prefix `2492-10`.
- This is a rename-map override; add to `scripts/rename-overrides.json` and re-run T-03 if T-10 has not already been executed against the updated map.

**Satisfies:** Requirement 7 AC-4.

### T-13 — Add stub entries for the six orphan content files

Per OpenSpec §10.3:

| File | Add to |
|---|---|
| `in-character/general-orders_2602-05-16.md` | `articles/in-character.json` |
| `out-of-character/cmc_newsletter_1999-02.md` | `articles/out-of-character.json` |
| `out-of-character/honours_1995-08-23.md` | `articles/out-of-character.json` |
| `out-of-character/interim-history_2496-to-2501.md` | `articles/out-of-character.json` |
| `out-of-character/mission-briefing_operation-hammerhead_2501-10.md` | `articles/out-of-character.json` |
| `out-of-character/mission-notice_operation-dawn_1999-01.md` | `articles/out-of-character.json` |

Each stub has a meaningful `Reference`, a `Title` derived from the file's H1 (if present, else the filename), the `ContentUrl`, and empty `PdfUrl`.

**Satisfies:** Requirement 6 AC-1, AC-2.

---

## Phase 4 — Verification scripts

### T-14 — Write `scripts/check-content-urls.py`

- Walks all `articles/*.json`.
- For every `ContentUrl`, asserts the file exists under `web/wwwroot/`.
- For every `PdfUrl`, asserts it begins with the area's canonical media-path prefix.
- Exits non-zero on any failure.

**Satisfies:** Requirement 6 AC-4, Requirement 10 (manual check support).

### T-15 — Run T-14; fix any failures before commit

**Satisfies:** evidence for Requirements 2, 5, 6.

---

## Phase 5 — Migration script

### T-16 — Write `scripts/migrate-media-paths.sh`

Implements design §5. Bash. Uses `aws s3 ls` for the inventory and `aws s3 mv` for moves. Reads `scripts/rename-map.json`.

**Satisfies:** Requirement 9.

### T-17 — Add `--reverse` rollback flag to T-16

Per design §7. Swaps `legacy` and `canonical` in each plan entry. Same idempotency check applies.

**Satisfies:** design §7 rollback strategy.

---

## Phase 6 — Cloudflare worker

### T-18 — Write `infra/cloudflare/workers/media_worker/build-redirects.mjs`

Reads `scripts/rename-map.json`, decodes legacy URL components per design §6.6, writes `redirects.generated.js`.

**Satisfies:** Requirement 8 AC-3.

### T-19 — Update `media_worker.js` to consult `REDIRECTS`

Per design §6.4. Imports the generated module; early-exits with `Response.redirect(canonical, 301)` on match.

**Satisfies:** Requirement 8 AC-1, AC-4.

### T-20 — Add `redirects.generated.js` to worker-local `.gitignore`

The generated file is build-time-derived. The map (`rename-map.json`) is the checked-in source of truth.

**Satisfies:** Requirement 8 AC-3 (table-driven, not hand-written).

### T-21 — Update `wrangler.jsonc` to run `build-redirects.mjs` pre-deploy

Use the `build.command` field of `wrangler.jsonc` or document the manual step.

**Satisfies:** Requirement 8 AC-3.

---

## Phase 7 — PR & sign-off

### T-22 — Commit + PR

- Single PR titled `Corpus cleanup: canonical area↔path mapping (#51)`.
- Description links to `docs/specs/51-corpus-cleanup.md` §7 and §10.3.
- Flags the formatting churn in `articles/*.json` (per design §4.1) so reviewers know to look past whitespace.
- Lists the six orphan area assignments for review (per design §3 — Requirement 6 AC-3 expects reviewer override).

### T-23 — Maintainer-executed post-merge steps

These do NOT block the PR but must complete before `#51` is closed:

1. **Back up the bucket first.** Run `./scripts/backup-media-bucket.sh <bucket-name>`. This enables S3 versioning (if not already on), writes an inventory manifest to `backups/<bucket>-<date>/`, and syncs every object to a local tarball. The migration is destructive (`aws s3 mv`); do not skip this step.
2. Run `./scripts/migrate-media-paths.sh <bucket-name>` (dry-run) — verify expected move count.
3. Run `./scripts/migrate-media-paths.sh <bucket-name> --execute`.
4. Deploy updated worker via `wrangler deploy` from `infra/cloudflare/workers/media_worker/`.
5. Smoke test: `curl -I -L https://media.5coy-cmc.org.uk/media/Mission+Reports/2495-11+Mission+%22Nova%22+(Calvin).pdf` returns 301 then 200; bytes match the canonical fetch.
6. Close `#51`; unblock `#50` production deployment.

**Rollback** (if smoke test fails): run `./scripts/migrate-media-paths.sh <bucket-name> --execute --reverse` to move canonical → legacy. If the move itself is what corrupted the bucket, restore from `backups/<bucket>-<date>/objects/` via `aws s3 sync`.

---

## Task → Requirement matrix

| Task | Requirements satisfied |
|---|---|
| T-01 | R2 |
| T-02 | R7 AC-5 |
| T-03 | R2, R8 (evidence) |
| T-04 | R2 AC-2, R5 AC-1/AC-2, R7 AC-1/AC-2 |
| T-05 | R3 AC-1 |
| T-06 | R3 AC-2 |
| T-07 | R3 AC-4 |
| T-08 | R7 AC-3 |
| T-09 | R4 AC-2 |
| T-10 | R2 AC-2, R5, R7 AC-1/AC-2 |
| T-11 | R4 AC-1, AC-3 |
| T-12 | R7 AC-4 |
| T-13 | R6 AC-1, AC-2 |
| T-14 | R6 AC-4, R10 (support) |
| T-15 | R2, R5, R6 |
| T-16 | R9 |
| T-17 | design §7 |
| T-18 | R8 AC-3 |
| T-19 | R8 AC-1, AC-4 |
| T-20 | R8 AC-3 |
| T-21 | R8 AC-3 |
| T-22 | R6 AC-3, R10 |
| T-23 | R8 AC-2, AC-5, R9, R10 |

Every requirement has at least one task; no task is orphaned.
