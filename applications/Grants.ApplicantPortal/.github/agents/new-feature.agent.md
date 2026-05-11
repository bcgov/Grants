---
description: Scaffold a complete Angular feature page for the Grants Applicant Portal frontend — creates standalone component files and registers a lazy-loaded route.
tools: [codebase, editFiles]
---

Scaffold a complete new Angular feature page for the Grants Applicant Portal frontend.

Ask the user for the **feature name** (kebab-case, e.g. `grant-summary`, `payment-history`) if not already provided.

## Before writing anything

Read one existing feature component — e.g. `src/Grants.ApplicantPortal.Frontend/src/app/features/workspace/workspace-selector.component.ts` — to understand the exact decorator usage, import patterns, and coding style already in this project.

## Create files in `src/Grants.ApplicantPortal.Frontend/src/app/features/<name>/`

- **`<name>.component.ts`** — standalone Angular component:
  - `selector: 'app-<name>'`, `standalone: true`
  - Use Angular 20 built-in control flow (`@if`, `@for`, `@switch`) — not `*ngIf` / `*ngFor`
  - Import individual directives (`RouterLink`, `AsyncPipe`) only if the template uses them
  - Do NOT import `CommonModule` or `RouterModule`

- **`<name>.component.html`** — minimal template with a wrapping `<div>` and a `<h1>` heading

- **`<name>.component.scss`** — empty file (no placeholder comments)

- **`<name>.component.spec.ts`** — basic `TestBed` spec that asserts the component creates successfully

## Register the route

Add a lazy-loaded route in `src/Grants.ApplicantPortal.Frontend/src/app/app.routes.ts` using `loadComponent`. Place it in the correct position relative to existing routes (authenticated routes after the auth guard).

## Check if a data service is needed

If the feature name implies data fetching (applicants, workspaces, payments, submissions), ask the user whether to create a `core/services/<name>.service.ts`. If yes, read an existing service first to match the pattern.

## Rules
- `standalone: true` is required on all components
- Always use `loadComponent` (lazy) — never eager route imports
- Do NOT create `*.module.ts` files — this project does not use NgModules for features
- SCSS files start empty

## Report
List every file created/modified and show the exact route entry added to `app.routes.ts`.
