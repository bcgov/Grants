---
name: new-shared-component
description: Create a new reusable presentational component in the shared layer of the Grants Applicant Portal — generates .ts, .html, .scss, and .spec.ts files.
---

Create a new reusable component in the shared layer of the Grants Applicant Portal frontend.

Component name (kebab-case): $ARGUMENTS

If `$ARGUMENTS` is empty, ask the user for the component name before doing anything else.

## Steps

1. **Read an existing shared component for patterns** — read `src/app/shared/components/toast/toast.component.ts` and its `.html` to understand the exact decorator style, import conventions, and Input/Output patterns used in this project.

2. **Create the component folder and files** under `src/app/shared/components/<name>/`:
   - `<name>.component.ts` — standalone component, `selector: 'app-<name>'`, `changeDetection: ChangeDetectionStrategy.OnPush`. Declare any obvious `@Input()` properties based on the component name.
   - `<name>.component.html` — minimal semantic template
   - `<name>.component.scss` — empty file
   - `<name>.component.spec.ts` — `TestBed` spec: component creates, and one test per `@Input()` that verifies binding works

3. **Ask about the component's purpose** if the name alone is ambiguous, before writing any logic — shared components must be generic and reusable, not tied to a specific feature's data shape.

4. **Report** — list every file created with its path, and remind the user to import the component in whichever feature component needs it.

## Rules
- `standalone: true` — never add to a module
- `ChangeDetectionStrategy.OnPush` — always, for shared components
- Inputs must use the `input()` signal function (Angular 17+) if the project already uses signals elsewhere; otherwise use `@Input()` decorator — check existing shared components first
- No business logic — shared components are presentational only; data fetching belongs in `core/services`
- scss files start empty
- Do not create a barrel `index.ts` unless one already exists in `src/app/shared/components/`
