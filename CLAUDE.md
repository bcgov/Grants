# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Grants — Monorepo

BC Government grants management platform.

## Structure

Two apps under `applications/`: `Grants.ApplicantPortal` (full-stack portal, frontend+backend) and `Grants.AutoUI` (Cypress E2E). Use `ls`/`Glob` to check current layout before assuming — don't rely on a hardcoded tree here.

## Quick Start

```powershell
# Full stack — run from applications/Grants.ApplicantPortal/
cd applications/Grants.ApplicantPortal
docker-compose up --build
# or use the PowerShell helper: .\dev-env.ps1 start

# Frontend:          http://localhost:4200
# Backend API:       http://localhost:5100
# Redis Commander:   http://localhost:8081
# PostgreSQL:        localhost:5434 (user: postgres / password: localdev / db: GrantsDB)
```

## Frontend

`cd applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend` — [Frontend CLAUDE.md](applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/.claude/CLAUDE.md) loads automatically when working here; treat it as ground truth for commands, skills, and architecture over anything summarized in this file.

## Backend

`cd applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend` — [Backend CLAUDE.md](applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/.claude/CLAUDE.md) loads automatically when working here; treat it as ground truth for commands, skills, and architecture over anything summarized in this file.

## Orchestrated Workflows (start here for most tasks)

| Skill | Trigger | What it does |
| --- | --- | --- |
| `/implement-ticket` | Paste ticket details | Analyze → architect → develop (parallel frontend+backend) → test → review → AutoUI guard → document → PR summary |
| `/fix-bug` | Paste bug report / stack trace | Locate root cause → targeted fix → verify tests → security check → AutoUI guard → document → summary |
| `/review-pr` | Current branch or PR# | Security + architecture + test coverage in parallel → structured verdict |
| `/refactor` | Target path + goal | Understand → plan → implement → verify nothing broke → AutoUI guard → document → summary |
| `/onboard` | Optional: `frontend` / `backend` | Codebase tour + patterns explanation + personalised cheat sheet |

## Sub-agents

Specializations are defined in `.claude/agents/*.md` — read the frontmatter there for the current list, don't assume it's static.

## Git Workflow

Branch flow: `dev` → `test` → `main`

| Branch type | Pattern | Base branch |
| --- | --- | --- |
| New feature / enhancement | `feature/AB#<ticket>` | `dev` |
| Bug fix | `bugfix/AB#<ticket>` | `dev` |
| Hotfix (production / test issue) | `hotfix/AB#<ticket>` | `test` or `main` |

**Commit message format**: `AB#<ticket> <short description>` — e.g. `AB#12345 add address validation`

All orchestrated skills (`/implement-ticket`, `/fix-bug`) will ask for the ticket number if it is not supplied up front.

## AutoUI

Cypress E2E suite: `applications/Grants.AutoUI/`. Targets deployed environments — tests do **not** run against localhost. [AutoUI CLAUDE.md](applications/Grants.AutoUI/CLAUDE.md) loads automatically when working here; treat it as ground truth for commands over anything summarized in this file.

Run `/sync-selectors` explicitly after changing any `data-cy` attribute in Angular templates — it detects drift and heals `cypress/selectors/registry.ts` without touching spec logic.

---

## Key Conventions

- **Backend**: FastEndpoints (not controllers), CQRS via MediatR, Ardalis.Result for all return types
- **Frontend**: Standalone Angular components, `loadComponent` lazy routes, no NgModules for features
- **Auth**: All backend endpoints require `RequireAuthenticatedUser` policy; frontend uses `auth.guard.ts`
- **Tests**: Unit tests live next to source; functional/integration tests are separate projects
