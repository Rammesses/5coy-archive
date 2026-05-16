# Requirements: AI Coding Support

**Feature:** AI Coding Support  
**Issue:** #48  
**Spec:** `docs/specs/48-ai-coding-support.md`  
**Phase:** 1 — Local Developer Experience  
**Status:** Awaiting review  

---

## Requirement 1 — Project Intelligence Document (CLAUDE.md)

**User Story:** As a developer opening this repository for the first time, I want a single authoritative document that an AI coding assistant can read to immediately understand the project, so that I get accurate, project-specific help without having to manually explain the architecture, conventions, or toolchain.

### Acceptance Criteria

1. GIVEN a developer opens this repository with Claude Code (or any AI assistant that reads context files), WHEN the assistant reads `CLAUDE.md`, THEN it SHALL correctly describe the project purpose, tech stack, and repository structure without additional prompting.

2. GIVEN a developer asks their AI assistant "how do I run the app locally?", WHEN the assistant reads `CLAUDE.md`, THEN it SHALL return the exact correct `dotnet run` command and the URL to open in the browser.

3. GIVEN a developer asks their AI assistant "what is the branching strategy?", WHEN the assistant reads `CLAUDE.md`, THEN it SHALL correctly describe the `master` → `dev` → `feature/*` flow and PR targets.

4. GIVEN a developer asks their AI assistant "how do I add a new page?", WHEN the assistant reads `CLAUDE.md`, THEN it SHALL describe the correct location for new Razor pages and reference the naming convention.

5. GIVEN a pull request changes the architecture, toolchain, or a documented convention, THEN the PR description SHOULD note whether `CLAUDE.md` has been updated accordingly.

---

## Requirement 2 — VS Code Workspace Configuration

**User Story:** As a developer using VS Code, I want the repository to configure my editor automatically on first open, so that I have the correct extensions, formatting rules, and run tasks without manual setup.

### Acceptance Criteria

1. GIVEN a developer opens the repository in VS Code, WHEN VS Code reads `.vscode/extensions.json`, THEN it SHALL prompt to install all recommended extensions, including a C#/Blazor extension and at least one AI coding assistant extension.

2. GIVEN a developer has the recommended extensions installed, WHEN they open a `.cs` or `.razor` file, THEN the editor SHALL format on save using the project's conventions (4-space indentation, no trailing whitespace).

3. GIVEN a developer opens the VS Code Command Palette and runs "Tasks: Run Task", WHEN they view the task list, THEN they SHALL see tasks for: Build, Test, Run (web), and Run (api).

4. GIVEN a developer runs the "Build" task, THEN it SHALL execute `dotnet build 5coy-archive.sln` and report success or failure in the Terminal panel.

5. GIVEN a developer runs the "Test" task, THEN it SHALL execute `dotnet test 5coy-archive.sln` and report results in the Terminal panel.

6. GIVEN a developer runs the "Run (web)" task, THEN it SHALL start the Blazor dev server and open `http://localhost:5000` in the default browser.

7. GIVEN VS Code configuration is committed to the repository, THEN `.vscode/settings.json` SHALL NOT contain any user-specific paths, machine-specific settings, or credentials.

---

## Requirement 3 — Dev Container

**User Story:** As a developer (or contributor) who does not want to install the .NET SDK, Azure Functions tools, or Node on their local machine, I want to open this repository in a pre-configured container — either locally via Docker or in GitHub Codespaces — so that I can build, test, and run the project immediately with no local setup.

### Acceptance Criteria

1. GIVEN a developer has Docker Desktop installed and opens the repository in VS Code, WHEN they select "Reopen in Container", THEN the dev container SHALL build and start without errors.

2. GIVEN a developer opens the repository in GitHub Codespaces, WHEN the Codespace starts, THEN the dev container SHALL build and start without errors.

3. GIVEN the dev container has started, WHEN the developer runs `dotnet build 5coy-archive.sln`, THEN the build SHALL succeed with 0 errors and 0 warnings.

4. GIVEN the dev container has started, WHEN the developer runs `dotnet test 5coy-archive.sln`, THEN all tests SHALL pass.

5. GIVEN the dev container has started, WHEN the developer runs `gh --version`, THEN the GitHub CLI SHALL be available and report a version number.

6. GIVEN the dev container has started, WHEN the developer runs `claude --version`, THEN the Claude Code CLI SHALL be available and report a version number.

7. GIVEN the dev container has started, WHEN the developer runs `node --version` and `npm --version`, THEN both SHALL be available (required for wrangler / Cloudflare tooling).

8. GIVEN the dev container has started, WHEN a post-create script runs, THEN it SHALL execute `dotnet restore 5coy-archive.sln` automatically so the developer does not need to do this manually.

9. GIVEN a developer is running on Apple Silicon (arm64), WHEN they open the dev container locally, THEN the container SHALL run natively on arm64 without Rosetta emulation.

---

## Requirement 4 — Coding Standards Documentation

**User Story:** As a developer or AI assistant working on this codebase, I want explicit, machine-readable standards documents so that code contributions are consistent with existing patterns and review feedback is predictable.

### Acceptance Criteria

1. GIVEN a developer asks their AI assistant "what commit message format should I use?", WHEN the assistant reads the standards documents, THEN it SHALL return the correct format with an example.

2. GIVEN a developer asks their AI assistant "how should I name a new Blazor service?", WHEN the assistant reads the standards documents, THEN it SHALL describe the naming convention and give a correct example.

3. GIVEN a developer asks their AI assistant to review a code change, WHEN the assistant reads the standards documents, THEN it SHALL reference specific standards by ID when raising a finding (e.g. "this violates CS-03").

4. GIVEN the standards documents are committed to the repository, THEN every rule SHALL use normative language (SHALL / SHOULD / MAY / SHALL NOT / SHOULD NOT) so that AI tools can distinguish requirements from guidance.

5. GIVEN the standards documents exist, THEN they SHALL cover at minimum:
   - C# coding conventions for this project (naming, structure, nullable usage)
   - Blazor component conventions (code-behind vs inline, parameter naming)
   - Git commit message format
   - PR description format and required sections
   - File and folder naming rules
