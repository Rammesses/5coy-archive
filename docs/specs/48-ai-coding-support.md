# OpenSpec: AI Coding Support

| Field | Value |
|---|---|
| Issue | [#48](https://github.com/Rammesses/5coy-archive/issues/48) |
| Branch | `feature/48-ai-coding-support` |
| Status | Draft |
| Authors | Rammesses |
| Created | 2026-05-16 |
| Keywords | RFC 2119 (SHALL / SHOULD / MAY / SHALL NOT / SHOULD NOT) |

---

## 1. Background

The 5coy-archive is a Blazor WebAssembly + Azure Functions site maintained by a small team. Developer onboarding, context-switching, and code-review consistency are friction points. AI coding tools can reduce that friction significantly, but only if they are deeply aware of the codebase's conventions, architecture, and toolchain — and if they are integrated at every stage of the development workflow, from a developer's first `git clone` to automated pipeline review.

---

## 2. Problem Statement

A new or returning developer currently has no structured source of truth for:
- how to build, run and test the project locally;
- the architectural decisions behind the tech stack;
- the coding conventions and patterns expected in PRs;
- how CI/CD pipelines are structured and what they do.

AI assistants are only as useful as the context they are given. Without explicit project documentation targeting AI tooling, Claude Code and equivalent tools default to generic behaviour, missing project-specific patterns and producing suggestions that require rework.

---

## 3. Goals

- **G1.** A developer who has never seen this codebase can clone, run, and make a meaningful contribution within one hour, with AI assistance throughout.
- **G2.** AI tools actively assist in every part of the development lifecycle: local coding, PR review, CI/CD.
- **G3.** The solution is provider-agnostic: no single AI vendor is required, and switching providers does not require structural changes.
- **G4.** All AI tooling configuration is version-controlled and kept in the repository alongside the code it describes.

## 4. Non-Goals

- This spec does not cover AI features visible to end-users of the archive site.
- This spec does not mandate a specific AI provider or require API key management in production infrastructure.
- This spec does not introduce AI-generated content into the archive corpus.

---

## 5. Stakeholders

| Role | Interest |
|---|---|
| Site developers | Primary beneficiaries — faster onboarding, better tooling, consistent review |
| Project maintainer | Reduced review burden; consistent PR quality |
| New contributors | Low-friction first contribution experience |

---

## 6. Functional Requirements

### 6.1 Project Intelligence Document (CLAUDE.md)

**FR-01** The repository SHALL contain a `CLAUDE.md` file at the root that provides authoritative, AI-optimised documentation of the project.

**FR-02** `CLAUDE.md` SHALL include:
- (a) a concise project overview and purpose;
- (b) the full tech stack with version constraints (Blazor WASM .NET 10, Azure Functions isolated worker, etc.);
- (c) the repository structure with the purpose of each project;
- (d) all commands needed to build, test, run and publish locally;
- (e) the branching strategy (`master` → `dev` → `feature/*`);
- (f) coding conventions and naming standards;
- (g) the PR process and review expectations.

**FR-03** `CLAUDE.md` SHOULD include annotated examples of the dominant patterns in the codebase (Blazor service injection, data loading, component structure).

**FR-04** `CLAUDE.md` SHALL be kept current: any PR that changes architecture, toolchain or conventions SHOULD include a corresponding update to `CLAUDE.md`.

### 6.2 IDE Configuration

**FR-05** The repository SHALL contain a `.vscode/` directory with:
- (a) `extensions.json` — a recommended extension list covering C#, Blazor, AI assistance (provider-agnostic: e.g. Continue, GitHub Copilot, or equivalent);
- (b) `settings.json` — workspace settings for formatting, linting and test discovery consistent with project conventions;
- (c) `tasks.json` — VS Code task definitions for build, test, run and publish.

**FR-06** The repository SHOULD contain equivalent configuration for JetBrains Rider (`.idea/` run configurations and code style settings).

**FR-07** IDE configuration SHALL NOT contain user-specific paths, API keys, or settings that would break on another developer's machine.

### 6.3 Dev Container

**FR-08** The repository SHALL contain a `.devcontainer/devcontainer.json` that defines a fully reproducible development environment.

**FR-09** The dev container SHALL include:
- (a) the correct .NET 10 SDK;
- (b) the Azure Functions Core Tools;
- (c) Node.js and npm (for wrangler / Cloudflare tooling);
- (d) the GitHub CLI (`gh`);
- (e) Claude Code CLI.

**FR-10** A developer SHALL be able to clone the repository, open it in the dev container, and run the full build and test suite without installing anything on their host machine.

**FR-11** The dev container SHOULD include a post-create script that runs `dotnet restore` and validates the environment.

### 6.4 Coding Standards Documentation

**FR-12** The repository SHALL contain a `docs/standards/` directory with human- and AI-readable standards documents covering:
- (a) C# / Blazor coding conventions;
- (b) git commit message format;
- (c) PR description format;
- (d) file and folder naming rules.

**FR-13** Standards documents SHALL use normative language (SHALL / SHOULD / MAY) for rules so AI tools can reliably distinguish requirements from guidance.

### 6.5 CI/CD AI Integration

**FR-14** The repository SHALL contain a GitHub Actions workflow that performs an automated AI code review on every pull request targeting `dev` or `master`.

**FR-15** The automated review SHALL post its findings as a PR comment, covering as a minimum:
- (a) adherence to coding standards;
- (b) potential security issues;
- (c) missing or inadequate tests.

**FR-16** The automated review SHOULD use a provider-agnostic mechanism (e.g. the Claude Code CLI or a configurable action) so the underlying model can be changed without modifying the workflow.

**FR-17** The repository SHOULD include a workflow that generates a structured PR summary when one is not provided by the author.

**FR-18** CI workflows SHALL NOT block merges on AI review findings alone; findings are advisory. Human approval remains required.

---

## 7. Non-Functional Requirements

**NFR-01 Provider agnosticism.** No requirement SHALL hardcode a specific AI provider. Where a provider must be named in configuration (e.g. a GitHub Action), it SHALL be referenced via a repository variable or secret so it can be swapped without a code change.

**NFR-02 Security.** AI tooling configuration SHALL NOT contain API keys, tokens or credentials in plain text. All secrets SHALL be stored in GitHub Actions secrets or equivalent.

**NFR-03 Maintainability.** All AI tooling configuration SHALL be reviewable by a human without AI assistance — no opaque binary formats.

**NFR-04 Cost transparency.** Any CI workflow that invokes an AI API SHALL log the model used and, where available, token counts, to enable cost monitoring.

---

## 8. Acceptance Criteria

**AC-01** A developer with no prior knowledge of the project clones the repo, opens it in the dev container, and can run `dotnet build` and `dotnet test` successfully within 10 minutes.

**AC-02** Claude Code, opened in the repo root, correctly describes the project architecture, the correct `dotnet run` command, and the branching strategy without additional prompting.

**AC-03** A pull request to `dev` automatically receives an AI code review comment within 5 minutes of opening.

**AC-04** The AI review comment correctly identifies at least one of: a style violation, a missing test, or a potential security issue, when such issues are deliberately introduced in a test PR.

**AC-05** All IDE configuration loads without errors in VS Code on macOS, Windows and Linux.

**AC-06** No secrets or API keys appear in the repository in plain text.

---

## 9. Phasing

### Phase 1 — Local Developer Experience *(implement first)*
- FR-01 through FR-04 (CLAUDE.md)
- FR-05 through FR-07 (IDE config)
- FR-08 through FR-11 (dev container)
- FR-12 through FR-13 (standards docs)

*Exit criterion: AC-01 and AC-02 pass.*

### Phase 2 — CI/CD AI Integration *(implement second)*
- FR-14 through FR-18 (automated PR review and summary)

*Exit criterion: AC-03 and AC-04 pass.*

### Phase 3 — Hardening *(implement third)*
- NFR-01 through NFR-04 verified across all deliverables
- AC-05 and AC-06 verified
- Review and update cycle established

---

## 10. Open Questions

| # | Question | Owner | Due |
|---|---|---|---|
| OQ-01 | Which AI provider / model should be the default for CI workflows? | Rammesses | Before Phase 2 |
| OQ-02 | Should the dev container be GitHub Codespaces-compatible, or local Docker only? | Rammesses | Before Phase 1 |
| OQ-03 | Are there any constraints on what code can be sent to an external AI API (e.g. data classification)? | Rammesses | Before Phase 2 |
| OQ-04 | Should JetBrains Rider support (FR-06) be in scope for Phase 1 or deferred? | Rammesses | Before Phase 1 |
