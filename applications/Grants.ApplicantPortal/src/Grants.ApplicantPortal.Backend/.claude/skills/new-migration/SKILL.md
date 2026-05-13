---
name: new-migration
description: Create a new Entity Framework Core migration for the Grants Applicant Portal backend — runs dotnet ef migrations add, then summarises the generated Up/Down SQL.
---

Create a new Entity Framework Core migration for the Grants Applicant Portal backend.

Migration name: $ARGUMENTS

If `$ARGUMENTS` is empty, ask the user for the migration name (PascalCase, e.g. `AddAddressCountryColumn`) before doing anything else.

## Steps

1. **Verify working directory** — confirm the shell is in `src/Grants.ApplicantPortal.Backend/` (or that commands can be run from the repo root with the appropriate `--project` flags).

2. **Check for pending model changes** — read `src/Grants.ApplicantPortal.API.Infrastructure/Data/AppDbContext.cs` (and relevant entity configurations) so you can predict what the migration should contain before running the command.

3. **Run the migration command**:
   ```bash
   dotnet ef migrations add $ARGUMENTS \
     --project src/Grants.ApplicantPortal.API.Migrations \
     --startup-project src/Grants.ApplicantPortal.API.Web
   ```

4. **Read the generated migration file** in `src/Grants.ApplicantPortal.API.Migrations/Migrations/<timestamp>_<Name>.cs` and summarise:
   - What `Up()` does (tables created, columns added, indexes, constraints)
   - What `Down()` does (rollback steps)
   - Flag any destructive operations (column drops, table drops, type changes) so the developer can review

5. **Check the snapshot** — confirm `ApplicationDbContextModelSnapshot.cs` was updated alongside the migration.

6. **Optionally apply the migration** — ask the user if they want to run `dotnet ef database update` now. If yes, run it and report success or error output.

## Rules

- Never edit generated migration files to add hand-written SQL — use EF fluent config or data seeds instead
- If the migration is empty (no model changes detected), report that and do not create it
- For destructive changes (column drops, renames), warn the user before applying to a shared or production database
- Migration names must be PascalCase and descriptive: `AddApplicantIdIndexToAddresses`, not `Update3`
