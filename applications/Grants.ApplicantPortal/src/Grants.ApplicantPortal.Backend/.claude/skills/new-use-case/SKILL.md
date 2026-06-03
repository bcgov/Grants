---
name: new-use-case
description: Scaffold a CQRS use case for the Grants Applicant Portal backend — creates a Command or Query record and its Handler, following the MediatR + Ardalis.Result pattern.
---

Scaffold a CQRS use case (Command/Query + Handler) for the Grants Applicant Portal backend.

Arguments (space-separated): `<Domain> <Action> <command|query>`
Examples: `Addresses Create command`, `Contacts Retrieve query`, `Submissions Delete command`

If `$ARGUMENTS` is empty, ask the user for the domain, action, and whether it is a command (mutating) or query (read-only) before doing anything else.

## Steps

1. **Read an existing use case for patterns** — read a comparable handler in `src/Grants.ApplicantPortal.API.UseCases/<Domain>/<Action>/`. For example, read `Addresses/Create/CreateAddressCommand.cs` and `CreateAddressHandler.cs` to understand record structure, interface usage, dependency injection, and result mapping before writing anything.

2. **Determine the folder** — files go in `src/Grants.ApplicantPortal.API.UseCases/<Domain>/<Action>/`. Create the folder if it does not exist.

3. **Create two files**:

   **`<Action><Domain>Command.cs`** (or `...Query.cs` for queries):
   - A `record` in namespace `Grants.ApplicantPortal.API.UseCases.<Domain>.<Action>`
   - Implements `ICommand<Result<T>>` for commands or `IQuery<Result<T>>` for queries
   - Constructor parameters represent everything the handler needs (ids, values, subject claim for auth checks)
   - Add a XML doc summary describing what the operation does

   **`<Action><Domain>Handler.cs`**:
   - A `class` in the same namespace
   - Implements `ICommandHandler<<Action><Domain>Command, Result<T>>` (or `IQueryHandler` for queries)
   - Constructor-inject only what is needed (repositories, cache services, `ILogger<>`)
   - Return `Result.Success(value)` on success
   - Return `Result.NotFound()`, `Result.Forbidden()`, or `Result.Invalid(errors)` for domain failures
   - Never throw exceptions for expected domain failures — use `Result` return values

4. **Check if a result DTO is needed** — if the handler returns a typed result (not `Result` or `Result<bool>`), check `UseCases/<Domain>/` for an existing mutation result or response DTO. If none fits, create `<Domain>MutationResult.cs` (for commands) or a suitable DTO record.

5. **Report** — list every file created with its full path and show the interfaces implemented.

## Rules

- Commands mutate state; queries only read — never mutate state inside a query handler
- Use `record` for Commands and Queries (immutable, value-semantic)
- All results use Ardalis.Result — never return raw values or throw for domain errors
- Dependency injection via constructor only
- Unit test the handler in `tests/API.UnitTests/` by mocking the injected interfaces
