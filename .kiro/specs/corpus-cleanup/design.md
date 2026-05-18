# Design: Corpus Cleanup

**Feature:** Corpus Cleanup — Canonical Area↔Path Mapping
**Issue:** #51
**Spec:** `docs/specs/51-corpus-cleanup.md`
**Requirements:** `.kiro/specs/corpus-cleanup/requirements.md`

---

## 1. Architecture Overview

The cleanup is structured as four interlocking deliverables. The first three are products of a single source of truth (a per-file rename map derived from the routing table); the fourth is the table itself.

```
                  +-------------------------------+
                  |   Routing Table (OpenSpec §7) |
                  |   9 areas, canonical paths    |
                  +---------------+---------------+
                                  |
            +---------------------+---------------------+
            |                                           |
            v                                           v
  +-------------------+                       +--------------------+
  | Per-file rename   |                       | sections.json      |
  | map (generated)   |                       | (unchanged)        |
  +---------+---------+                       +--------------------+
            |
   +--------+--------+--------+----------------+
   |                 |        |                |
   v                 v        v                v
+--------+    +-----------+   +----------+   +--------------+
| git mv | -> | articles  |   | aws s3   |   | Cloudflare   |
| local  |    | *.json    |   | migration|   | media_worker |
| moves  |    | rewrites  |   | script   |   | 301 layer    |
+--------+    +-----------+   +----------+   +--------------+
```

The four deliverables converge in a single PR. Once merged:

- Local content + JSON state is consistent with the routing table.
- A maintainer runs the migration script against the live S3 bucket.
- The deployed worker handles legacy URL preservation.

There is no application code change beyond the worker; the .NET site reads its data files as before.

---

## 2. The Per-File Rename Map

The rename map is the source of truth feeding the JSON rewrites, the migration script, and the worker's redirect table. It is constructed deterministically by a one-off Python script (`scripts/generate-rename-map.py`) run during implementation. Its output is checked into the repository as `scripts/rename-map.json`.

### 2.1 Generation algorithm

```python
def canonical_filename(legacy_path: str) -> str:
    # Input:  "/media/Mexal's+Letters/2495-05+-+Marie+Celesta.pdf"
    # Output: "/media/in-character/mexals-letters/2495-05-marie-celesta.pdf"

    decoded = urllib.parse.unquote_plus(legacy_path)
    # → "/media/Mexal's Letters/2495-05 - Marie Celesta.pdf"

    parts = decoded.split('/')
    new_parts = []
    for part in parts:
        if part == '':
            new_parts.append(part)
            continue
        if part == 'media':
            new_parts.append(part)
            continue
        normalised = (
            part.lower()
                .replace("'", '')
                .replace('"', '')
                .replace('&', 'and')
                .replace('+', '-')
                .replace(' ', '-')
        )
        normalised = re.sub(r'[^a-z0-9.-]', '-', normalised)
        normalised = re.sub(r'-+', '-', normalised).strip('-')
        new_parts.append(normalised)

    # Then apply the folder-level rewrite from the routing table:
    # /media/<legacy-folder>/* → /media/<canonical-area-prefix>/*
    return apply_folder_remap('/'.join(new_parts))
```

### 2.2 Map structure

`scripts/rename-map.json`:

```json
{
  "version": 1,
  "generated_at": "2026-05-17T00:00:00Z",
  "source": "docs/specs/51-corpus-cleanup.md §7 + on-disk articles/*.json crawl",
  "moves": [
    {
      "legacy": "/media/Mexal's+Letters/2495-05+-+Marie+Celesta.pdf",
      "canonical": "/media/in-character/mexals-letters/2495-05-marie-celesta.pdf",
      "area": "mexals-letters"
    },
    ...
  ]
}
```

The map is committed for two reasons: it is the auditable record of "what moved where" (essential for incident response if a URL stops working), and the worker reads it at build time to construct its redirect table.

### 2.3 Override mechanism

Where the auto-generated canonical name is unreadable or contextually wrong, a sibling file `scripts/rename-overrides.json` SHALL provide per-file overrides:

```json
{
  "overrides": {
    "/media/Some+Mangled+Name.pdf": "/media/out-of-character/some-better-name.pdf"
  }
}
```

