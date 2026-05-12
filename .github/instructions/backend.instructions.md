---
applyTo: applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/**
---

# Backend Instructions — .NET 9

These rules apply to all files in the .NET backend (`applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/`).

## Architecture layers

```
src/
├── API.Web/            # HTTP layer — FastEndpoints, one folder per domain
├── API.UseCases/       # CQRS — Commands, Queries, Handlers (MediatR)
├── API.Core/           # Domain entities and interfaces
├── API.Infrastructure/ # EF Core DbContext, repositories, external adapters
└── API.Migrations/     # Entity Framework Core database migrations
```

## FastEndpoints (not controllers)

**Do not create MVC controllers.** Every API action is a FastEndpoints endpoint.

Each action = **four files** in `API.Web/<Domain>/`:

| File | Purpose |
|---|---|
| `<Action>.cs` | Endpoint — route, auth policy, summary, mediator dispatch |
| `<Action>.Request.cs` | Request record with `public const string Route` |
| `<Action>.Response.cs` | Response record |
| `<Action>.Validator.cs` | `AbstractValidator<TRequest>` (FluentValidation) |

## CQRS with MediatR

- Commands mutate state → implement `ICommand<Result<T>>`
- Queries read state → implement `IQuery<Result<T>>`
- Handlers implement `ICommandHandler` or `IQueryHandler`
- **Never put business logic inside an endpoint's `HandleAsync`** — dispatch to MediatR

## Ardalis.Result

All handlers return `Result<T>` or `Result`. Endpoints map results to HTTP:

```csharp
IsSuccess        → 200 / 201
NotFound         → SendNotFoundAsync()
Forbidden        → SendForbiddenAsync()
Invalid          → AddError() + SendErrorsAsync(422)
result.Errors    → AddError() + SendErrorsAsync(400)
```

**Never throw exceptions for expected domain failures** — use `Result` return values.

## Authentication

- Every authenticated endpoint must call `Policies(AuthPolicies.RequireAuthenticatedUser)` inside `Configure()`
- Extract the current user with `HttpContext.GetRequiredProfile()` — never decode JWT claims manually
- Validate resource ownership in the handler (profile ID must match the resource owner)

## Data access

- Inject `IApplicationDbContext` — never `new AppDbContext()`
- All queries use EF Core — **no raw SQL strings**; if raw SQL is necessary use parameterised `FromSqlRaw`
- Migrations live in `API.Migrations/` — run with `dotnet ef migrations add <Name> --project src/Grants.ApplicantPortal.API.Migrations --startup-project src/Grants.ApplicantPortal.API.Web` from the backend root

## Validation

- Every request type must have a corresponding `AbstractValidator` with `RuleFor` covering all required fields
- String fields: `NotEmpty()` for required, `MaximumLength()` for all free-text inputs

## Testing

- **Unit tests**: `tests/API.UnitTests/` — mock repositories, test handlers in isolation
- **Integration tests**: `tests/API.IntegrationTests/` — real PostgreSQL, no mocks
- **Functional tests**: `tests/API.FunctionalTests/` — HTTP-level against the running app
- Do not mock the database in integration or functional tests
- Run: `dotnet test` from `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/`

## NuGet packages

- All package versions are managed centrally in `Directory.Packages.props` — do not add `Version="..."` attributes to individual `.csproj` files
