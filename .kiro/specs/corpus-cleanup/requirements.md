# Requirements: Corpus Cleanup

**Feature:** Corpus Cleanup — Canonical Area↔Path Mapping
**Issue:** #51
**Spec:** `docs/specs/51-corpus-cleanup.md`
**Blocks:** #50 production deployment
**Status:** Awaiting review

---

## Requirement 1 — Canonical Routing Table

**User Story:** As the implementer of `#50`, I want a single authoritative table mapping each area to its content path, media path, and ingest-bucket prefix, so that I can populate `AreaTable.cs` without having to reconstruct the mapping from disparate sources.

### Acceptance Criteria

1. GIVEN the OpenSpec at `docs/specs/51-corpus-cleanup.md`, WHEN section 7 is read, THEN the table SHALL contain exactly one row per area listed in `web/wwwroot/data/articles.json` (after the miscellanea normalisation in Requirement 4), with no missing or duplicate Reference values.

2. GIVEN a row in the routing table, WHEN any of its path fields is examined, THEN every path SHALL be lowercase ASCII with hyphens as the only non-alphanumeric character (kebab-case).

3. GIVEN two areas A and B where B is nested under A per `sections.json`, WHEN their content / media / ingest paths are compared, THEN B's path SHALL begin with A's path as a prefix (mirrored nesting).

4. GIVEN an upload to an ingest-bucket key, WHEN the routing table is consulted to determine the destination area, THEN longest-prefix-match SHALL be used to resolve ambiguity between a parent prefix and a nested child prefix.

---

## Requirement 2 — Path Normalisation

**User Story:** As a future contributor uploading a PDF, I want the canonical media paths to use predictable kebab-case names, so that I do not have to URL-encode spaces or apostrophes and can construct a target path mentally.

### Acceptance Criteria

1. GIVEN any markdown file under `web/wwwroot/content/`, WHEN its path is compared against the canonical content path of the area whose data file references it, THEN the file's directory SHALL match the canonical content path exactly.

2. GIVEN any `ContentUrl` or `PdfUrl` string in any `web/wwwroot/data/articles/*.json` file, WHEN parsed, THEN it SHALL begin with the canonical content / media path of its containing area as defined in the routing table.

3. GIVEN the migration script `scripts/migrate-media-paths.sh` is run against the live media bucket in `--execute` mode, WHEN it completes, THEN every PDF SHALL reside at the canonical media path matching the routing table.

4. GIVEN a renamed PDF (per FR-03 / D-02), WHEN its filename is compared against the source filename, THEN the new name SHALL be lowercase ASCII with hyphens replacing spaces, plus signs, percent-encoded characters, ampersands, and apostrophes.

5. GIVEN a folder rename moves the file's parent directory, WHEN `git mv` is used for content moves, THEN git's rename detection SHALL preserve history (verified by `git log --follow` on at least one moved file).

---

## Requirement 3 — Content Tree Mirrors `sections.json`

**User Story:** As a maintainer reading the repository, I want the content directory layout to match the navigation hierarchy, so that the relationship between an article's URL and its position in the nav is unambiguous.

### Acceptance Criteria

1. GIVEN the `mission-reports` area is a child of `in-character` per `sections.json`, WHEN its canonical content path is examined, THEN it SHALL be `/content/in-character/mission-reports/` (not `/content/mission-reports/`).

2. GIVEN the `scenarios` area is a child of `out-of-character` per `sections.json`, WHEN its canonical content path is examined, THEN it SHALL be `/content/out-of-character/scenarios/` (not `/content/scenarios/`).

3. GIVEN the `miscellanea` area is a child of `out-of-character` per `sections.json`, WHEN its canonical content path is examined, THEN it SHALL be `/content/out-of-character/miscellanea/`.

4. GIVEN any area's landing-page markdown, WHEN its location is examined, THEN it SHALL live at `/content/<area>.md` (content root), not nested inside the area's folder. Specifically, `/content/out-of-character/out-of-character.md` SHALL be moved to `/content/out-of-character.md`.