`generate-rename-map.py` reads `rename-overrides.json` *after* its own derivation and replaces the `canonical` value for any matching `legacy` key. PR review identifies which files need overrides.

---

## 3. Content File Moves

Local content moves use `git mv` exclusively so move history is preserved:

```bash
# mission-reports content: top-level → nested under in-character/
git mv web/wwwroot/content/mission-reports web/wwwroot/content/in-character/mission-reports

# scenarios content: top-level → nested under out-of-character/
git mv web/wwwroot/content/scenarios web/wwwroot/content/out-of-character/scenarios

# OOC landing page: nested → content root
git mv web/wwwroot/content/out-of-character/out-of-character.md web/wwwroot/content/out-of-character.md

# Rename mision-vortex → mission-vortex (D-10 bug fix)
git mv web/wwwroot/content/in-character/briefing-notes_mision-vortex.md \
       web/wwwroot/content/in-character/briefing-notes_mission-vortex.md
```

Content filenames themselves are NOT normalised to strict kebab-case in this pass — they already use lowercase-with-hyphens-and-underscores and existing patterns are recognisable. Only the typo fixes from FR-10 are applied to filenames. PDF filenames ARE normalised (D-02) because they currently use display-name spelling.

### 3.1 Marie-Celesta spelling normalisation

Content markdown files already use `marie-celesta`. The PDF filename `Welcome-to-the-Marie-Celeste.pdf` is the outlier. The canonical filename becomes `welcome-to-the-marie-celesta.pdf` (note: `celesta`, matching the existing markdown filename). This is encoded in `rename-overrides.json`.

---

## 4. Articles JSON Rewrites

Article data files are rewritten by a one-off Python script (`scripts/rewrite-article-urls.py`) that reads each `articles/*.json`, applies the rename map to every `ContentUrl` and `PdfUrl`, and writes back with stable JSON formatting (2-space indent, trailing commas removed, preserved key order). The script also:

- Normalises `reference` → `Reference` keys (FR-08).
- Fixes the title typo in `mexals-letters_2495-01_grants-world` (FR-10).
- Removes the three duplicate entries from `in-character.json` (FR-05).
- Creates `miscellanea.json` and removes the inline `Articles` array from `articles.json` (FR-07).
- Adds stub entries for the six orphan files (FR-09).

The script is committed under `scripts/` for auditability but is NOT idempotent in the strict sense: re-running it after the rewrite is complete would NOT detect already-canonical URLs and would be a no-op only because the rename-map matching would not fire. This is acceptable because the script is single-use — its outputs are checked into the repository, and future maintenance happens via direct JSON edits.

### 4.1 Stable JSON formatting

The current `articles/*.json` files use inconsistent indentation (2 spaces, mixed with 4-space-leading from nested objects). To make the rewrite diff readable, the script normalises to:

- 2-space indent throughout.
- No trailing whitespace.
- One key per line.
- Preserve insertion order (Python 3.7+ `dict` ordering is the default).
- Final newline at EOF.

This means the rewrite diff will contain *formatting* changes alongside *content* changes. The PR description SHALL flag this so reviewers know to look past whitespace churn.

---

## 5. Migration Script (`scripts/migrate-media-paths.sh`)

### 5.1 Behaviour

```
USAGE:
  ./scripts/migrate-media-paths.sh <bucket> [--execute] [--map <path>]

ARGUMENTS:
  <bucket>           S3 bucket name (e.g., 5coy-media-prod)

OPTIONS:
  --execute          Perform live moves. Default is --dry-run.
  --map <path>       Path to rename-map.json. Default: scripts/rename-map.json
  --concurrency <n>  Parallel aws s3 mv calls. Default: 8.

EXIT CODES:
  0   success (dry-run or execute)
  1   precondition failure (missing bucket, missing map, missing aws cli)
  2   one or more moves failed in --execute mode
```

### 5.2 Logic

