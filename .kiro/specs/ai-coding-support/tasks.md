# Tasks: AI Coding Support

**Feature:** AI Coding Support  
**Issue:** #48  
**Phase:** 1 — Local Developer Experience  
**Status:** Awaiting review  

---

## Phase 1 Tasks

### Task 1 — Write CLAUDE.md

**Requirement:** Req 1  
**Deliverable:** `CLAUDE.md` at repository root

- [ ] 1.1 Create `CLAUDE.md` with all sections defined in design §2.2:
  - Purpose
  - Tech Stack (with exact versions)
  - Repository Structure (per-project descriptions)
  - Build, Test & Run (exact commands)
  - Architecture (data loading, WASM model, API placeholder status)
  - Key Patterns (service injection, data loading, Blazor component structure)
  - Branching & PR Strategy
  - Coding Conventions (summary, referencing `docs/standards/`)
  - Known Constraints & Gotchas (all items from design §2.4)
- [ ] 1.2 Verify AC-02: ask Claude Code in the repo root to describe the project architecture, `dotnet run` command, and branching strategy — all must be correct without additional prompting

**Dependencies:** None  
**Estimated effort:** Small (write once)

---

### Task 2 — VS Code Workspace Configuration

**Requirement:** Req 2  
**Deliverable:** `.vscode/extensions.json`, `.vscode/settings.json`, `.vscode/tasks.json`

- [ ] 2.1 Create `.vscode/extensions.json` with the seven recommended extensions listed in design §3.1
- [ ] 2.2 Create `.vscode/settings.json` with the seven settings listed in design §3.2 (including the `[razor]` override disabling `formatOnSave`)
- [ ] 2.3 Create `.vscode/tasks.json` with four tasks: Build, Test, Run (web), Run (api), all as defined in design §3.3
- [ ] 2.4 Verify no user-specific paths, machine-specific settings, or credentials appear in any `.vscode/` file (Req 2 AC-7)
- [ ] 2.5 Smoke-test: open repository in VS Code, confirm:
  - Extension recommendations prompt appears
  - "Tasks: Run Task" shows all four tasks
  - Build task runs `dotnet build 5coy-archive.sln` and reports success

**Dependencies:** None  
**Estimated effort:** Small

---

### Task 3 — Dev Container

**Requirement:** Req 3  
**Deliverable:** `.devcontainer/devcontainer.json`, `.devcontainer/post-create.sh`

- [ ] 3.1 Create `.devcontainer/devcontainer.json` with:
  - Base image `mcr.microsoft.com/devcontainers/base:ubuntu-24.04`
  - Features: `dotnet:2` (v10.0), `node:1` (lts), `github-cli:1`, `azure-functions-core-tools:2`
  - Port forwarding: 5000 (auto), 7071 (auto)
  - `customizations.vscode.extensions` containing the same seven extensions as `.vscode/extensions.json`
- [ ] 3.2 Create `.devcontainer/post-create.sh` with the script defined in design §4.4:
  - `set -e`
  - `dotnet restore 5coy-archive.sln`
  - `npm install -g @anthropic-ai/claude-code`
  - Version verification (`dotnet`, `node`, `gh`, `claude`)
- [ ] 3.3 Make `post-create.sh` executable (`chmod +x`)
- [ ] 3.4 Add `postCreateCommand` to `devcontainer.json` pointing to the script
- [ ] 3.5 Verify: open repository in local Docker dev container (or Codespaces); confirm `dotnet build 5coy-archive.sln` succeeds and `claude --version` responds

**Dependencies:** Task 2 (extensions list must be finalised before duplicating into devcontainer.json)  
**Estimated effort:** Small–Medium (container build time for verification)

---

### Task 4 — Coding Standards Documents

**Requirement:** Req 4  
**Deliverable:** `docs/standards/csharp-conventions.md`, `docs/standards/git-conventions.md`, `docs/standards/pr-conventions.md`

- [ ] 4.1 Create `docs/standards/` directory
- [ ] 4.2 Write `docs/standards/csharp-conventions.md` covering rules CS-01 through CS-10 as defined in design §5.2:
  - Each rule: unique ID, normative language (SHALL/SHOULD/MAY), rationale, correct and incorrect example
- [ ] 4.3 Write `docs/standards/git-conventions.md` covering rules GIT-01 through GIT-07 as defined in design §5.3
- [ ] 4.4 Write `docs/standards/pr-conventions.md` covering rules PR-01 through PR-06 as defined in design §5.4
- [ ] 4.5 Verify all rules use normative language (no "should" without capitalisation, no ambiguous phrasing)
- [ ] 4.6 Verify `CLAUDE.md` §Coding Conventions references `docs/standards/` and the correct file names

**Dependencies:** Task 1 (CLAUDE.md must reference these files by final path)  
**Estimated effort:** Medium (writing 23 rules with examples)

---

### Task 5 — Commit and Raise PR

**Deliverable:** PR from `feature/48-ai-coding-support` → `dev`

- [ ] 5.1 Stage and commit all Phase 1 deliverables:
  - `CLAUDE.md`
  - `.vscode/extensions.json`, `settings.json`, `tasks.json`
  - `.devcontainer/devcontainer.json`, `post-create.sh`
  - `docs/standards/csharp-conventions.md`, `git-conventions.md`, `pr-conventions.md`
  - `.kiro/specs/ai-coding-support/requirements.md`, `design.md`, `tasks.md`
- [ ] 5.2 Raise PR to `dev` with summary of all deliverables and a checklist linking each to its requirement
- [ ] 5.3 Verify AC-01: a developer following the dev container path can run `dotnet build` and `dotnet test` within 10 minutes of cloning
- [ ] 5.4 Verify AC-02: Claude Code correctly describes architecture, commands, and branching without prompting
- [ ] 5.5 Verify AC-05: all VS Code configuration loads without errors on the current machine
- [ ] 5.6 Verify AC-06: no secrets or API keys present in any committed file

**Dependencies:** Tasks 1–4 complete  
**Estimated effort:** Small

---

## Implementation Order

```
Task 1 (CLAUDE.md)
    │
    ├─► Task 2 (VS Code)  ──┐
    │                        ├─► Task 3 (Dev Container)
    └─► Task 4 (Standards) ─┘
                              │
                              └─► Task 5 (Commit & PR)
```

Tasks 2 and 4 can run in parallel after Task 1 is complete. Task 3 depends on Task 2 (for the extensions list). Task 5 depends on all previous tasks.

---

## Out of Scope (Phase 2)

The following are explicitly deferred:

- GitHub Actions workflow for automated AI code review (FR-14 through FR-18)
- Multi-provider AI backend selection via repository variable
- Automated PR summary generation
