# 5coy-archive — Project Intelligence

This document is the authoritative context file for AI coding assistants working in this repository. It is written for an AI reader first, a human reader second.

---

## Purpose

5coy-archive is the source for [5coy-archive.org.uk](https://5coy-archive.org.uk) — an archival website for the 5th Company, Cheshire Military Corps. It presents articles, missions, personnel, sections, timelines, and related media.

---

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| Web UI | Blazor WebAssembly (`Microsoft.NET.Sdk.BlazorWebAssembly`) | .NET 10 |
| UI components | Blazor Bootstrap | 3.5.0 |
| Markdown rendering | Markdig | 1.2.0 |
| HTML sanitisation | HtmlSanitizer | 9.0.892 |
| Browser logging | Blazor.Extensions.Logging | 2.0.4 |
| API | Azure Functions v4, isolated worker model | net10.0 |
| Analytics | Application Insights (raw JS snippet in `index.html`) | — |
| Hosting | Azure Static Web Apps | — |
| Media CDN | Cloudflare Edge Worker → Amazon S3 | — |
| Infrastructure | Bicep (`infra/`) | — |
| CI/CD | GitHub Actions | — |

---

## Repository Structure

```
5coy-archive/
├── 5coy-archive.sln          # Root solution — ALWAYS use this for builds
├── web/                      # Blazor WASM application (the site)
│   ├── Pages/                # Razor page components
│   ├── Services/             # Data-access services (injected via DI)
│   ├── Shared/               # Layout and nav components
│   ├── wwwroot/
│   │   ├── data/             # All JSON data files loaded at runtime
│   │   └── index.html        # Entry point; contains AppInsights JS snippet
│   ├── Program.cs            # WASM host bootstrap and DI registration
│   └── web.csproj
├── web.test/                 # bUnit + NUnit tests for the web project
├── api/                      # Azure Functions isolated worker (placeholder)
│   ├── MediaRedirect.cs      # Single HTTP-trigger function
│   ├── Program.cs            # Isolated worker entry point
│   └── api.csproj
├── api.test/                 # MSTest tests for the api project
├── web2/                     # Experimental Blazor project — ignore for now
├── docs/
│   ├── specs/                # OpenSpec feature specs
│   └── standards/            # Coding, git and PR conventions (see below)
├── infra/
│   ├── azure/                # Bicep templates for Azure resources
│   └── cloudflare/           # Cloudflare Worker source
├── .kiro/specs/              # Kiro-style planning docs (requirements/design/tasks)
├── .github/workflows/        # CI/CD pipelines
├── .devcontainer/            # Dev container definition (Docker + Codespaces)
├── .vscode/                  # VS Code workspace configuration
└── package.json              # wrangler (Cloudflare Worker tooling)
```

---

## Build, Test & Run

All commands run from the repository root using the **root solution** `5coy-archive.sln`.

```bash
# Build
dotnet build 5coy-archive.sln

# Test
dotnet test 5coy-archive.sln

# Run the web app locally
dotnet run --project web/web.csproj
# → opens at http://localhost:5000

# Run the API locally (optional — not called by the web app)
dotnet run --project api/api.csproj
# → Azure Functions host at http://localhost:7071
```

> **Never use `web/web.sln`** — it exists as an artefact but is not the authoritative solution file. Always use `5coy-archive.sln`.

---

## Architecture

### Data Loading

All data is loaded at runtime from static JSON files served alongside the WASM bundle:

```
web/wwwroot/data/
├── articles.json       # Article groups with optional per-group data files
├── sections.json       # Navigation sections hierarchy
├── missions.json       # Missions
├── personnel.json      # Personnel records
└── videos.json         # Video metadata
```

Services (`web/Services/`) fetch data via an injected `HttpClient` using `GetFromJsonAsync<T>`. There are **no server-side API calls at runtime** — the Azure Functions API project is a placeholder and is currently not wired to the web app.

### Service Pattern

Each domain has an interface and implementation registered as scoped in `Program.cs`:

```csharp
builder.Services.AddScoped<IArticlesService, ArticlesService>();
```

Services lazy-load their data on first access and cache it in private fields. They receive `HttpClient` and `ILogger<T>` via constructor injection.

### Adding a New Page

1. Create `web/Pages/MyPage.razor` with `@page "/my-route"` directive.
2. Add a link to `web/Shared/NavMenu.razor` if it needs navigation.
3. Inject required services: `@inject IArticlesService ArticlesService`
4. Prefer code-behind (`.razor.cs`) over `@code {}` blocks for logic-heavy pages.

### Markdown Rendering

Markdown content is processed by `IMarkdownService` (using Markdig) then sanitised by `IHtmlSanitizer` (HtmlSanitizer) before rendering as raw HTML via `@((MarkupString)html)`.

---

## Key Patterns

### Constructor Injection

Services always receive dependencies via constructor, never property injection:

```csharp
public class ArticlesService : IArticlesService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArticlesService> _logger;

    public ArticlesService(HttpClient httpClient, ILogger<ArticlesService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

### Async Data Access

All service methods are `async Task<T>` with `*Async` suffix (see CS-04 in `docs/standards/csharp-conventions.md`). Data is loaded lazily via `EnsureDataLoadedAsync()` pattern.

### Blazor Component Parameters

Component parameters use the `[Parameter]` attribute:

```csharp
[Parameter] public string ArticleRef { get; set; }
```

---

## Branching & PR Strategy

```
master  ← production (deploys to Azure Static Web Apps on push)
  └── dev  ← integration branch
        └── feature/<issue>-<slug>   ← feature work
        └── fix/<issue>-<slug>       ← bug fixes
```

- Feature branches target `dev` in PRs (never `master` directly).
- `dev` → `master` PRs are release PRs.
- Commit message format: imperative mood, ≤ 72 chars subject line (see `docs/standards/git-conventions.md`).

---

## Coding Conventions (summary)

Full rules with examples are in `docs/standards/`:

- `csharp-conventions.md` — C# and Blazor rules (CS-01 through CS-10)
- `git-conventions.md` — Commit and branch rules (GIT-01 through GIT-07)
- `pr-conventions.md` — PR format rules (PR-01 through PR-06)

Quick reference:
- File-scoped namespaces (CS-01)
- Nullable reference types enabled (CS-02)
- `var` when type is obvious from RHS (CS-03)
- `*Async` suffix on async methods (CS-04)
- `I*Service` interface naming (CS-05)
- No `@code` blocks when a `.razor.cs` code-behind exists (CS-09)

---

## Known Constraints & Gotchas

### `web/web.sln` exists but is not the authoritative solution
Always use `5coy-archive.sln` at the repository root for all builds and test runs. `web/web.sln` is an artefact that can cause confusion.

### `blazor.boot.json` does not exist in .NET 10
.NET 10 replaced `blazor.boot.json` with a fingerprinted asset manifest. A 404 on `/_framework/blazor.boot.json` is expected and harmless.

### `System.Net.Http.Json` must NOT be added as a package reference
It is part of the .NET 10 BCL. Adding it as a NuGet package reference causes a version conflict.

### `Microsoft.AspNetCore.Cors` must NOT be referenced from the Blazor WASM project
This is a server-side package. Referencing it from `web.csproj` causes a build failure.

### `BlazorApplicationInsights` is incompatible with .NET 10 — do not re-add it
The package only ships net8.0/net9.0 targets. Under the .NET 10 WASM runtime, `BlazorApplicationInsights.Models.Config` cannot be resolved, causing a `ManagedError` at startup that breaks the entire application. Application Insights is instrumented via a raw JS snippet in `web/wwwroot/index.html` instead.

### Azure Functions API uses isolated worker model
`api/` targets `Microsoft.Azure.Functions.Worker` (isolated worker), not the deprecated `Microsoft.Azure.WebJobs.*` in-process model. The in-process model is not supported on .NET 10.

### The API is not called by the web app at runtime
`api/` is a placeholder. The web app loads all data from static JSON files. Do not add `HttpClient` calls from `web/` to `http://localhost:7071` — they will fail in production where CORS is not configured.
