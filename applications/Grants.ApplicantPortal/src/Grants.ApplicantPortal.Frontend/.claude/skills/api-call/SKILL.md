---
name: api-call
description: Add a typed API method to the Grants Applicant Portal for a new backend endpoint — reads api.service.ts patterns, creates the response interface if needed, adds the method and a spec.
---

Add a typed API method to the Grants Applicant Portal frontend for a new backend endpoint.

Endpoint description: $ARGUMENTS

If `$ARGUMENTS` is empty, ask the user to describe the endpoint (HTTP verb, path, and what it returns) before doing anything else.

## Steps

1. **Read `src/app/api.service.ts`** in full — understand the existing method signatures, HTTP verbs used, base URL handling, error propagation pattern, and return types before writing anything.

2. **Determine the right service to add the method to**:
   - If the endpoint is general-purpose / shared across features → add to `api.service.ts`
   - If it belongs to a specific domain (applicant, workspace, payments) → add to the matching `core/services/<domain>.service.ts` which calls `ApiService` internally
   - If unsure, ask the user before proceeding

3. **Check or create the response interface**:
   - Look in `src/app/shared/models/` for an existing interface that matches the response shape
   - If none exists, create `src/app/shared/models/<resource>.interface.ts` with a typed interface — no `any`

4. **Add the method** following the existing pattern in the target service:
   - Return type must be `Observable<T>` where `T` is the typed interface
   - Use the correct HTTP verb from the endpoint description (GET, POST, PUT, PATCH, DELETE)
   - For POST/PUT/PATCH include a typed request body parameter
   - Match error handling to how other methods in the same file handle errors

5. **Add a corresponding spec** in the service's `*.spec.ts`:
   - Mock the HTTP call with `HttpClientTestingModule` or mock `ApiService`
   - Assert correct URL, verb, and that the typed response is passed through

6. **Report** — show the exact method signature added, the file it was added to, and any new interface created.

## Rules
- No `any` types on request bodies or responses — always define an interface
- Never call `HttpClient` directly from a feature component — the method must live in a service
- All methods return `Observable<T>`, never `Promise<T>`
- Use the same base URL / path prefix convention already in `api.service.ts`
- Do not subscribe inside the service method