---

## Requirement 4 — Miscellanea as a First-Class Area

**User Story:** As the implementer of `#50`, I want every area to be uniformly represented by its own data file under `web/wwwroot/data/articles/`, so that the routing-table generator does not need a special case for inline articles.

### Acceptance Criteria

1. GIVEN `web/wwwroot/data/articles.json`, WHEN the `miscellanea` entry is inspected, THEN it SHALL reference a `"Data"` field pointing to `/data/articles/miscellanea.json`, not an inline `"Articles"` array.

2. GIVEN `web/wwwroot/data/articles/miscellanea.json`, WHEN it is parsed, THEN it SHALL contain the `prop-designs` entry currently inlined in `articles.json`.

3. GIVEN the existing inline `Articles` array in `articles.json`, WHEN the cleanup is complete, THEN it SHALL be removed.

---

## Requirement 5 — De-Duplication of Cross-Area Overlap

**User Story:** As a reader, I want each document to have a single canonical home in the article indexes, so that link sharing, search-engine indexing and editorial responsibility are unambiguous.

### Acceptance Criteria

1. GIVEN any `Reference` value appearing in `web/wwwroot/data/articles/*.json`, WHEN searched across all article data files, THEN it SHALL appear in exactly one file (counting `Children:` entries as part of their parent's file).

2. GIVEN the three previously-duplicated entries (`2492-10_mission-procyon-intel`, `2496-02_mission-nova_comms-001`, `2496-02_mission-intruder_comms-002`), WHEN the cleanup is complete, THEN they SHALL appear only in `mission-reports.json` as `Children:` of their respective parent mission entries.

3. GIVEN a content markdown file, WHEN searched across all `ContentUrl` references, THEN it SHALL be referenced by exactly one entry across all article data files.

4. GIVEN a cross-area relationship (e.g., a mexals-letters entry referring to a mission), WHEN the relationship is expressed in the data file, THEN it SHALL use the `MissionRef` field rather than duplicating the entry into the related area.

---

## Requirement 6 — Orphan Content Triage

**User Story:** As a maintainer, I want every markdown file under `web/wwwroot/content/` to be reachable from the site's navigation, so that no content silently rots while occupying disk space.

### Acceptance Criteria

1. GIVEN the six orphan content files listed in §10.3 of the OpenSpec, WHEN the cleanup PR is opened, THEN each SHALL have a stub entry added to the proposed area's `articles/*.json` file.

2. GIVEN a stub entry for an orphan, WHEN inspected, THEN it SHALL have a populated `Reference`, `Title`, and `ContentUrl`; the `PdfUrl` MAY be the empty string if no PDF exists.

3. GIVEN PR review of the orphan stubs, WHEN a reviewer disagrees with the proposed area, THEN the reviewer MAY move the entry to another area's file; the cleanup PR SHALL NOT block on consensus per orphan.

4. GIVEN the cleanup PR has merged, WHEN `find web/wwwroot/content -name '*.md'` is compared against the union of all `ContentUrl` references, THEN no content file (other than landing pages registered in `sections.json`) SHALL lack an article entry.

---

## Requirement 7 — Data-Quality Bug Fixes

**User Story:** As a future automated consumer of the article indexes (`#50`, search), I want the JSON to be syntactically and semantically clean, so that I do not need to special-case typos and casing inconsistencies.

### Acceptance Criteria

1. GIVEN every article entry across all data files, WHEN its field keys are inspected, THEN they SHALL use the canonical casing: `Reference`, `Title`, `ContentUrl`, `PdfUrl`, `MissionRef`, `Children`.

2. GIVEN the `mexals-letters_2495-01_grants-world` entry, WHEN its `Title` is read, THEN it SHALL be `2495-01 - Grant's World` (apostrophe, not double-quote).

3. GIVEN the `briefing-notes_mision-vortex` entry, WHEN its `Reference`, `ContentUrl` filename, and the markdown file's name are inspected, THEN all three SHALL spell `mission-vortex` (not `mision-vortex`).

4. GIVEN the `2492-10_mission-procyon-intel` entry, WHEN its `PdfUrl` is inspected, THEN the filename portion SHALL match the entry's date prefix (`2492-10-mission-procyon-intel.pdf`), not `2496-00`.

5. GIVEN any marie-celesta reference, WHEN the markdown filename and PDF filename are compared, THEN both SHALL spell `marie-celesta` (the historical document spelling); inconsistent `marie-celeste` filenames SHALL be renamed to `marie-celesta`.

---

## Requirement 8 — Legacy URL Compatibility

**User Story:** As a reader who has bookmarked a PDF URL or shared it on social media, I want the old URL to continue working indefinitely, so that no link I have shared ever breaks.

### Acceptance Criteria

1. GIVEN a request to the Cloudflare worker for any URL matching a pre-cleanup `/media/...` path, WHEN the worker handles the request, THEN it SHALL respond with HTTP `301 Moved Permanently` and a `Location` header pointing to the canonical post-cleanup URL.

2. GIVEN a request to a URL that was never valid (neither legacy nor canonical), WHEN the worker handles the request, THEN it SHALL respond with HTTP `404 Not Found` (no change to existing behaviour for genuinely-missing files).

3. GIVEN the worker is being deployed, WHEN the legacy→canonical mapping is configured, THEN it SHALL be table-driven from a single data structure derived from the routing table (§7 of the OpenSpec) and the per-file rename map; there SHALL NOT be hand-written conditional logic per legacy path.

4. GIVEN a `HEAD` request to any legacy URL, WHEN handled by the worker, THEN the response SHALL include the same `Location` header as the equivalent `GET`, with status `301`.

5. GIVEN the migration has executed, WHEN a legacy URL is fetched via curl, THEN following the redirect SHALL deliver the same PDF bytes that the canonical URL delivers.

---

## Requirement 9 — Migration Script

**User Story:** As the maintainer executing the corpus cleanup, I want a single repeatable script to perform the S3 moves, so that I do not have to construct dozens of `aws s3 mv` commands by hand or worry about partially-completed migrations.

### Acceptance Criteria

1. GIVEN the script `scripts/migrate-media-paths.sh`, WHEN run with no arguments, THEN it SHALL print usage instructions and exit non-zero.

2. GIVEN the script is run with `<bucket-name>` as a positional argument and no flags, WHEN it executes, THEN it SHALL default to `--dry-run` mode and print every `aws s3 mv` command it *would* execute without performing any mutation.

3. GIVEN the script is run with `<bucket-name> --execute`, WHEN it runs, THEN it SHALL invoke `aws s3 mv` for every legacy→canonical move and report success/failure per object.

4. GIVEN the script is run twice in `--execute` mode against a partially-migrated bucket, WHEN the second run completes, THEN it SHALL skip already-migrated objects, complete without error, and not duplicate any object.

5. GIVEN the script completes, WHEN its final output is read, THEN it SHALL print a verification summary listing any objects still residing at legacy paths.

---

## Requirement 10 — Build & Render Verification

**User Story:** As a maintainer reviewing the PR, I want the existing .NET site to continue rendering every article correctly under its new canonical path, so that I can confirm no regression before merging.

### Acceptance Criteria

1. GIVEN the cleanup PR's branch is checked out, WHEN `dotnet build` is run against `web/`, THEN it SHALL complete with no errors and no new warnings.

2. GIVEN the site is run locally, WHEN one article per area is loaded in a browser, THEN the markdown SHALL render correctly and the linked PDF SHALL be served from the canonical URL.

3. GIVEN the site is run locally, WHEN a legacy `/media/...` URL is fetched, THEN the local environment SHALL either redirect (if the worker fallback is wired locally) or 404 cleanly; a 500 SHALL NOT occur.
