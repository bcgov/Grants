## Summary

<!-- What does this PR do? Link the work item: AB#<ticket> -->

## Type of change

- [ ] Bug fix
- [ ] New feature / enhancement
- [ ] Refactor (no functional change)
- [ ] Infrastructure / config
- [ ] Documentation

## Changes

<!-- Bullet list of notable changes. Be specific — file paths and function names are helpful. -->

-
-

## Testing

<!-- How was this tested? Check all that apply. -->

- [ ] Unit tests added / updated and passing (`npm test` / `dotnet test ...UnitTests`)
- [ ] Integration tests passing (`dotnet test ...IntegrationTests`)
- [ ] Manually tested locally (describe scenario below)
- [ ] No tests needed — explain why:

**Manual test notes:**

<!-- Describe the scenario you tested and the expected vs actual result. -->

## Checklist

- [ ] No `any` types introduced (TypeScript)
- [ ] No business logic added directly to Angular components or FastEndpoints `HandleAsync`
- [ ] All new backend endpoints have `Policies(AuthPolicies.RequireAuthenticatedUser)` applied
- [ ] No secrets or environment-specific values committed
- [ ] `CLAUDE.md` / `copilot-instructions.md` updated if architecture changed

## Screenshots (if UI change)

<!-- Before / after screenshots or a short screen recording -->
