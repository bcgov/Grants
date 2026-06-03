---
description: Security-focused review of Grants Applicant Portal code changes — checks for injection vulnerabilities, authentication/authorisation gaps, secrets exposure, and OWASP Top 10 issues.
tools: [search/codebase]
---

Perform a security-focused review of the Grants Applicant Portal code changes.

If no specific files or diff are provided, review all files changed on the current branch compared to `main`.

## Security checklist

### Authentication & Authorisation
- [ ] All new FastEndpoints have `Policies(AuthPolicies.RequireAuthenticatedUser)` in `Configure()` — no endpoint is accidentally unauthenticated
- [ ] Resource ownership is validated in handlers (e.g. the requesting user's profile ID matches the resource's owner) — not just "is logged in"
- [ ] JWT claims are read only via `HttpContext.GetRequiredProfile()` — never manually decoded or trusted from request body
- [ ] Angular route guards (`auth.guard.ts`) are applied to all new authenticated pages

### Injection
- [ ] No raw SQL strings — all DB queries use EF Core parameterised methods or `FromSqlRaw` with parameters
- [ ] No `Process.Start()` or shell execution with user-controlled input
- [ ] Angular templates do not use `[innerHTML]` with unsanitised user content

### Secrets & Config
- [ ] No API keys, passwords, connection strings, or tokens committed in source files
- [ ] No `appsettings.json` entries that belong in environment variables / OpenShift secrets
- [ ] No hardcoded BC Gov environment URLs in application code

### Input Validation
- [ ] All backend request types have a corresponding FluentValidation `AbstractValidator`
- [ ] String length limits applied to all free-text fields (`MaximumLength`)
- [ ] File upload endpoints (if any) validate MIME type and size

### Dependency & Infrastructure
- [ ] No new `NuGet` or `npm` packages added without a clear necessity
- [ ] Docker base images are pinned to a specific digest or minor version — not `:latest`
- [ ] CORS configuration does not add wildcard (`*`) origins

### Data Exposure
- [ ] API responses do not include internal IDs, stack traces, or sensitive fields beyond what the frontend needs
- [ ] No PII logged at `Information` level or below — Keycloak subject/email should not appear in plain logs

## Output format

1. **Critical** — must be fixed before merge (auth gaps, injection, secrets)
2. **High** — strong recommendation to fix
3. **Informational** — notes for awareness, no action required

For each finding: file path, line reference, description, and a concrete remediation suggestion.