```
1. Validate aws cli is on PATH, the map file exists, the bucket name is valid.
2. List bucket contents into a local sorted file.
3. For each {legacy, canonical} pair in the rename map:
   a. Check if legacy key exists in the listing.
   b. If yes, plan a move legacy → canonical.
   c. If no, check if canonical exists. If yes, skip (already migrated).
   d. If neither exists, warn and skip (orphan in map).
4. In --dry-run: print the planned moves and exit 0.
5. In --execute: run aws s3 mv with --quiet, parallelised at the specified concurrency.
6. Re-list bucket contents.
7. Report:
   - Total moves planned / executed / skipped.
   - Any legacy paths still present (failure).
   - Any unexpected files not in the rename map.
```

### 5.3 Idempotency

The `legacy exists ? : canonical exists ? skip` check makes re-execution safe. The script never deletes the canonical side; never creates the legacy side; only ever moves legacy → canonical. Running twice is equivalent to running once.

---

## 6. Cloudflare Worker (`infra/cloudflare/workers/media_worker/media_worker.js`)

### 6.1 Change scope

The worker currently proxies `/media/*` to a Cloudfront origin (20 lines of code). The change adds:

- A `REDIRECTS` map embedded as a JS literal at the top of the file (generated from `rename-map.json` by a build-time script).
- An early-exit branch in `handleRequest`: if the pathname matches a `REDIRECTS` key, return `301` immediately.

### 6.2 Lookup strategy

`REDIRECTS` is a plain object (`Record<string, string>`). Lookup is `O(1)` with no construction cost beyond what V8 already does for static object literals. Approximate size after generation: ~150 entries (one per current PDF), well under any worker bundle limit.

### 6.3 Build-time generation

A new script `infra/cloudflare/workers/media_worker/build-redirects.mjs` runs as a pre-deploy hook (added to `wrangler.jsonc` or invoked from a make/npm script):

```javascript
import { readFile, writeFile } from 'node:fs/promises';

const map = JSON.parse(await readFile('../../../../scripts/rename-map.json', 'utf8'));
const redirects = Object.fromEntries(
  map.moves.map(({ legacy, canonical }) => [legacy, canonical])
);
const output = `// AUTO-GENERATED from scripts/rename-map.json — do not edit.\n` +
               `export const REDIRECTS = ${JSON.stringify(redirects, null, 2)};\n`;
await writeFile('./redirects.generated.js', output);
```

The worker imports `redirects.generated.js`. The generated file is `.gitignore`d at the worker level (regenerated on every deploy from the checked-in rename map).

### 6.4 Updated worker flow

```javascript
import { REDIRECTS } from './redirects.generated.js';

const ORIGIN = 'dxa4dl52uee85.cloudfront.net';

