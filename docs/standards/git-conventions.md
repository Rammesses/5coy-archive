# Git Conventions

**Scope:** All commits and branches in this repository.  
**Keywords:** RFC 2119 (SHALL / SHOULD / MAY / SHALL NOT / SHOULD NOT)

---

## GIT-01 — Commit subject line length

**Rule:** The commit subject line SHALL be 72 characters or fewer.

**Rationale:** Subject lines longer than 72 characters are truncated in `git log --oneline`, GitHub PR views, and many terminal widths.

```
# Correct (47 chars)
Add article detail page with markdown rendering

# Incorrect (81 chars)
Add a new article detail page that renders markdown content using the Markdig library
```

---

## GIT-02 — Imperative mood in subject line

**Rule:** The commit subject line SHALL use imperative mood ("Add", "Fix", "Remove", "Update") — not past tense ("Added") or present continuous ("Adding").

**Rationale:** Matches Git's own convention (e.g. "Merge branch…", "Revert…") and reads naturally as a completion: "If applied, this commit will *Add article detail page*."

```
# Correct
Fix null reference in ArticlesService

# Incorrect
Fixed null reference in ArticlesService
Fixing null reference in ArticlesService
```

---

## GIT-03 — Blank line between subject and body

**Rule:** When a commit message includes a body, the subject line and body SHALL be separated by a blank line.

```
# Correct
Add mission timeline component

Previously missions were listed without chronological context.
This adds a sortable timeline view backed by missions.json.

# Incorrect (no blank line)
Add mission timeline component
Previously missions were listed without chronological context.
```

---

## GIT-04 — Body explains why, not what

**Rule:** The commit body SHOULD explain the motivation for the change — the *why* — rather than describing what the code does (the *what* is visible in the diff).

```
# Correct
Remove BlazorApplicationInsights package

The package has no net10.0 target. Under the .NET 10 WASM runtime it
causes a ManagedError on startup due to an unresolvable type reference.
AppInsights is now instrumented via the existing JS snippet in index.html.

# Incorrect
Remove BlazorApplicationInsights package

Deleted the package reference from web.csproj and removed the
ApplicationInsightsComponent from App.razor.
```

---

## GIT-05 — Branch naming

**Rule:** Branch names SHALL follow the pattern `type/<issue>-<slug>`, where:

- `type` is one of: `feature`, `fix`, `chore`, `docs`
- `<issue>` is the GitHub issue number
- `<slug>` is a short kebab-case description

```
# Correct
feature/48-ai-coding-support
fix/52-missions-page-crash
chore/55-update-dependencies

# Incorrect
ai-coding-support
feature-ai
joel/my-branch
```

---

## GIT-06 — No force-push to integration or production branches

**Rule:** `git push --force` (or `--force-with-lease`) SHALL NOT be used on `dev` or `master`.

**Rationale:** These branches are shared; force-pushing rewrites history that others may have already pulled, causing diverged local copies.

Feature branches MAY be force-pushed by their author while a PR is open, but only before any approvals have been given.

---

## GIT-07 — Co-author trailer for AI-assisted commits

**Rule:** Commits that include code substantially generated or modified by an AI assistant SHOULD include a `Co-Authored-By` trailer identifying the model used.

```
# Correct
Add dev container definition

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
```

**Rationale:** Maintains an accurate record of authorship for licensing and audit purposes. Not required for trivial suggestions (autocomplete, single-line fixes).
