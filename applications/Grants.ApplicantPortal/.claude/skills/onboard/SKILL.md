---
name: onboard
description: New developer orientation for the Grants Applicant Portal — tours the codebase, explains key architecture patterns, answers "how do I..." questions, and produces a personalised cheat sheet.
---

Run a new developer onboarding session for the Grants Applicant Portal.

Focus area (optional): $ARGUMENTS

Examples: `onboard frontend`, `onboard backend`, `onboard full-stack`

Default (no arguments): full-stack overview.

---

## Phase 1 — Welcome and scope

Ask the developer:
1. What is your background? (e.g. "strong Angular, new to .NET" / "backend dev, first Angular project")
2. What area will you work on first? (frontend / backend / both)
3. Any specific questions you already have?

Tailor the depth of the tour based on their answers.

---

## Phase 2 — Codebase tour

Use the **Explore** sub-agent to read key files and produce a guided tour.

### If frontend is in scope

Walk through:
- `src/Grants.ApplicantPortal.Frontend/src/app/` — the four layers (core / features / layout / shared)
- `app.routes.ts` — how routing works, the auth guard pattern
- `app.config.ts` — Angular providers and Keycloak setup
- `api.service.ts` — how HTTP calls are made
- One complete feature (e.g. `features/workspace/`) — component, route, service call end-to-end
- `server.js` — the Express container, proxy, health checks

### If backend is in scope

Walk through:
- The six projects and their responsibilities (`API.Web`, `API.UseCases`, `API.Core`, `API.Infrastructure`, `API.Migrations`, `API.ServiceDefaults`)
- One complete endpoint end-to-end (e.g. `Addresses/Create`) — endpoint → command → handler → result → response
- `Program.cs` — FastEndpoints registration, auth, middleware
- `AppDbContext` — EF Core setup, how entities are configured
- `Directory.Packages.props` — centralised NuGet versioning

---

## Phase 3 — Key patterns explained

Explain in plain language (adapted to their background from Phase 1):

- **How auth works end-to-end**: Keycloak → login redirect → JWT → Bearer token → `auth.guard` / `auth.interceptor` → `RequireAuthenticatedUser` policy → `GetRequiredProfile()`
- **How a feature is built**: ticket → endpoint → use case → handler → frontend service → component
- **How to run the stack locally**: `docker-compose up --build` vs individual services
- **How tests are organised**: unit / integration / functional on backend; spec files on frontend

---

## Phase 4 — Answer their questions

Address any specific questions from Phase 1. Use the **Explore** sub-agent to read relevant files before answering.

---

## Phase 5 — Cheat sheet

Produce a personalised quick-reference card:

```markdown
## Grants Applicant Portal — Cheat Sheet

### Run the stack
docker-compose up --build   # full stack
npm start                   # frontend only (http://localhost:4200)
dotnet run --project ...    # backend only (https://localhost:7000)

### Common tasks
/implement-ticket <paste ticket>   # full ticket → code pipeline
/fix-bug <describe the bug>        # diagnose and fix
/review-pr                         # review current branch

### Add something new
/new-endpoint <Domain> <Action>         # backend endpoint
/new-use-case <Domain> <Action> command # CQRS handler
/new-feature <name>                     # Angular page
/new-service <name>                     # Angular core service
/api-call <endpoint description>        # typed frontend HTTP method

### Tests
dotnet test                                           # all backend suites
dotnet test tests/...UnitTests                        # unit only
npm test -- --no-progress --watch=false               # frontend

### Migrations
dotnet ef migrations add <Name> --project src/API.Migrations --startup-project src/API.Web
```

## Rules

- Do not invent information — read the actual files before explaining them
- Match explanation depth to the developer's background
- The cheat sheet must reference real commands that actually work in this repo
