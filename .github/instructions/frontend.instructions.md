---
applyTo: applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/**
---

# Frontend Instructions — Angular 20

These rules apply to all files in the Angular frontend (`applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/`).

## Architecture layers

```
src/app/
├── core/       # Singletons: auth config, guards, interceptors, domain services
├── features/   # Smart page-level components — lazy-loaded via loadComponent
├── layout/     # Header / shell — presentational only, no data fetching
└── shared/     # Dumb components, directives, TypeScript models, utility services
```

- Business logic → `core/services`
- Page logic → `features`
- Reusable UI → `shared`
- **Never** import `features` from `core` or `shared`

## Components

- All components must be **`standalone: true`** — do not create `NgModule` files for features
- Use **`loadComponent`** for all routes (lazy) — never eager imports in `app.routes.ts`
- Use Angular 20 built-in control flow: **`@if`, `@for`, `@switch`** — not `*ngIf` / `*ngFor` directives
- Import only what the template needs (`RouterLink`, `AsyncPipe`) — never `CommonModule` or `RouterModule`

## TypeScript

- **No `any` types** — define interfaces in `src/app/shared/models/`
- All HTTP service methods return **`Observable<T>`** — never `Promise<T>`
- Never subscribe inside a service method — return the observable to the caller
- Never call `HttpClient` directly from a component — use a service

## HTTP & API

- All backend calls go through `src/app/api.service.ts` or a domain service in `core/services/`
- In dev, the Angular dev server proxies `/api` to the backend
- In production, `server.js` proxies when `ENABLE_API_PROXY=true`

## Auth

- All authenticated pages must be covered by `auth.guard.ts` via the route config
- `auth.interceptor.ts` injects the Keycloak Bearer token automatically — do not add `Authorization` headers manually
- Keycloak config: `src/app/core/auth/auth.config.ts`

## Styling

- Use BC Gov Bootstrap v5 classes where possible (`@bcgov/bootstrap-v5-theme`)
- Component-scoped styles go in `*.component.scss`
- SCSS files for new components start empty — no placeholder comments

## Testing

- Unit test files live next to source: `<name>.component.spec.ts`, `<name>.service.spec.ts`
- Use `TestBed` for Angular component and service tests
- Run: `npm test` from `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/`
