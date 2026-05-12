---
description: Create a new Entity Framework Core migration for the Grants Applicant Portal backend — runs dotnet ef migrations add and summarises the generated Up/Down operations.
tools: [search/codebase, edit/editFiles, execute/runInTerminal]
---

Create a new Entity Framework Core migration for the Grants Applicant Portal backend.

Ask the user for the **migration name** (PascalCase, descriptive — e.g. `AddApplicantIdIndexToAddresses`, not `Update3`) if not already provided.

## Steps

1. **Read the DbContext and relevant entity configs** in `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/src/Grants.ApplicantPortal.API.Infrastructure/Data/` to understand what model changes are pending before running the command.

2. **Run the migration** from `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/`:
   ```bash
   dotnet ef migrations add <MigrationName> \
     --project src/Grants.ApplicantPortal.API.Migrations \
     --startup-project src/Grants.ApplicantPortal.API.Web
   ```

3. **Read the generated migration file** in `src/Grants.ApplicantPortal.API.Migrations/Migrations/` and summarise:
   - What `Up()` does (tables, columns, indexes, constraints created)
   - What `Down()` does (rollback steps)
   - Flag any **destructive operations** (column drops, type changes, table drops) so the developer reviews before applying

4. **Confirm the snapshot** — verify `ApplicationDbContextModelSnapshot.cs` was updated alongside the migration.

5. **Ask if the developer wants to apply the migration now** with `dotnet ef database update`. Warn if destructive changes are present.

## Rules
- Migration names must be PascalCase and descriptive
- Never hand-edit generated migration files to add raw SQL — use EF fluent config or data seeds
- If the migration generates empty `Up()`/`Down()`, report that no model changes were detected and do not commit it
- Warn loudly before applying destructive migrations to a shared or production database
