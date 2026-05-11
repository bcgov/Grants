---
description: Scaffold a CQRS use case for the Grants Applicant Portal backend — creates a Command or Query record and its Handler using MediatR and Ardalis.Result.
tools: [codebase, editFiles]
---

Scaffold a CQRS use case (Command or Query + Handler) for the Grants Applicant Portal backend.

Ask the user for:
- **Domain** (e.g. `Addresses`, `Contacts`, `Submissions`)
- **Action** (e.g. `Create`, `Update`, `Delete`, `Retrieve`)
- **Type**: command (mutates state) or query (read-only)

## Before writing anything

Read a comparable handler in `src/Grants.ApplicantPortal.Backend/src/Grants.ApplicantPortal.API.UseCases/<Domain>/<Action>/` to understand record structure, interface usage, dependency injection, and result-mapping conventions used in this project.

## Create two files in `src/Grants.ApplicantPortal.API.UseCases/<Domain>/<Action>/`

### `<Action><Domain>Command.cs` (or `...Query.cs` for queries)
- Namespace: `Grants.ApplicantPortal.API.UseCases.<Domain>.<Action>`
- A `record` with constructor parameters representing everything the handler needs
- Implements `ICommand<Result<T>>` for commands, or `IQuery<Result<T>>` for queries
- Include an XML doc `<summary>` describing what the operation does

### `<Action><Domain>Handler.cs`
- Same namespace
- `class` implementing `ICommandHandler<<Action><Domain>Command, Result<T>>` (or `IQueryHandler` for queries)
- Constructor-inject only required dependencies (repositories, cache services, `ILogger<>`)
- Return `Result.Success(value)` on success
- Return `Result.NotFound()`, `Result.Forbidden()`, or `Result.Invalid(errors)` for domain failures — never throw for expected failures

## Rules
- Commands mutate state; queries only read — never mutate inside a query handler
- Use `record` for Commands/Queries (immutable)
- All results use Ardalis.Result — no raw return values or exceptions for domain errors
- Constructor injection only — no service locator
- Check `UseCases/<Domain>/` for an existing mutation result DTO before creating a new one

## After creating files
Report each file path created and the interfaces implemented. Offer to also run `/new-endpoint` to wire up the HTTP layer.
