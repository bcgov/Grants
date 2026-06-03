# Grants Applicant Portal — Backend

.NET 9 Web API for BC Government grants applicants. Deployed as a Docker container on OpenShift.

## Tech Stack

- **Framework**: FastEndpoints (endpoint-per-feature, not MVC controllers)
- **CQRS**: MediatR with Commands/Queries in `UseCases/`, Handlers co-located with Commands
- **Result Pattern**: Ardalis.Result — all handlers return `Result<T>` or `Result`
- **Validation**: FluentValidation — one validator per endpoint request
- **ORM**: Entity Framework Core with PostgreSQL (migrations in `API.Migrations`)
- **Cache**: Redis via `IDistributedCache`
- **Auth**: Keycloak JWT — policy `RequireAuthenticatedUser` on all authenticated endpoints
- **Testing**: xUnit (unit, integration, functional, Aspire)
- **Service Discovery**: .NET Aspire (`API.AspireHost`)

## Common Commands

```bash
# From src/Grants.ApplicantPortal.Backend/
dotnet run --project src/Grants.ApplicantPortal.API.Web          # API at https://localhost:7000
dotnet test                                                       # all test suites
dotnet test tests/Grants.ApplicantPortal.API.UnitTests            # unit tests only
dotnet test tests/Grants.ApplicantPortal.API.IntegrationTests
dotnet test tests/Grants.ApplicantPortal.API.FunctionalTests

# Migrations (from src/Grants.ApplicantPortal.Backend/)
dotnet ef migrations add <Name> --project src/Grants.ApplicantPortal.API.Migrations --startup-project src/Grants.ApplicantPortal.API.Web
dotnet ef database update --project src/Grants.ApplicantPortal.API.Migrations --startup-project src/Grants.ApplicantPortal.API.Web
```

## Architecture

```
src/
├── API.Web/                    # FastEndpoints — one folder per domain area
│   ├── Addresses/              # Create.cs, Create.Request.cs, Create.Response.cs, Create.Validator.cs
│   ├── Contacts/
│   ├── Organizations/
│   ├── Submissions/
│   ├── Payments/
│   └── Auth/
├── API.UseCases/               # CQRS: Commands, Queries, Handlers
│   ├── Addresses/
│   │   ├── Create/             # CreateAddressCommand.cs + CreateAddressHandler.cs
│   │   ├── Retrieve/
│   │   └── Delete/
│   └── ...
├── API.Core/                   # Domain entities, interfaces, domain services
├── API.Core.Features/          # Feature flags
├── API.Infrastructure/         # EF DbContext, repositories, external service adapters
├── API.Migrations/             # EF Core migrations
├── API.Plugins/                # Plugin system
├── API.ServiceDefaults/        # Aspire service defaults (health checks, telemetry)
└── API.AspireHost/             # Local orchestration via .NET Aspire
```

**Rule**: Endpoints dispatch to handlers via MediatR. Handlers own business logic. Never put business logic in endpoints.

## Endpoint Pattern

Each endpoint lives in `API.Web/<Domain>/` and consists of four files:

| File | Purpose |
|---|---|
| `<Action>.cs` | Endpoint class — inherits `Endpoint<TRequest, TResponse>`, configures route/auth/summary, calls `_mediator.Send()` |
| `<Action>.Request.cs` | Request record with `Route` const — e.g. `"/api/v1/addresses"` |
| `<Action>.Response.cs` | Response record |
| `<Action>.Validator.cs` | FluentValidation `AbstractValidator<TRequest>` |

Use `/new-endpoint <domain> <action>` to scaffold these files.

## Use Case Pattern

Each use case lives in `API.UseCases/<Domain>/<Action>/`:

| File | Purpose |
|---|---|
| `<Action><Domain>Command.cs` or `<Action><Domain>Query.cs` | MediatR record implementing `ICommand<Result<T>>` or `IQuery<Result<T>>` |
| `<Action><Domain>Handler.cs` | Handler implementing `ICommandHandler` or `IQueryHandler` |

Use `/new-use-case <domain> <action>` to scaffold these files.

## Result Pattern

All handlers return `Result<T>` (Ardalis.Result). Endpoints map results to HTTP responses:

```csharp
if (result.IsSuccess)   → return 200/201 with result.Value
ResultStatus.NotFound   → SendNotFoundAsync()
ResultStatus.Forbidden  → SendForbiddenAsync()
ResultStatus.Invalid    → AddError() + SendErrorsAsync(422)
result.Errors.Any()     → AddError() + SendErrorsAsync(400)
```

## Key Files

| File | Purpose |
|---|---|
| `src/API.Web/Program.cs` | App startup — FastEndpoints, auth, middleware registration |
| `src/API.Web/Auth/AuthPolicies.cs` | Named auth policy constants |
| `src/API.Web/Extensions/HttpContextExtensions.cs` | `GetRequiredProfile()` — extracts Keycloak profile from JWT |
| `src/API.Infrastructure/Data/AppDbContext.cs` | EF Core DbContext |
| `Directory.Packages.props` | Centralized NuGet version management |

## Scaffolding

| Task | Command |
|---|---|
| New endpoint + request/response/validator | `/new-endpoint <domain> <action>` |
| New use case (command/query + handler) | `/new-use-case <domain> <action>` |
| New EF migration | `/new-migration <MigrationName>` |
| Run tests | `/run-tests [suite]` |

## Testing Conventions

- **Unit tests**: `tests/API.UnitTests/` — test handlers in isolation, mock repositories
- **Integration tests**: `tests/API.IntegrationTests/` — test with real DB (no mocks)
- **Functional tests**: `tests/API.FunctionalTests/` — HTTP-level tests against the running app
- Test class naming: `<Subject>Tests` — e.g. `CreateAddressHandlerTests`
- Do not mock the database in integration or functional tests
