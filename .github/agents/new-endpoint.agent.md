---
description: Scaffold a complete FastEndpoints endpoint for the Grants Applicant Portal backend — creates the endpoint class, request, response, and validator files.
tools: [search/codebase, edit/editFiles, execute/runInTerminal]
---

Scaffold a complete FastEndpoints endpoint for the Grants Applicant Portal backend.

Ask the user for: **Domain** (e.g. `Addresses`) and **Action** (e.g. `Create`, `Update`, `Delete`, `Retrieve`) if not already provided.

## Before writing anything

Read an existing comparable endpoint in `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/src/Grants.ApplicantPortal.API.Web/<Domain>/` to understand the exact constructor injection, `Configure()` structure, result-mapping pattern, and namespace conventions used in this project.

## Create four files in `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/src/Grants.ApplicantPortal.API.Web/<Domain>/`

### `<Action>.cs` — endpoint class
- Namespace: `Grants.ApplicantPortal.API.Web.<Domain>`
- Class `<Action>` inherits `Endpoint<<Action>Request, <Action>Response>`
- Constructor injects `IMediator _mediator`
- `Configure()`: sets HTTP verb + route from `<Action>Request.Route`, applies `Policies(AuthPolicies.RequireAuthenticatedUser)`, adds `Summary(...)` with response codes 200/201, 400, 401, 403, 404, 422
- `HandleAsync()`: builds a Command/Query from the request, calls `_mediator.Send()`, maps `Result<T>` to HTTP responses using the standard pattern:
  - `IsSuccess` → return 200/201 with `result.Value`
  - `ResultStatus.NotFound` → `SendNotFoundAsync()`
  - `ResultStatus.Forbidden` → `SendForbiddenAsync()`
  - `ResultStatus.Invalid` → `AddError()` + `SendErrorsAsync(422)`
  - `result.Errors.Any()` → `AddError()` + `SendErrorsAsync(400)`

### `<Action>.Request.cs` — request record
- `public const string Route = "/api/v1/<plural-domain>[/{id}]";`
- Properties as `required` strings/Guids or nullable where optional

### `<Action>.Response.cs` — response record
- Properties matching what the use case handler returns

### `<Action>.Validator.cs` — FluentValidation validator
- Inherits `AbstractValidator<<Action>Request>`
- `RuleFor` with `NotEmpty()` for required fields, `MaximumLength()` where appropriate

## Rules
- Never put business logic in the endpoint — dispatch to MediatR only
- All endpoints require `Policies(AuthPolicies.RequireAuthenticatedUser)` in `Configure()`
- Route convention: `/api/v1/<plural-resource>` for collections, `/api/v1/<plural-resource>/{id}` for single resources
- Do NOT create MVC controllers

## After creating files
Ask if the user also wants to scaffold the matching use case (Command/Query + Handler) in `API.UseCases/<Domain>/<Action>/`.
