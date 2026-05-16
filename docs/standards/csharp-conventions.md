# C# and Blazor Coding Conventions

**Scope:** All `.cs` and `.razor` files in this repository.  
**Keywords:** RFC 2119 (SHALL / SHOULD / MAY / SHALL NOT / SHOULD NOT)

---

## CS-01 — File-scoped namespaces

**Rule:** C# files SHALL use file-scoped namespace declarations.

**Rationale:** Reduces indentation by one level across the entire file with no semantic difference.

```csharp
// Correct
namespace Services;

public class ArticlesService { }
```

```csharp
// Incorrect
namespace Services
{
    public class ArticlesService { }
}
```

---

## CS-02 — Nullable reference types

**Rule:** Nullable reference types SHALL be enabled at the project level (`<Nullable>enable</Nullable>` in `.csproj`).

**Rationale:** Catches null-reference bugs at compile time. All new code must be nullable-aware; use `?` annotations where null is a valid value, and `!` only where provably non-null.

---

## CS-03 — `var` usage

**Rule:** `var` SHOULD be used when the type is immediately obvious from the right-hand side.

```csharp
// Correct — type obvious from constructor call
var service = new ArticlesService(httpClient, logger);

// Correct — type obvious from cast / literal
var count = 0;

// Incorrect — type not obvious from RHS
var result = GetArticles();
```

---

## CS-04 — Async method naming

**Rule:** Asynchronous methods SHALL use the `*Async` suffix.

```csharp
// Correct
public async Task<Article[]> GetItemsAsync(string sectionRef) { }

// Incorrect
public async Task<Article[]> GetItems(string sectionRef) { }
```

---

## CS-05 — Service interface naming

**Rule:** Service interfaces SHALL be named `I*Service`.

```csharp
// Correct
public interface IArticlesService { }
public interface IMissionsService { }

// Incorrect
public interface IArticles { }
public interface ArticlesService { }
```

---

## CS-06 — Constructor injection

**Rule:** Services SHALL receive dependencies via constructor injection. Property injection SHALL NOT be used.

**Rationale:** Constructor injection makes dependencies explicit and enables null-checking at construction time.

```csharp
// Correct
public class ArticlesService : IArticlesService
{
    private readonly HttpClient _httpClient;

    public ArticlesService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }
}

// Incorrect
public class ArticlesService : IArticlesService
{
    public HttpClient HttpClient { get; set; }
}
```

---

## CS-07 — Null argument checking

**Rule:** Constructor null checks SHOULD use `ArgumentNullException.ThrowIfNull` rather than manual `if (x == null) throw` guards.

```csharp
// Correct (.NET 6+)
public ArticlesService(HttpClient httpClient)
{
    ArgumentNullException.ThrowIfNull(httpClient);
    _httpClient = httpClient;
}

// Acceptable (legacy style, still valid)
public ArticlesService(HttpClient httpClient)
{
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
}
```

---

## CS-08 — Blazor component parameters

**Rule:** All Blazor component parameters SHALL use the `[Parameter]` attribute. `[CascadingParameter]` is permitted where cascade is the correct mechanism.

```razor
// Correct
@code {
    [Parameter] public string ArticleRef { get; set; }
}
```

```razor
// Incorrect
@code {
    public string ArticleRef { get; set; }
}
```

---

## CS-09 — Blazor code-behind

**Rule:** Razor components SHALL NOT contain `@code {}` blocks when a code-behind file (`.razor.cs`) exists for that component.

**Rationale:** Mixing `@code {}` and a code-behind creates two partial class fragments that can conflict and reduces discoverability.

```
// Correct — logic in code-behind
ArticleDetail.razor      → markup only, no @code block
ArticleDetail.razor.cs   → inherits ComponentBase, contains all logic
```

```razor
// Incorrect — @code block present alongside a .razor.cs file
@code {
    protected override async Task OnInitializedAsync() { ... }
}
```

---

## CS-10 — `HttpClient` usage

**Rule:** `HttpClient` SHALL be injected via DI. It SHALL NOT be constructed directly except in `Program.cs` where it is registered as a scoped service.

**Rationale:** Direct construction bypasses the base address configuration set in `Program.cs` and will produce incorrect URLs at runtime.

```csharp
// Correct — injection
public class ArticlesService
{
    public ArticlesService(HttpClient httpClient) { ... }
}
```

```csharp
// Incorrect — direct construction
var client = new HttpClient();
```

The registration in `Program.cs` is the single permitted point of construction:

```csharp
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
```