async function handleRequest(request) {
  const url = new URL(request.url);
  const pathname = url.pathname;

  // Legacy URL preservation: 301 to canonical
  if (REDIRECTS[pathname]) {
    const canonical = new URL(url);
    canonical.pathname = REDIRECTS[pathname];
    return Response.redirect(canonical.toString(), 301);
  }

  // Existing proxy path
  if (pathname.startsWith('/media')) {
    url.hostname = ORIGIN;
    return fetch(url.toString(), request);
  }

  return fetch(request);
}
```

### 6.5 Method handling

`Response.redirect()` returns a `301` with no body. Browsers and well-behaved curl invocations follow it on both `GET` and `HEAD`. This satisfies Requirement 8 AC-4 without extra code.

### 6.6 No URL-decoding required

`pathname` from `new URL(request.url)` is already decoded for the path component (e.g., `%20` becomes space). However, the legacy URLs in the rename map are stored as they were encoded in the article JSON (`+` for space, `%27` for apostrophe). The build-time generator therefore writes the `REDIRECTS` map with *decoded* keys, matching what the worker observes.

The generator script must therefore decode keys before insertion:

```javascript
[decodeURIComponent(legacy.replaceAll('+', '%20')), canonical]
```

This subtle point is the most likely source of an integration bug; integration tests (Requirement 8 AC-5) catch it.

---

## 7. Rollback Strategy

If a problem surfaces post-merge, rollback has three layers:

| Layer | Rollback action | Recovery time |
|---|---|---|
| Cloudflare worker | Redeploy previous worker version via `wrangler rollback` | < 1 minute |
| S3 media bucket | Re-run migration script with a reversed map (`canonical → legacy`); script gains a `--reverse` flag for this purpose | ~5 minutes |
| Local repo state | `git revert <cleanup-commit>`; site rebuilds against pre-cleanup paths | requires deploy |

The reversed-map capability for S3 rollback is a small addition to the migration script: a `--reverse` flag that swaps `legacy` and `canonical` in each rename entry before planning. Acceptance Criterion R9-AC-4's idempotency check applies in both directions.

---

## 8. Test Strategy

Manual / scripted verification, no new automated test framework introduced:

| Check | Method |
|---|---|
| Routing table self-consistency | `scripts/validate-routing-table.py` parses the OpenSpec, asserts kebab-case + nested-prefix rules |
| JSON rewrites preserve every entry | Diff entry-count per file before/after; assert equal except for `in-character.json` (−3) and `articles.json` (miscellanea inlined → external) |
| Every ContentUrl resolves | `scripts/check-content-urls.py` walks all `articles/*.json`, asserts each `ContentUrl` exists on disk under `web/wwwroot/` |
| Every PdfUrl is canonical | Regex assertion against the canonical-path prefixes from §7 |
| Migration script dry-run | `./scripts/migrate-media-paths.sh <bucket>` reports the expected number of moves, no errors |
| Worker redirects | After staging deploy, `curl -I -L <legacy-url>` returns `301` then `200`; bytes match canonical fetch |
| Build cleanliness | `dotnet build web/` exits 0 with no new warnings |
| Render smoke | Local site renders one article per area; PDF links resolve |

---

## 9. Sequencing

The work has a hard dependency order; out-of-order execution risks inconsistent intermediate states.

```
1. Generate rename-map.json (from current article data + routing table)
2. Apply local changes:
   2a. git mv content files
   2b. Rewrite articles/*.json (URLs, deduplication, miscellanea split, orphan stubs, bug fixes)
   2c. Add orphan stubs
3. Generate redirects.generated.js (for the worker; auto-derived from map)
4. Commit local + worker changes (single PR)
5. Open PR; reviewers validate against §7 and §10.3
6. Merge PR
7. Maintainer runs migrate-media-paths.sh --dry-run, inspects, then --execute
8. Maintainer deploys updated worker
9. Smoke test: curl legacy URLs, verify 301 + canonical resolution
```

Steps 2–6 happen in this session. Steps 7–9 require the maintainer's AWS + Cloudflare creds and are out of scope for this session per the user's scope decision.

---

## 10. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| A legacy URL exists in the wild that we don't have a `PdfUrl` reference for | Medium | A bookmark 404s | Worker logs unmatched `/media/...` 404s for one week post-deploy; add to rename map and redeploy if discovered |
| Rename script normalises a filename in a way that loses information | Low | An article appears under a wrong-feeling URL | `rename-overrides.json` allows per-file fixes; PR review catches the obvious cases |
| `git mv` doesn't preserve history for some files | Very low | Loss of `git log --follow` continuity | Sample-check one file per moved directory before commit |
| Migration script partial-fails halfway | Low | Bucket in inconsistent state | Idempotent design (R9-AC-4); re-run completes safely |
| Worker deploy succeeds but redirects map is stale | Low | New canonical URLs 200, but legacy URLs still hit the proxy (404) | Build hook regenerates `redirects.generated.js` from the checked-in map on every deploy |
| Reviewer disagrees with an orphan's area assignment | High | One JSON edit per disagreement during review | Explicitly scoped (R6-AC-3) — does not block PR |

---

## 11. Out-of-Scope

The following are deliberately deferred:

- **Renaming content filenames to strict kebab-case.** Existing filenames mix `_` and `-`. Touching them would balloon the diff with no functional benefit; #50's ingestion writes new files in strict kebab-case from day one.
- **Updating PdfUrl encoding to use `%20` instead of `+` consistently.** Post-cleanup, no PdfUrl needs encoding at all (kebab-case ASCII).
- **Per-article `Reference` audit for collisions across files.** Requirement 5 AC-1 guarantees uniqueness *after* deduplication; a wider audit (e.g., references that *look* like they should match across areas but don't) is a separate cleanup.
- **`MissionRef` accuracy audit.** Many entries have empty `MissionRef`; populating these requires editorial judgement and is out of scope.
- **`web/wwwroot/data/videos/*.json` parallel cleanup.** Videos are not part of `#50`'s ingest scope.
