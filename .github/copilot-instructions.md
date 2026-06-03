# GitHub Copilot Instructions — Grants Applicant Portal

BC Government full-stack application for grant applicants. Angular 20 frontend + .NET 9 backend on OpenShift.

---

## Project Overview

```
applications/
├── Grants.ApplicantPortal/
│   ├── src/
│   │   ├── Grants.ApplicantPortal.Frontend/   # Angular 20 SPA (port 4200 dev / 4000 Docker)
│   │   └── Grants.ApplicantPortal.Backend/    # .NET 9 FastEndpoints API (port 7000 dev / 5100 Docker)
│   └── docker-compose.yml                     # Full stack: frontend, backend, PostgreSQL, Redis
└── Grants.AutoUI/                             # Cypress E2E test suite (targets deployed envs)
    └── cypress/
        ├── e2e/        # Spec files (*.cy.ts)
        ├── pages/      # Page Object Model classes
        └── support/    # Custom commands and reusable login flows
```

### Cypress E2E (Grants.AutoUI)

Specs run against deployed environments — not locally. After any UI change, use the **autoui-guardian** agent to:
1. Fix existing specs or page objects broken by the change (self-healing)
2. Create stub spec files for new user-facing features (with `it.skip` placeholders for QA to implement)

Backend-only changes require no AutoUI action.

---

## Frontend (Angular 20)

### Architecture

```
src/app/
├── core/          # Singletons: auth config, guards, interceptors, domain services
├── features/      # Smart (page-level) standalone components — lazy-loaded
├── layout/        # Header and shell — presentational only
└── shared/        # Dumb components, directives, models, utility services
```

**Layer rule**: Business logic → `core/services`. Page logic → `features`. Reusable UI → `shared`. Never import `features` from `core` or `shared`.

### Coding Conventions

- All components must be **standalone** (`standalone: true`) — no NgModules for features
- Routes use **`loadComponent`** (lazy) — never eager imports in `app.routes.ts`
- Use Angular 20 built-in control flow: `@if`, `@for`, `@switch` — not `*ngIf` / `*ngFor`
- Import individual directives (`RouterLink`, `AsyncPipe`) — never `CommonModule` or `RouterModule`
- **No `any` types** — define interfaces in `src/app/shared/models/`
- All HTTP calls return `Observable<T>` — never `Promise`; never subscribe inside a service
- Backend calls go through `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/src/app/api.service.ts` — components never call `HttpClient` directly

### Auth

- All authenticated pages are protected by `auth.guard.ts`
- `auth.interceptor.ts` attaches `Authorization: Bearer <token>` to every outgoing request
- Keycloak config is in `src/app/core/auth/auth.config.ts`

### Testing

- **Tests are mandatory** — every new component and service must have a `*.spec.ts` written in the same pass as the implementation
- Unit tests live next to source: `*.component.spec.ts`, `*.service.spec.ts`
- Use `TestBed` with `imports: [StandaloneComponent]` for standalone components — never `declarations`
- Use `HttpClientTestingModule` + `HttpTestingController` for services that make HTTP calls
- Minimum coverage: creates + key `@Input` bindings + primary method behaviour
- Run: `npm test -- --no-progress --watch=false --browsers=ChromeHeadless` from `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/`

---

## Backend (.NET 9)

### Architecture

```
src/
├── API.Web/           # FastEndpoints — one folder per domain, one file per action
├── API.UseCases/      # CQRS: Commands/Queries + Handlers (MediatR)
├── API.Core/          # Domain entities and interfaces
├── API.Infrastructure/# EF Core DbContext, repositories, Redis, external adapters
└── API.Migrations/    # Entity Framework Core migrations
```

### FastEndpoints Pattern

Each API action = **four files** in `API.Web/<Domain>/`:

| File | Purpose |
|---|---|
| `<Action>.cs` | Endpoint class — route, auth policy, summary, mediator dispatch |
| `<Action>.Request.cs` | Request record with `Route` const |
| `<Action>.Response.cs` | Response record |
| `<Action>.Validator.cs` | `AbstractValidator<TRequest>` (FluentValidation) |

Never use MVC controllers. Never put business logic in endpoints — dispatch via `_mediator.Send()`.

### CQRS Pattern

Each use case = **two files** in `API.UseCases/<Domain>/<Action>/`:

- `<Action><Domain>Command.cs` — `record` implementing `ICommand<Result<T>>`
- `<Action><Domain>Handler.cs` — class implementing `ICommandHandler`, returns `Result<T>`

Use `IQuery<Result<T>>` / `IQueryHandler` for read-only operations.

### Result Pattern (Ardalis.Result)

All handlers return `Result<T>`. Endpoints map results to HTTP responses:

```csharp
IsSuccess        → 200/201
NotFound         → SendNotFoundAsync()
Forbidden        → SendForbiddenAsync()
Invalid          → AddError() + SendErrorsAsync(422)
result.Errors    → AddError() + SendErrorsAsync(400)
```

Never throw exceptions for expected domain failures.

### Auth

- Every authenticated endpoint must call `Policies(AuthPolicies.RequireAuthenticatedUser)` in `Configure()`
- Extract the user profile with `HttpContext.GetRequiredProfile()` — never read JWT claims manually
- Auth is Keycloak OIDC (JWT Bearer tokens)

### Testing

- **Tests are mandatory** — every new handler must have a unit test written in the same pass as the implementation
- **Unit**: `tests/API.UnitTests/<Domain>/<HandlerName>Tests.cs` — mock dependencies with `Moq`, assert `Result<T>` values
- **Integration**: `tests/API.IntegrationTests/` — real PostgreSQL (no mocks)
- **Functional**: `tests/API.FunctionalTests/` — HTTP-level, real running app
- Run: `dotnet test` from `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/`

---

## Shared Conventions

### Naming

| Thing | Convention |
|---|---|
| Angular components | `kebab-case` selector, `PascalCase` class |
| Angular services | `PascalCase`, suffix `Service` |
| C# classes | `PascalCase` |
| C# records (Commands/Queries) | `<Action><Domain>Command` / `<Action><Domain>Query` |
| API routes | `/api/v1/<plural-resource>` |
| Git branches | `feature/AB#<ticket>-short-description` or `bugfix/AB#<ticket>-...` |

### Security

- No raw SQL strings — use EF Core parameterized queries or `FromSqlRaw` with parameters
- No secrets in code or config files — use environment variables or OpenShift secrets
- Validate all user input at the API boundary (FluentValidation on backend, Angular Reactive Forms on frontend)
- CORS is configured in `Program.cs` — do not add wildcard `*` origins

### What NOT to do

- Do not add `any` types in TypeScript
- Do not add `using var db = new AppDbContext()` — always inject `IApplicationDbContext`
- Do not add `NgModule` for new Angular features
- Do not call `HttpClient` directly from Angular components
- Do not put business logic inside FastEndpoints `HandleAsync` — it belongs in the Use Case handler
