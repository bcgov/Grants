# Grants Applicant Portal — UI Style Guide

Authoritative reference for all frontend UI decisions. Derived from the existing codebase.
Claude Code checks new plans against this guide before implementing (Phase 3 compliance gate).

---

## BC Gov Bootstrap v5

Use the `@bcgov/bootstrap-v5-theme` classes. Do not invent custom CSS for anything the theme already provides.

### Buttons

| Use case | Class |
| --- | --- |
| Primary action | `btn btn-primary` |
| Secondary / cancel | `btn btn-outline-secondary` |
| Destructive | `btn btn-outline-primary` (confirm in a dialog first) |
| Link style | `btn btn-link` |
| Small variant | add `btn-sm` |

**Never**: bare `<button>` without `btn`, custom background colours on buttons, Bootstrap `btn-danger` (use outline + confirmation dialog instead).

### Layout

- Use `container-fluid` + `row` + `col-*` for all page layouts
- Spacing via Bootstrap utilities (`mb-3`, `g-0`, `p-0`) — not custom margins/padding in SCSS
- `d-flex`, `align-items-center`, `justify-content-center` for flex alignment

### Typography

- Headings: standard HTML `<h1>`–`<h4>` — the BC Gov theme styles these automatically
- Muted text: `text-muted`
- Danger/validation: `text-danger`
- Success: `text-success`

### Cards

```html
<div class="card">
  <div class="card-header">Title</div>
  <div class="card-body">Content</div>
</div>
```

### Tables

Always use the shared `<app-datatable>` component for data grids — do not build raw `<table>` markup in feature components.

### Forms

```html
<label class="form-label" for="fieldId">Label</label>
<input class="form-control" id="fieldId" />
<select class="form-select"></select>
```

- Use `[(ngModel)]` for simple single-field bindings
- Use Angular Reactive Forms (`FormGroup` / `FormControl`) for multi-field forms with validation
- Never use inline `style=""` for form layout — use Bootstrap grid

### Alerts and Toasts

Use the shared `<app-toast>` component for all user-facing notifications — do not create raw `<div class="alert">` in feature templates.

For inline status banners, use `alert-block` class pattern consistent with existing components.

---

## Icons

Use **Font Awesome 6** only. Always add `aria-hidden="true"` to decorative icons.

```html
<i class="fas fa-check" aria-hidden="true"></i>
```

Common icons in use: `fa-chevron-right`, `fa-chevron-left`, `fa-spinner fa-spin`, `fa-bell`, `fa-times`, `fa-search`, `fa-check`, `fa-external-link-alt`.

**Never**: custom SVG icons in feature components (SVGs belong in `public/` or `shared/`), emoji as icons.

---

## Angular Templates

### Control flow

New code **must** use Angular 20 built-in control flow:

```html
@if (condition) { ... }
@else { ... }
@for (item of items; track item.id) { ... }
@switch (value) { @case ('x') { ... } }
```

**Never** use `*ngIf`, `*ngFor`, `*ngSwitch` in new components. Legacy components may still have these — do not introduce them in new code even if surrounding code uses them.

### Imports

Import only what the template uses. Never import `CommonModule` or `RouterModule` in a standalone component.

```typescript
// Correct
imports: [RouterLink, AsyncPipe]

// Wrong
imports: [CommonModule, RouterModule]
```

### Routing

Use `[routerLink]` for internal navigation. For external links always add `target="_blank" rel="noopener noreferrer"`.

---

## Loading, Error, and Empty States

**Loading**: use `<app-loading-overlay>` — do not build custom spinners in feature templates.

**Empty data**: use the `.no-data-state` pattern:
```html
@if (items.length === 0) {
  <div class="no-data-state">No records found.</div>
}
```

**Errors**: surface via `<app-toast>` using the `ToastService` — do not use `alert()` or `console.error()` in production code.

---

## Colours and CSS Variables

Only use BC Gov design tokens — never hardcode hex/RGB values in SCSS:

| Token | Use |
| --- | --- |
| `var(--bc-primary)` | Primary brand colour |
| `var(--bc-blue)` | Interactive blue |
| `var(--bc-gold)` | Accent / highlight |
| `var(--bc-gray-*)` | Neutral greys |

**Never**: `color: #1a1a1a`, `background: blue`, or any hardcoded colour value.

---

## Accessibility (non-negotiable)

- All interactive non-`<button>` elements: `role="button"` + `tabindex="0"` + `(keydown.enter)` + `(keydown.space)` handlers
- All icon-only buttons: `[attr.aria-label]="'Descriptive label'"`
- Decorative icons: `aria-hidden="true"`
- Active navigation items: `[attr.aria-current]="'page'"`
- Expandable elements: `[attr.aria-expanded]="isOpen"`
- Screen-reader text: `<span class="sr-only">text</span>` — not invisible divs

---

## What requires a deviation confirmation

The compliance gate will pause and ask for explicit confirmation if the plan includes:

- Hardcoded hex/RGB colour values in SCSS or inline styles
- Custom spinner or loading component instead of `<app-loading-overlay>`
- Custom alert/notification HTML instead of `<app-toast>`
- Raw `<table>` markup in a feature component instead of `<app-datatable>`
- `*ngIf` / `*ngFor` in new component templates
- `CommonModule` or `RouterModule` imported in a new standalone component
- `any` type on a model or HTTP response
- No `aria-*` attributes on interactive non-button elements
- `console.log` / `alert()` in component or service code
