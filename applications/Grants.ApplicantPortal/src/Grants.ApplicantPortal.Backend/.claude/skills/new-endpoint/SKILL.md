---
name: new-endpoint
description: Scaffold a complete FastEndpoints endpoint for the Grants Applicant Portal backend — creates the endpoint class, request, response, and validator files, then registers it within the domain folder.
---

Scaffold a complete FastEndpoints endpoint for the Grants Applicant Portal backend.

Arguments (space-separated): `<Domain> <Action>`
Examples: `Addresses Create`, `Contacts Update`, `Payments Retrieve`

If `$ARGUMENTS` is empty, ask the user for the domain and action before doing anything else.

## Steps

1. **Read an existing endpoint for patterns** — read a comparable endpoint in `src/Grants.ApplicantPortal.API.Web/<Domain>/`. For example, read `Addresses/Create.cs` and `Addresses/Create.Request.cs` to understand constructor injection, `Configure()` structure, result mapping, and naming conventions before writing anything.

2. **Determine the domain folder** — the endpoint files go in `src/Grants.ApplicantPortal.API.Web/<Domain>/`. Create the folder if it does not exist.

3. **Create four files** following the existing pattern:

   **`<Action>.cs`** — endpoint class:
   - Class name: `<Action>` in namespace `Grants.ApplicantPortal.API.Web.<Domain>`
   - Constructor-inject `IMediator _mediator`
   - Inherits `Endpoint<<Action>Request, <Action>Response>`
   - `Configure()`: sets HTTP verb + route from `<Action>Request.Route`, applies `Policies(AuthPolicies.RequireAuthenticatedUser)`, adds `Summary(...)` with response codes 200/201, 400, 401, 403, 404, 422
   - `HandleAsync()`: calls `_mediator.Send()` with a new Command/Query, then maps `Result<T>` to HTTP responses using the standard result-mapping pattern (IsSuccess → return value; NotFound → SendNotFoundAsync; Forbidden → SendForbiddenAsync; Invalid → AddError + SendErrorsAsync(422); Errors → SendErrorsAsync(400))

   **`<Action>.Request.cs`** — request record:
   - `public const string Route = "/api/v1/<domain-plural>/<path>";`
   - Properties as `required` or nullable strings/Guids matching the use case

   **`<Action>.Response.cs`** — response record:
   - Properties matching what the use case handler returns

   **`<Action>.Validator.cs`** — FluentValidation validator:
   - Inherits `AbstractValidator<<Action>Request>`
   - Add sensible `RuleFor` rules: NotEmpty for required fields, MaximumLength where applicable

4. **Ask whether to also scaffold the use case** — if no matching `UseCases/<Domain>/<Action>/` folder exists, ask if the user also wants to run `/new-use-case` for it.

5. **Report** — list every file created with its full path and show the route that was registered.

## Rules

- All endpoints must apply `Policies(AuthPolicies.RequireAuthenticatedUser)` in `Configure()`
- Constructor injection only — no service locator
- Never put business logic in the endpoint; dispatch to a handler via `_mediator.Send()`
- Follow the result-mapping pattern exactly — do not introduce custom HTTP status codes
- Route convention: `/api/v1/<plural-domain>` for collections, `/api/v1/<plural-domain>/{id}` for single resources
