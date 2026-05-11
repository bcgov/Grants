---
description: Add a typed API method to the Grants Applicant Portal frontend for a new backend endpoint — creates the response interface if needed and adds the method with a test.
tools: [codebase, editFiles]
---

Add a typed API method to the Grants Applicant Portal frontend for a new backend endpoint.

Ask the user to describe the endpoint (HTTP verb, path, and what it returns) if not already provided.

## Before writing anything

Read `src/Grants.ApplicantPortal.Frontend/src/app/api.service.ts` in full to understand the existing method signatures, HTTP verb usage, base URL handling, error propagation pattern, and return types.

## Determine the right service

- Endpoint is general-purpose / shared → add to `api.service.ts`
- Endpoint belongs to a specific domain (applicant, workspace, payments) → add to the matching `core/services/<domain>.service.ts` which calls `ApiService` internally
- If unsure, ask the user before proceeding

## Check or create the response interface

Look in `src/app/shared/models/` for an existing interface matching the response shape. If none exists, create `src/app/shared/models/<resource>.interface.ts` with a typed interface — no `any`.

## Add the method

Follow the existing pattern in the target service:
- Return type: `Observable<T>` where `T` is the typed interface
- Correct HTTP verb (GET, POST, PUT, PATCH, DELETE)
- Typed request body parameter for POST/PUT/PATCH
- Error handling matching how other methods in the same file handle errors

## Add a spec

In the service's `*.spec.ts`:
- Mock the HTTP call with `HttpClientTestingModule` or mock `ApiService`
- Assert correct URL, verb, and that the typed response passes through

## Rules
- No `any` types on request bodies or responses — always define an interface
- Never call `HttpClient` directly from a feature component
- All methods return `Observable<T>`, never `Promise<T>`
- Do not subscribe inside the service method

## Report
Show the exact method signature added, the file it was added to, and any new interface created.
