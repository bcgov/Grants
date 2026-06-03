---
name: new-feature
description: Scaffold a complete new feature page for the Grants Applicant Portal — creates component files, registers a lazy route, and optionally creates a core service.
---

Scaffold a complete new feature for the Grants Applicant Portal frontend.

Feature name (kebab-case): $ARGUMENTS

If `$ARGUMENTS` is empty, ask the user for the feature name before doing anything else.

## Steps

1. **Read existing features for patterns** — read one existing feature component (e.g. `src/app/features/workspace/workspace-selector.component.ts`) so you match the exact coding style, decorator usage, and import patterns already in this project.

2. **Create the feature folder and files** under `src/app/features/<name>/`:
   - `<name>.component.ts` — standalone Angular component with `selector: 'app-<name>'`. Do not import `CommonModule` or `RouterModule` — use Angular 20's built-in control flow (`@if`, `@for`, `@switch`) in templates and import individual directives (`RouterLink`, `RouterOutlet`) only if the template needs them.
   - `<name>.component.html` — minimal template with a wrapping `<div>` and a heading
   - `<name>.component.scss` — empty placeholder
   - `<name>.component.spec.ts` — basic `TestBed` spec that checks the component creates successfully

3. **Register the route** in `src/app/app.routes.ts` — add a lazy-loaded route using `loadComponent` pointing at the new component. Choose a sensible URL path from the feature name. Place it in the correct position relative to existing routes (authenticated routes go after the auth guard is applied).

4. **Check if a data service is needed** — if the feature name implies data fetching (e.g. involves applicants, workspaces, payments), ask the user whether to also create a `core/services/<name>.service.ts`. If yes, create it following the pattern in step 1 (read an existing service first).

5. **Report what was created** — list every file created/modified with its path, and show the route entry that was added.

## Rules
- All components must be standalone (`standalone: true`)
- The route must use `loadComponent` (lazy), never eager import in routes
- Do not create a feature module file (`*.module.ts`) — this project does not use NgModules for features
- Do not import `CommonModule` or `RouterModule` — use Angular 20 control flow syntax and individual directive imports
- scss files start empty — do not add placeholder comments
