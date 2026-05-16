# Design: AI Coding Support

**Feature:** AI Coding Support  
**Issue:** #48  
**Phase:** 1 — Local Developer Experience  
**Status:** Awaiting review  

---

## 1. Overview

Phase 1 delivers four artefacts that together constitute a complete local developer experience layer:

```
5coy-archive/
├── CLAUDE.md                          # Req 1 — Project intelligence document
├── .vscode/
│   ├── extensions.json                # Req 2 — Recommended extensions
│   ├── settings.json                  # Req 2 — Workspace settings
│   └── tasks.json                     # Req 2 — Build/test/run tasks
├── .devcontainer/
│   ├── devcontainer.json              # Req 3 — Container definition
│   └── post-create.sh                 # Req 3 — Post-create setup script
└── docs/
    └── standards/
        ├── csharp-conventions.md      # Req 4 — C# and Blazor rules
        ├── git-conventions.md         # Req 4 — Commit and branch rules
        └── pr-conventions.md          # Req 4 — PR format rules
```

---

## 2. CLAUDE.md

### 2.1 Purpose

`CLAUDE.md` is the primary context document consumed by AI coding assistants. Claude Code reads it automatically on startup; other tools (GitHub Copilot, Amazon Kiro) can be configured to read it via their context/include mechanisms.

It is written for an AI reader first, a human reader second — dense, precise, and free of marketing language.

### 2.2 Structure

```
# 5coy-archive — Project Intelligence

## Purpose
## Tech Stack
## Repository Structure
## Build, Test & Run
## Architecture
## Key Patterns
## Branching & PR Strategy
## Coding Conventions (summary — see docs/standards/ for detail)
## Known Constraints & Gotchas
```

### 2.3 Key content decisions

| Section | Detail |
|---|---|
| Tech Stack | .NET 10, Blazor WebAssembly (`Microsoft.NET.Sdk.BlazorWebAssembly`), Azure Functions v4 isolated worker (`net10.0`), Blazor Bootstrap 3.5, Markdig 1.2, HtmlSanitizer 9 |
| Build commands | `dotnet build 5coy-archive.sln`, `dotnet test 5coy-archive.sln`, `dotnet run` from `/web` |
| Run URL | `http://localhost:5000` |
| Data loading | All data loaded from local JSON files in `web/wwwroot/data/` via `HttpClient`; no server-side API calls at runtime |
| API project | `api/` is an Azure Functions isolated worker — currently a placeholder, not called by the web app |
| BlazorApplicationInsights | Removed (no .NET 10 support); AppInsights instrumented directly via JS snippet in `index.html` |
| Branching | `master` (production) ← `dev` (integration) ← `feature/<issue>-<slug>` |
| PR targets | Feature branches → `dev`; `dev` → `master` for releases |

### 2.4 Gotchas to document

- `web/web.sln` exists alongside the root `5coy-archive.sln`; always use the root solution for builds.
- `blazor.boot.json` does not exist in .NET 10 (replaced by fingerprinted asset system); 404 on that path is expected.
- `System.Net.Http.Json` must not be added as a package reference — it is included in the .NET 10 BCL.
- `Microsoft.AspNetCore.Cors` is a server-side package and must not be referenced from the Blazor WASM project.
- `BlazorApplicationInsights` has no .NET 10 target and crashes the WASM runtime; do not re-add it.

---

## 3. VS Code Configuration

### 3.1 extensions.json

Recommended extensions, grouped by purpose:

| Purpose | Extension ID |
|---|---|
| C# language support | `ms-dotnettools.csdevkit` |
| Blazor / Razor | `ms-dotnettools.blazorwasm-companion` |
| AI assistant (Claude) | `anthropic.claude-code` |
| AI assistant (Copilot) | `github.copilot` |
| EditorConfig support | `editorconfig.editorconfig` |
| Spell check | `streetsidesoftware.code-spell-checker` |
| Markdown preview | `yzhang.markdown-all-in-one` |

Extensions are listed as `recommendations`, not `unwantedRecommendations`. No extension is forced.

### 3.2 settings.json

| Setting | Value | Reason |
|---|---|---|
| `editor.formatOnSave` | `true` | Enforces consistent formatting |
| `editor.tabSize` | `4` | Matches project C# convention |
| `editor.insertSpaces` | `true` | No tabs |
| `files.trimTrailingWhitespace` | `true` | Clean diffs |
| `files.insertFinalNewline` | `true` | POSIX compliance |
| `dotnet-test-explorer.testProjectPath` | `**/*.test.csproj` | Enables test discovery |
| `[razor]` `editor.formatOnSave` | `false` | Razor formatter is destructive in VS Code; disabled until stable |

### 3.3 tasks.json

Four tasks, all using `dotnet` CLI, running from the workspace root:

| Task label | Command | Group |
|---|---|---|
| Build | `dotnet build 5coy-archive.sln` | build (default) |
| Test | `dotnet test 5coy-archive.sln` | test (default) |
| Run (web) | `dotnet run --project web/web.csproj` | none |
| Run (api) | `dotnet run --project api/api.csproj` | none |

Tasks use `$msCompile` problem matcher so errors appear in the VS Code Problems panel.

---

## 4. Dev Container

### 4.1 Base image

Use the official Microsoft dev container feature approach rather than a custom `Dockerfile`, to stay compatible with Codespaces and automatic updates:

```json
{
  "image": "mcr.microsoft.com/devcontainers/base:ubuntu-24.04",
  "features": {
    "ghcr.io/devcontainers/features/dotnet:2": { "version": "10.0" },
    "ghcr.io/devcontainers/features/node:1": { "version": "lts" },
    "ghcr.io/devcontainers/features/github-cli:1": {}
  }
}
```

Azure Functions Core Tools v4 and Claude Code CLI are both installed in `post-create.sh` via npm. The `devcontainers-contrib` feature for Azure Functions Core Tools was archived and is no longer resolvable; npm is the supported cross-platform install method.

### 4.2 Architecture compatibility

The base image `ubuntu-24.04` supports both `amd64` and `arm64`. All features listed above publish multi-arch images. No Rosetta emulation required on Apple Silicon.

For Codespaces, the default machine type (2-core, 8GB) is sufficient to build and run the solution.

### 4.3 VS Code extensions in container

The dev container JSON SHALL specify the same extensions as `.vscode/extensions.json` under `customizations.vscode.extensions` so they are auto-installed in Codespaces.

### 4.4 post-create.sh

```bash
#!/bin/bash
set -e
echo "==> Restoring .NET packages..."
dotnet restore /workspaces/5coy-archive/5coy-archive.sln
echo "==> Installing Claude Code CLI..."
npm install -g @anthropic-ai/claude-code
echo "==> Dev environment ready."
dotnet --version && node --version && gh --version && claude --version
```

The script is idempotent and exits non-zero on any failure so Codespaces surfaces the error rather than silently continuing.

### 4.5 Port forwarding

| Port | Service | Auto-forward |
|---|---|---|
| 5000 | Blazor dev server | Yes |
| 7071 | Azure Functions local | Yes |

---

## 5. Coding Standards Documents

### 5.1 Approach

Standards are written as Markdown files in `docs/standards/`. Each rule has:
- a unique ID (`CS-01`, `GIT-01`, `PR-01`, etc.)
- normative language (SHALL / SHOULD / MAY)
- a brief rationale
- a correct and incorrect example where applicable

This structure lets AI tools reference rules by ID in review comments and lets developers link to specific rules in PR discussions.

### 5.2 csharp-conventions.md — coverage

| Rule ID | Topic |
|---|---|
| CS-01 | File-scoped namespaces (SHALL use) |
| CS-02 | Nullable reference types (SHALL enable at project level) |
| CS-03 | `var` usage (SHOULD use when type is obvious from RHS) |
| CS-04 | Async method naming (`*Async` suffix, SHALL) |
| CS-05 | Service interface naming (`I*Service`, SHALL) |
| CS-06 | Constructor injection over property injection (SHALL) |
| CS-07 | `ArgumentNullException.ThrowIfNull` over manual null checks (SHOULD) |
| CS-08 | Blazor component parameters (SHALL use `[Parameter]` attribute) |
| CS-09 | No `@code` blocks in `.razor` files where a code-behind `.razor.cs` exists |
| CS-10 | `HttpClient` usage (SHALL inject, SHALL NOT construct directly except in `Program.cs`) |

### 5.3 git-conventions.md — coverage

| Rule ID | Topic |
|---|---|
| GIT-01 | Commit subject line ≤ 72 characters (SHALL) |
| GIT-02 | Commit subject in imperative mood ("Add", not "Added" or "Adding") (SHALL) |
| GIT-03 | Blank line between subject and body (SHALL when body present) |
| GIT-04 | Body explains *why*, not *what* (SHOULD) |
| GIT-05 | Branch naming: `feature/<issue>-<slug>`, `fix/<issue>-<slug>` (SHALL) |
| GIT-06 | No force-push to `dev` or `master` (SHALL NOT) |
| GIT-07 | Co-author trailer for AI-assisted commits (SHOULD) |

### 5.4 pr-conventions.md — coverage

| Rule ID | Topic |
|---|---|
| PR-01 | PR title ≤ 70 characters (SHALL) |
| PR-02 | PR body SHALL include a Summary section (bullet points) |
| PR-03 | PR body SHALL include a Test Plan section (checklist) |
| PR-04 | PRs to `master` SHALL target `dev` first (SHALL NOT open direct feature → master) |
| PR-05 | PRs SHALL link to the relevant issue (SHOULD use "Closes #N" syntax) |
| PR-06 | AI-generated PR bodies SHALL be marked with the Claude Code footer (SHOULD) |
