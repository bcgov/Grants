# Client-Side Submission PDF Generation

> Introduced by: AB#34070 — clickable "Submission Type" column in the Submissions grid that renders a submission's form.io content as a downloadable/viewable PDF.

## Overview

Applicants need to view or download a PDF copy of a submission from the Submissions grid — desktop gets a clickable "Submission Type" link that opens the PDF in a new tab, mobile gets a download icon. Rather than standing up server-side PDF rendering (a second form.io/headless-browser runtime to keep in sync with the frontend's), the Portal renders the submission entirely in the applicant's own browser: fetch the submission's form.io `schema` + `data` from `GET /Submissions/{PluginId}/{Provider}/{SubmissionId:Guid}/Form` (see [API Endpoints](../auto/API-Endpoints.md)), render it off-screen with the same `formiojs` library already used elsewhere in the app, rasterize the rendered DOM with `html2canvas`, and paginate that into a PDF with `jsPDF` — entirely client-side, with no server-side PDF generation involved. No submission ever passes through a server-side rendering step or is written to disk — the PDF exists only as an in-memory blob in the applicant's browser.

## Architecture

- `SubmissionPdfService` (`core/services/submission-pdf.service.ts`) owns the whole pipeline:
  1. `fetchSubmissionForm()` — calls the backend endpoint, unwraps the `{ data: { schema, data } }` envelope
  2. `generatePdf()` — patches the schema (see "Known quirks" below), creates an off-screen `<div>`, renders it via `Formio.createForm(container, schema, { readOnly: true, flatten: true })`, applies the submission via `form.setSubmission({ data })`, injects scoped CSS to hide read-only chrome and align styling with UNITY's own print output, waits for formio's redraw plus a settle delay, then rasterizes with `html2canvas`
  3. `canvasToPdfBlob()` — paginates the rasterized canvas into an A4 `jsPDF` document
  4. `viewSubmissionPdf()` / `downloadSubmissionPdf()` — open the resulting blob in a new tab (desktop) or trigger a download (mobile)
- Entry point is the Submissions grid: the "Submission Type" column is now an `action-link` datatable column (see `DatatableColumn.type` in `shared/components/datatable/datatable.models.ts`) that emits `(cellAction)`, wired up in `submissions.component.ts`.
- New ambient type shim `shared/types/html2canvas.d.ts` — the published `@types/html2canvas` package targets an incompatible older API, so this is a hand-written replacement.

### Known quirks (and why the fixes exist)

Rendering a real form.io/Bootstrap DOM through `html2canvas` surfaces several library-level quirks that needed explicit workarounds — these are non-obvious, so they're documented here rather than left as silent code:

- **Leftover empty box under select fields** — form.io renders a plain `<input class="formio-select-autocomplete-input">` as a sibling after every select widget (for browser autofill/accessibility), entirely separate from the Choices.js widget itself. `injectPdfStyles()` and `removeFormioRenderArtifacts()` both hide/remove it, along with Choices.js's own closed dropdown list and search input.
- **Duplicate headings** — form.io's `htmlelement` component (CHEFS's authored heading/content blocks) wraps its HTML in a `<p>` tag by default. If the authored content itself contains a block-level element (common for headings), the browser's HTML parser auto-closes the `<p>` early and re-parses the remainder as a sibling node, rendering the same heading twice. `generatePdf()` walks the schema before rendering and forces every `htmlelement` component's `tag` to `div` (which can legally contain block children), mirroring the same fix UNITY applies in its own print pipeline.
- **Popup blocking on `viewSubmissionPdf`** — the PDF isn't ready until after an `await`, but by then the call is no longer synchronously inside the click handler, so a browser can block `window.open()` as an unsolicited popup. Fixed by opening a blank tab synchronously before the `await`, then navigating that already-open tab to the blob URL once ready.

## Key files

| File | Purpose |
|---|---|
| `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/src/app/core/services/submission-pdf.service.ts` | Fetch + render + rasterize + PDF pipeline |
| `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/src/app/core/services/submission-pdf.service.spec.ts` | Unit tests (module-boundary mocks — real DOM/canvas rendering is not meaningfully unit-testable) |
| `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/src/app/core/models/submission-form.model.ts` | `SubmissionFormResponse` shape (`{ schema, data }`) |
| `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/src/app/shared/types/html2canvas.d.ts` | Hand-written ambient type shim for `html2canvas` |
| `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/src/app/shared/components/datatable/datatable.models.ts` | New `'action-link'` column type + `DatatableActionLinkConfig` |
| `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/src/app/features/applicant-info/submissions/submissions.component.ts` | Wires the grid's "Submission Type" column to `SubmissionPdfService` |
| `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/src/Grants.ApplicantPortal.API.Web/Submissions/RetrieveSubmissionForm.cs` | Backend endpoint supplying the `schema`/`data` payload |

## Usage

Call the service from any component that has a submission's `pluginId`/`provider`/`submissionId` (typically off a Submissions grid row):

```typescript
// Desktop: open in a new tab
await this.submissionPdfService.viewSubmissionPdf(pluginId, provider, submissionId);

// Mobile: trigger a download
await this.submissionPdfService.downloadSubmissionPdf(pluginId, provider, submissionId);
```

## Related docs

- [API Endpoints](../auto/API-Endpoints.md) — `GET /Submissions/{PluginId}/{Provider}/{SubmissionId:Guid}/Form`
- [Plugin Architecture](Plugin-Architecture.md) — source of the `schema`/`data` payload (DEMO fixture vs. UNITY live call)
- [Resource Ownership Validation](Resource-Ownership-Validation.md) — ownership check gating access to a submission's form data
