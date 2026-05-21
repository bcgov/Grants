# Grants — Monorepo

BC Government grants management platform.

## Structure

```text
applications/
├── Grants.ApplicantPortal/            # Full-stack grant applicant portal
│   ├── src/
│   │   ├── Grants.ApplicantPortal.Frontend/   # Angular 20 SPA
│   │   │   └── .claude/                       # Claude Code context for frontend
│   │   └── Grants.ApplicantPortal.Backend/    # .NET 9 Web API
│   │       └── .claude/                       # Claude Code context for backend
│   └── docker-compose.yml                     # Full stack via Docker
└── Grants.AutoUI/                     # Cypress E2E test suite
documentation/                         # Architecture decisions and guides
```

## Stack

| Layer | Technology |
| --- | --- |
| Frontend | Angular 20, TypeScript, standalone components, Keycloak OIDC, BC Gov Bootstrap v5 |
| Backend | .NET 9, FastEndpoints, MediatR (CQRS), Ardalis.Result, FluentValidation |
| Database | PostgreSQL 17, Entity Framework Core migrations |
| Cache | Redis 7 |
| Auth | Keycloak (OIDC/OAuth2) |
| Quality | SonarCloud (bcgov org) |
| CI | GitHub Actions |
| Deployment | OpenShift (BC Government) |

## Quick Start

```powershell
# Full stack (run from applications/Grants.ApplicantPortal/)
cd applications/Grants.ApplicantPortal
docker-compose up --build

# Frontend: http://localhost:4000
# Backend API: http://localhost:5100
# Redis Commander: http://localhost:8081
```

## Frontend

```bash
cd applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend
npm start          # dev server at http://localhost:4200
npm test           # Karma/Jasmine unit tests
npm run build      # production build
```

Claude Code skills: `/new-feature`, `/new-shared-component`, `/new-service`, `/api-call`, `/env-check`

Full context: [Frontend CLAUDE.md](applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/.claude/CLAUDE.md)

## Backend

```bash
cd applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend
dotnet run --project src/Grants.ApplicantPortal.API.Web          # https://localhost:7000
dotnet test                                                       # all test suites
dotnet test tests/Grants.ApplicantPortal.API.UnitTests            # unit tests only
```

Claude Code skills: `/new-endpoint`, `/new-use-case`, `/new-migration`, `/run-tests`

Full context: [Backend CLAUDE.md](applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/.claude/CLAUDE.md)

## Orchestrated Workflows (start here for most tasks)

| Skill | Trigger | What it does |
| --- | --- | --- |
| `/implement-ticket` | Paste ticket details | Analyze → architect → develop (parallel frontend+backend) → test → review → AutoUI guard → document → PR summary |
| `/fix-bug` | Paste bug report / stack trace | Locate root cause → targeted fix → verify tests → security check → AutoUI guard → document → summary |
| `/review-pr` | Current branch or PR# | Security + architecture + test coverage in parallel → structured verdict |
| `/refactor` | Target path + goal | Understand → plan → implement → verify nothing broke → AutoUI guard → document → summary |
| `/onboard` | Optional: `frontend` / `backend` | Codebase tour + patterns explanation + personalised cheat sheet |

## Sub-agents (used automatically by orchestrators)

| Agent | Specialisation |
| --- | --- |
| `backend-developer` | .NET 9, FastEndpoints, CQRS, EF Core |
| `frontend-developer` | Angular 20, standalone components, Keycloak |
| `test-guardian` | Runs suites, diagnoses failures, writes missing tests |
| `security-reviewer` | Auth gaps, OWASP, secrets, injection (read-only) |
| `code-reviewer` | Architecture rules, naming conventions (read-only) |
| `autoui-guardian` | Cypress E2E self-healing: fixes broken specs and stubs new ones for new features |
| `auto-documenter` | Keeps `documentation/auto/` and architecture docs in sync after code changes |

## Git Workflow

Branch flow: `dev` → `test` → `main`

| Branch type | Pattern | Base branch |
| --- | --- | --- |
| New feature / enhancement | `feature/AB#<ticket>` | `dev` |
| Bug fix | `bugfix/AB#<ticket>` | `dev` |
| Hotfix (production / test issue) | `hotfix/AB#<ticket>` | `test` or `main` |

**Commit message format**: `AB#<ticket> <short description>` — e.g. `AB#12345 add address validation`

All orchestrated skills (`/implement-ticket`, `/fix-bug`) will ask for the ticket number if it is not supplied up front.

## Key Conventions

- **Backend**: FastEndpoints (not controllers), CQRS via MediatR, Ardalis.Result for all return types
- **Frontend**: Standalone Angular components, `loadComponent` lazy routes, no NgModules for features
- **Auth**: All backend endpoints require `RequireAuthenticatedUser` policy; frontend uses `auth.guard.ts`
- **Tests**: Unit tests live next to source; functional/integration tests are separate projects
