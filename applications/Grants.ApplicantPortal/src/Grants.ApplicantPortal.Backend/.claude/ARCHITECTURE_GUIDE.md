# Grants Applicant Portal — Backend Architecture Guide

Authoritative reference for all backend structural decisions.
Claude Code checks new plans against this guide before implementing (Phase 3 compliance gate).

---

## Project structure — one rule per layer

```
API.Web/          HTTP only — route, auth policy, request mapping, mediator dispatch, response mapping
API.UseCases/     Business logic only — commands, queries, handlers
API.Core/         Domain entities and interfaces — no infrastructure dependencies
API.Infrastructure/ EF Core DbContext, repositories, Redis, external adapters
API.Migrations/   EF Core migrations — generated, not hand-edited
```

**Never skip layers.** `API.Web` cannot call `API.Infrastructure` directly. `API.UseCases` cannot reference `API.Web`.

---

## FastEndpoints (not controllers)

Every HTTP action = **exactly four files** in `API.Web/<Domain>/`:

| File | Contains |
| --- | --- |
| `<Action>.cs` | Endpoint class — `Configure()` + `HandleAsync()` only |
| `<Action>.Request.cs` | Request record with `public const string Route` |
| `<Action>.Response.cs` | Response record |
| `<Action>.Validator.cs` | `AbstractValidator<TRequest>` |

**Never**:
- MVC `[ApiController]` / `ControllerBase` classes
- Business logic inside `HandleAsync` — it must only call `_mediator.Send()` and map the result
- Combining request + response into a single file
- Skipping the validator file (even if validation is simple)

---

## CQRS via MediatR

Every use case = **two files** in `API.UseCases/<Domain>/<Action>/`:

| File | Contains |
| --- | --- |
| `<Action><Domain>Command.cs` | `record` implementing `ICommand<Result<T>>` |
| `<Action><Domain>Handler.cs` | `class` implementing `ICommandHandler<TCommand, Result<T>>` |

Use `IQuery<Result<T>>` / `IQueryHandler` for read-only operations.

**Never**:
- Business logic in endpoint `HandleAsync`
- Database calls directly from an endpoint
- Queries that mutate state
- Commands that return raw values instead of `Result<T>`

---

## Ardalis.Result — result mapping

All handlers return `Result<T>`. The endpoint maps the result to HTTP:

```csharp
if (result.IsSuccess)          → Response = result.Value; return;
ResultStatus.NotFound          → await SendNotFoundAsync(ct);
ResultStatus.Forbidden         → await SendForbiddenAsync(ct);
ResultStatus.Invalid           → AddError(e.ErrorMessage); await SendErrorsAsync(422, ct);
result.Errors.Any()            → AddError(e); await SendErrorsAsync(400, ct);
```

**Never**:
- `throw new Exception(...)` for expected domain failures — use `Result.Forbidden()`, `Result.NotFound()`, `Result.Invalid()`
- Return raw values from handlers — always wrap in `Result`
- HTTP status codes outside the above set without explicit justification

---

## Authentication — non-negotiable

Every authenticated endpoint **must** have in `Configure()`:

```csharp
Policies(AuthPolicies.RequireAuthenticatedUser);
```

The current user is always retrieved via:

```csharp
var profile = HttpContext.GetRequiredProfile();
```

**Never**:
- Read JWT claims manually from `HttpContext.User.Claims`
- Trust identity information from the request body
- Skip the policy on an endpoint because "it's just a GET"
- Validate ownership in the endpoint — ownership checks belong in the handler

---

## FluentValidation — required for every endpoint

Every request type must have a corresponding `AbstractValidator<TRequest>`.

Minimum rules:
- `RuleFor(x => x.RequiredField).NotEmpty()` for every required property
- `RuleFor(x => x.TextField).MaximumLength(n)` for all string inputs

**Never**:
- Validate input inside `HandleAsync` or a handler
- Skip the validator file because the handler does its own checks
- Use `MaximumLength` > 500 without explicit justification

---

## Entity Framework Core

- Always inject `IApplicationDbContext` — never `new AppDbContext()`
- All queries: EF Core LINQ — no raw SQL strings
- If raw SQL is unavoidable: `FromSqlRaw` with parameterised inputs only
- Migrations: generated via `dotnet ef migrations add` — never hand-edited
- Navigation property loading: explicit `.Include()` — never lazy loading

---

## NuGet package versioning

All versions are centralised in `Directory.Packages.props`.

**Never**:
- Add `Version="x.y.z"` to individual `.csproj` references
- Add a new NuGet package without checking if it's already in `Directory.Packages.props`
- Add a package that duplicates functionality already provided by an existing dependency

---

## Naming conventions

| Thing | Convention | Example |
| --- | --- | --- |
| Endpoint class | `<Action>` | `Create`, `Update`, `Delete`, `RetrieveAddresses` |
| Request record | `<Action><Domain>Request` | `CreateAddressRequest` |
| Response record | `<Action><Domain>Response` | `CreateAddressResponse` |
| Command record | `<Action><Domain>Command` | `CreateAddressCommand` |
| Query record | `<Action><Domain>Query` | `RetrieveAddressesQuery` |
| Handler class | `<Action><Domain>Handler` | `CreateAddressHandler` |
| Route constant | `Route` on the request record | `/api/v1/addresses` |

---

## What requires a deviation confirmation

The compliance gate will pause and ask for explicit confirmation if the plan includes:

- A `ControllerBase` or `[ApiController]` class anywhere in `API.Web`
- Business logic (database calls, calculations, domain rules) directly inside `HandleAsync`
- A handler that throws an exception for an expected domain failure instead of returning `Result`
- An endpoint missing `Policies(AuthPolicies.RequireAuthenticatedUser)`
- A request type with no corresponding `AbstractValidator`
- Raw SQL strings (not `FromSqlRaw` with parameters)
- `new AppDbContext()` or direct `DbContext` construction
- A `Version=` attribute on a `.csproj` NuGet reference
- A handler that returns a plain value instead of `Result<T>`
- Ownership/authorisation logic placed in the endpoint instead of the handler
