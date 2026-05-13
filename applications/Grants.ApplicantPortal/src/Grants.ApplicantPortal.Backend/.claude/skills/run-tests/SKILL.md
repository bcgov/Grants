---
name: run-tests
description: Run backend test suites for the Grants Applicant Portal — unit, integration, functional, or all, with a clear pass/fail summary.
---

Run backend tests for the Grants Applicant Portal.

Suite selector: $ARGUMENTS
Valid values: `unit`, `integration`, `functional`, `all` (default: `all`)

## Steps

1. **Determine which suite(s) to run** based on `$ARGUMENTS`:

   | Argument | Project |
   |---|---|
   | `unit` | `tests/Grants.ApplicantPortal.API.UnitTests` |
   | `integration` | `tests/Grants.ApplicantPortal.API.IntegrationTests` |
   | `functional` | `tests/Grants.ApplicantPortal.API.FunctionalTests` |
   | `all` or empty | run `dotnet test` from the backend root (all projects) |

2. **Run the appropriate command** from `src/Grants.ApplicantPortal.Backend/`:

   ```bash
   # All suites
   dotnet test --logger "console;verbosity=normal"

   # Single suite
   dotnet test tests/Grants.ApplicantPortal.API.UnitTests --logger "console;verbosity=normal"
   ```

3. **Parse the output** and report:
   - Total passed / failed / skipped
   - Any failing test names with their error messages
   - Duration

4. **If tests fail** — read the failing test file(s) and the source they test to diagnose the root cause. Do not just report the failure — explain what went wrong and suggest a fix if it is clear.

## Rules

- Do not skip tests with `[Skip]` to make a run pass
- Integration and functional tests require PostgreSQL and Redis to be running (via `docker-compose up postgres redis`)
- If infrastructure is not available, report that clearly rather than letting tests time out
