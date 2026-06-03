---
name: new-service
description: Create a new injectable service in the core layer of the Grants Applicant Portal — sets up ApiService injection, BehaviorSubject state, typed observables, and a spec file.
---

Create a new Angular service in the core layer of the Grants Applicant Portal frontend.

Service name (kebab-case, without the "-service" suffix): $ARGUMENTS

If `$ARGUMENTS` is empty, ask the user for the service name before doing anything else.

## Steps

1. **Read an existing core service for patterns** — read `src/app/core/services/applicant.service.ts` to match the exact coding style: constructor injection, RxJS operators used, error handling approach, and how the service interacts with `api.service.ts`.

2. **Create `src/app/core/services/<name>.service.ts`**:
   - `@Injectable({ providedIn: 'root' })`
   - Inject `ApiService` (not `HttpClient` directly — all HTTP goes through `api.service.ts`)
   - Only add `BehaviorSubject` + public `Observable` if the service holds shared state that multiple components need to react to — if it is purely a set of HTTP call wrappers, plain methods returning `Observable<T>` are sufficient
   - Methods return `Observable<T>` — never subscribe inside the service itself
   - Include a `private handleError` or delegate to `ErrorHandlerService` for HTTP errors — check how existing services handle errors

3. **Create `src/app/core/services/<name>.service.spec.ts`**:
   - Test that the service is injectable
   - One test per public method: mock `ApiService`, assert the correct endpoint is called and the return value is passed through

4. **Check if a matching interface is needed** — if the service deals with a new data type not already in `src/app/shared/models/`, create `src/app/shared/models/<name>.interface.ts` with the TypeScript interface.

5. **Report** — list every file created, and remind the user to inject the service in whichever component or feature needs it.

## Rules
- `providedIn: 'root'` — never add to a module's `providers` array
- Never call `HttpClient` directly — always go through `ApiService`
- Never `.subscribe()` inside the service — return observables to the caller
- No `any` types — define proper interfaces in `shared/models/`
- If the service holds shared state, expose it as a readonly observable (`asObservable()`) and keep the `BehaviorSubject` private
