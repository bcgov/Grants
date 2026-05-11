---
description: Review code changes in the Grants Applicant Portal for project conventions, architecture rules, security, and test coverage — produces a structured report.
tools: [codebase]
---

Review the code changes in the Grants Applicant Portal for correctness, project conventions, and completeness.

If no specific files or diff are provided, review all files changed on the current branch compared to `main`.

## Review checklist

### Architecture
- [ ] Backend: No business logic inside FastEndpoints `HandleAsync` — logic belongs in the Use Case handler
- [ ] Backend: All new endpoints have `Policies(AuthPolicies.RequireAuthenticatedUser)` in `Configure()`
- [ ] Backend: Handlers return `Result<T>` via Ardalis.Result — no raw exceptions for domain failures
- [ ] Backend: No direct `new AppDbContext()` — always use injected `IApplicationDbContext`
- [ ] Frontend: All new components are `standalone: true` — no `NgModule` created
- [ ] Frontend: Routes use `loadComponent` (lazy) — no eager imports in `app.routes.ts`
- [ ] Frontend: No `CommonModule` or `RouterModule` imports in standalone components
- [ ] Frontend: Business logic in `core/services` — not in feature components

### Code quality
- [ ] No `any` types in TypeScript
- [ ] All Angular service HTTP methods return `Observable<T>`, not `Promise<T>`
- [ ] FluentValidation validators present for all new backend request types
- [ ] No `Console.WriteLine` / `console.log` left in production code

### Security
- [ ] No secrets, credentials, or environment-specific URLs hardcoded
- [ ] User input validated at the API boundary (FluentValidation backend, Reactive Forms frontend)
- [ ] No raw SQL strings — EF Core parameterized queries only
- [ ] No wildcard CORS origins added

### Tests
- [ ] New handlers have unit tests in `tests/API.UnitTests/`
- [ ] New Angular components/services have `*.spec.ts` files

## Output format

Produce a structured report:
1. **Passed** — items that look correct
2. **Issues** — specific violations with file + line reference and a suggested fix
3. **Suggestions** — non-blocking improvements worth considering

Be concise. Flag blockers clearly. Do not repeat what the code already does correctly if there are no issues.
