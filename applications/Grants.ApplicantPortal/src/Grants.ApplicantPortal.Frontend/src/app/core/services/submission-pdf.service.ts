import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom, throwError } from 'rxjs';
import { catchError, map, retry } from 'rxjs/operators';
import { Formio } from 'formiojs';
import { jsPDF } from 'jspdf';
import html2canvas from 'html2canvas';
import { SubmissionFormResponse } from '../models/submission-form.model';
import { environment } from '../../../environments/environment';

/**
 * Minimal shape of the form.io form instance we interact with — formiojs ships `Promise<any>` from `createForm`.
 * `submission`/`setSubmission` both expect a full form.io submission object — `{ data: { <componentKey>: value } }`
 * — not a bare field map. Callers must pass the already-`{ data: ... }`-shaped object straight through; wrapping
 * it again (`{ data: submission }`) double-nests it and every component silently receives `undefined`.
 */
interface FormioFormInstance {
  ready: Promise<unknown>;
  submission: { data: Record<string, unknown> };
  /**
   * Promise-returning counterpart to the `submission` setter — resolves once formiojs has
   * actually finished applying the new data and re-rendering. The bare `submission =` setter
   * kicks off the same work internally but discards the promise, so callers have no signal
   * for when the redraw has actually completed in the DOM.
   */
  setSubmission?: (submission: { data: Record<string, unknown> }, flags?: Record<string, unknown>) => Promise<unknown>;
  destroy?: (deleteState?: boolean) => void;
}

/**
 * Raw envelope shape returned by `GET /Submissions/{PluginId}/{Provider}/{SubmissionId}/Form`.
 * Matches the project-wide plugin-sourced-data envelope (see `OrgbookResponse`): the
 * endpoint's own `data` field is itself the `{ schema, data }` submission form payload —
 * i.e. double-nested. This interface exists purely to unwrap that envelope; callers only
 * ever see the unwrapped `SubmissionFormResponse`.
 */
interface SubmissionFormEnvelope {
  profileId: string;
  pluginId: string;
  provider: string;
  submissionId: string;
  data: SubmissionFormResponse;
  populatedAt: string;
  cacheStatus?: string;
  cacheStore?: string;
}

/**
 * Loose shape of a form.io component schema node, covering only the fields
 * `patchHtmlElementTags` needs to traverse (containers, columns, table rows).
 */
interface FormioComponentSchema {
  type?: string;
  tag?: string;
  components?: FormioComponentSchema[];
  columns?: { components?: FormioComponentSchema[] }[];
  rows?: { components?: FormioComponentSchema[] }[][];
  [key: string]: unknown;
}

const PDF_RENDER_SETTLE_DELAY_MS = 300;

@Injectable({
  providedIn: 'root',
})
export class SubmissionPdfService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  /**
   * Fetches the form.io schema + submission data for a single submission.
   */
  fetchSubmissionForm(
    pluginId: string,
    provider: string,
    submissionId: string
  ): Observable<SubmissionFormResponse> {
    const url = `${this.baseUrl}/Submissions/${pluginId}/${provider}/${submissionId}/Form`;
    return this.http.get<SubmissionFormEnvelope>(url).pipe(
      retry({ count: 2, delay: 1000 }),
      map((envelope) => envelope.data),
      catchError((error) => {
        return throwError(() => error);
      })
    );
  }

  /**
   * Fetches and renders the submission as a PDF, then opens it in a new browser tab.
   *
   * The tab is opened synchronously, before any `await`, so it stays inside the click's
   * user-gesture context — most browsers treat a `window.open` issued after an `await`
   * (i.e. no longer synchronously inside the event handler) as not user-initiated and
   * silently block it as a popup. That means the tab necessarily sits open (and, without
   * `writePlaceholder`, blank/white) for the whole PDF generation time — `writePlaceholder`
   * puts a "Generating PDF…" message in it so that wait doesn't look like a broken/frozen
   * tab. Once the PDF is actually ready, the tab is navigated to the blob URL, replacing
   * the placeholder. If the popup was blocked anyway (`popup` is null), fall back to a
   * second `window.open` attempt as a best effort.
   */
  async viewSubmissionPdf(pluginId: string, provider: string, submissionId: string): Promise<void> {
    const popup = window.open('', '_blank');
    this.writePlaceholder(popup);
    const blob = await this.buildPdfBlob(pluginId, provider, submissionId);
    const url = URL.createObjectURL(blob);
    if (popup) {
      popup.location.href = url;
    } else {
      window.open(url, '_blank');
    }
    // Give the newly opened tab time to actually load the blob before revoking it.
    setTimeout(() => URL.revokeObjectURL(url), 60000);
  }

  /**
   * `document.write` is deprecated — a `window.open('', '_blank')` target already has a
   * full (empty) document, so the placeholder is built with plain DOM APIs instead.
   */
  private writePlaceholder(popup: Window | null): void {
    if (!popup) {
      return;
    }
    const doc = popup.document;
    doc.title = 'Generating PDF…';
    Object.assign(doc.body.style, {
      margin: '0',
      height: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontFamily: 'system-ui, sans-serif',
      color: '#495057',
      background: '#fff',
    });
    const message = doc.createElement('p');
    message.textContent = 'Generating PDF…';
    doc.body.appendChild(message);
  }

  /**
   * Fetches and renders the submission as a PDF, then triggers a browser download.
   */
  async downloadSubmissionPdf(
    pluginId: string,
    provider: string,
    submissionId: string,
    fileName = `submission-${submissionId}.pdf`
  ): Promise<void> {
    const blob = await this.buildPdfBlob(pluginId, provider, submissionId);
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  }

  private async buildPdfBlob(pluginId: string, provider: string, submissionId: string): Promise<Blob> {
    const response = await firstValueFrom(this.fetchSubmissionForm(pluginId, provider, submissionId));
    return this.generatePdf(response.schema, response.data);
  }

  /**
   * Renders the form.io schema + data off-screen, rasterizes it, and paginates the
   * result into a jsPDF document. Not meaningfully unit-testable as a whole (real DOM/canvas
   * rendering) — see the spec file for the module-boundary mock strategy. The submission-data
   * application step is extracted into `applySubmissionData` below specifically so that piece
   * of logic (which API is used, and that it's awaited) IS directly unit-testable.
   *
   * `submission` is already a full form.io submission object (`{ data: { <componentKey>: value } }`),
   * not a flat field map — it comes straight from `SubmissionFormResponse.data`, which the backend
   * populates in that shape. It must be passed through to formio as-is (see `applySubmissionData`).
   */
  private async generatePdf(schema: Record<string, unknown>, submission: Record<string, unknown>): Promise<Blob> {
    const container = this.createOffscreenContainer();
    const styleEl = this.injectPdfStyles(container.id);
    document.body.appendChild(container);

    try {
      this.patchHtmlElementTags(schema as FormioComponentSchema);
      // `flatten: true` renders every component in one pass, including ones inside
      // tabs/wizard pages — without it, a multi-tab CHEFS-style form would only
      // rasterize whichever tab happened to be active. `renderMode: 'form'` (the
      // default, made explicit here) keeps normal form.io templates so the scoped
      // CSS below targets predictable markup.
      const form = (await Formio.createForm(container, schema, {
        readOnly: true,
        renderMode: 'form',
        flatten: true,
      })) as unknown as FormioFormInstance;
      await form.ready;
      await this.applySubmissionData(form, submission);
      // Read-only mode disables the submit/next/previous buttons, it doesn't remove
      // them — hide anything the CSS missed (e.g. rendered after our style tag) and
      // neutralize link clicks, scoped to this container only (never the live page).
      this.stripInteractiveChrome(container);
      // Defensive buffer for actual browser paint/layout after applySubmissionData resolves —
      // canvas rasterization can still race a same-tick DOM mutation in some browsers. Not
      // relied on alone anymore now that we await formiojs's own completion signal above.
      await this.delay(PDF_RENDER_SETTLE_DELAY_MS);

      const canvas = await html2canvas(container, {
        scale: 2,
        onclone: (_clonedDocument, clonedContainer) => this.removeFormioRenderArtifacts(clonedContainer),
      });
      return this.canvasToPdfBlob(canvas);
    } finally {
      container.remove();
      styleEl.remove();
    }
  }

  /**
   * form.io's `htmlelement` component (CHEFS's authored heading/content blocks) wraps its
   * HTML in a `<p ref="html">` tag by default. When that authored content itself contains a
   * block-level element (a `<div>`, `<h3>`, etc. — common for heading markup authored in
   * CHEFS), the browser can't nest a block element inside a `<p>`, so its HTML parser
   * auto-closes the `<p>` early and re-parses the remainder as a sibling node — one authored
   * heading becomes two DOM nodes, both rendered (e.g. "C.1 Overview of Funding Request"
   * appearing twice). Forcing the tag to `div` before the schema reaches `Formio.createForm`
   * sidesteps the auto-repair entirely, since a `<div>` can legally contain block children.
   * Matches the fix Unity applies in its own `Details.js` before rendering the same schemas.
   * Mutates `schema` in place and recurses into containers/columns/table rows so it catches
   * `htmlelement` components at any nesting depth.
   */
  private patchHtmlElementTags(schema: FormioComponentSchema): void {
    if (!Array.isArray(schema?.components)) {
      return;
    }
    this.walkFormComponents(schema.components);
  }

  private walkFormComponents(components: FormioComponentSchema[] | undefined): void {
    if (!Array.isArray(components)) {
      return;
    }
    components.forEach((comp) => {
      if (comp.type === 'htmlelement' && (!comp.tag || comp.tag === 'p')) {
        comp.tag = 'div';
      }
      this.walkFormComponents(comp.components);
      if (Array.isArray(comp.columns)) {
        comp.columns.forEach((col) => this.walkFormComponents(col.components));
      }
      if (Array.isArray(comp.rows)) {
        comp.rows.forEach((row) => {
          if (Array.isArray(row)) {
            row.forEach((cell) => this.walkFormComponents(cell.components));
          }
        });
      }
    });
  }

  /**
   * Scoped (to this render's container id only — never global) CSS mirroring what Unity's
   * own print-to-PDF pipeline applies for the same form.io schemas, so the two outputs stay
   * visually aligned: hide read-only mode's disabled buttons (submit/next/previous), neutralize
   * link styling, pin heading font sizes, add visible borders/padding to data grid tables
   * (form.io's `datagrid` component renders borderless by default), and force-hide the select
   * widget's closed dropdown option list. The heading pinning matters here specifically because
   * this container renders inside the live app document rather than a dedicated print window —
   * Bootstrap sizes headings with `calc(... + vw)`, which resolves against the real browser
   * viewport, not this container's fixed width, so without pinning them the PDF's heading sizes
   * would vary with whatever window size happened to be open when it was generated.
   *
   * The select dropdown list matters because `formio.form.css` hides it (when closed) via
   * `visibility: hidden` + `position: absolute` rather than `display: none` — `visibility:hidden`
   * is a paint-only hide that still occupies layout, and html2canvas has known inconsistencies
   * rendering it faithfully, so without an explicit `display: none` override the full list of
   * every selectable option can show up baked into the rasterized PDF underneath the selected
   * value, instead of just the one value the applicant actually chose. `.choices__input` is the
   * same widget's search/filter box. `.formio-select-autocomplete-input` is a separate, plain
   * `<input>` form.io itself renders as a sibling right after every select component (for browser
   * autofill/accessibility) — confirmed via live DOM inspection to be the actual cause of a
   * previously-unidentified empty input box under every select field in the PDF; it isn't part
   * of Choices.js at all, so it needed its own rule.
   */
  private injectPdfStyles(containerId: string): HTMLStyleElement {
    const style = document.createElement('style');
    style.textContent = `
      #${containerId} button { display: none !important; }
      #${containerId} a {
        pointer-events: none !important;
        cursor: default !important;
        text-decoration: none !important;
        color: black !important;
      }
      #${containerId} h1, #${containerId} .h1 { font-size: 2.5rem !important; }
      #${containerId} h2, #${containerId} .h2 { font-size: 2rem !important; }
      #${containerId} h3, #${containerId} .h3 { font-size: 1.75rem !important; }
      #${containerId} h4, #${containerId} .h4 { font-size: 1.5rem !important; }
      #${containerId} h5, #${containerId} .h5 { font-size: 1.25rem !important; }
      #${containerId} h6, #${containerId} .h6 { font-size: 1rem !important; }
      #${containerId} .datagrid-table,
      #${containerId} .datagrid-table td,
      #${containerId} .datagrid-table th {
        border: 2px solid #ddd !important;
        padding: 10px;
      }
      #${containerId} .choices__list--dropdown,
      #${containerId} .choices__list[aria-expanded] {
        display: none !important;
      }
      #${containerId} .choices__input,
      #${containerId} .formio-select-autocomplete-input {
        display: none !important;
      }
    `;
    document.head.appendChild(style);
    return style;
  }

  /**
   * Removes (not just CSS-hides) form.io/Choices.js rendering artifacts that shouldn't appear in
   * a static PDF: the select widget's closed dropdown option list, its search/filter box, and
   * form.io's own `.formio-select-autocomplete-input` helper (a plain input it renders as a
   * sibling after every select field, unrelated to Choices.js, for browser autofill/accessibility
   * — this is what was actually causing the previously-unidentified leftover empty box). The
   * `display: none` CSS rules in `injectPdfStyles` target the same selectors and should be
   * sufficient on their own; removing the nodes outright here is a second, independent guarantee.
   */
  private removeFormioRenderArtifacts(root: HTMLElement): void {
    try {
      root
        .querySelectorAll('.choices__list--dropdown, .choices__input, .formio-select-autocomplete-input')
        .forEach((el) => el.remove());
    } catch {
      // Leave whatever's there — the CSS rules in injectPdfStyles are a (weaker) fallback.
    }
  }

  /** Belt-and-suspenders JS pass backing `injectPdfStyles` — scoped to `container` only. */
  private stripInteractiveChrome(container: HTMLElement): void {
    container.querySelectorAll<HTMLButtonElement>('button[disabled="disabled"]').forEach((btn) => {
      btn.style.display = 'none';
    });
    container.querySelectorAll<HTMLAnchorElement>('a').forEach((link) => {
      link.addEventListener('click', (event) => event.preventDefault());
    });
  }

  /**
   * Applies a submission to a rendered form instance and waits for formiojs to actually
   * finish the resulting redraw before returning. Prefers `setSubmission`, which returns the
   * promise formiojs resolves once the new data has been applied and re-rendered; falls back
   * to the bare `submission` property setter (fire-and-forget) only if a given form instance
   * doesn't expose `setSubmission`.
   *
   * `submission` is already `{ data: { <componentKey>: value } }` — it must be passed straight
   * through, not re-wrapped as `{ data: submission }`, which would double-nest it and leave
   * every component reading `undefined`.
   */
  private async applySubmissionData(form: FormioFormInstance, submission: Record<string, unknown>): Promise<void> {
    if (typeof form.setSubmission === 'function') {
      await form.setSubmission(submission as { data: Record<string, unknown> });
    } else {
      form.submission = submission as { data: Record<string, unknown> };
    }
  }

  private createOffscreenContainer(): HTMLDivElement {
    const container = document.createElement('div');
    // Unique per render so the scoped styles in `injectPdfStyles` never leak onto the live
    // page or onto a concurrent render (belt-and-suspenders alongside the component's own
    // in-flight guard).
    container.id = `submission-pdf-render-${crypto.randomUUID()}`;
    container.style.position = 'fixed';
    container.style.left = '-10000px';
    container.style.top = '0';
    container.style.width = '800px';
    container.style.backgroundColor = '#ffffff';
    return container;
  }

  private canvasToPdfBlob(canvas: HTMLCanvasElement): Blob {
    const pdf = new jsPDF({ unit: 'pt', format: 'a4' });
    const pageWidth = pdf.internal.pageSize.getWidth();
    const pageHeight = pdf.internal.pageSize.getHeight();
    const imgWidth = pageWidth;
    const imgHeight = (canvas.height * imgWidth) / canvas.width;
    const imgData = canvas.toDataURL('image/png');

    let heightLeft = imgHeight;
    let position = 0;
    pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
    heightLeft -= pageHeight;

    while (heightLeft > 0) {
      position = heightLeft - imgHeight;
      pdf.addPage();
      pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
      heightLeft -= pageHeight;
    }

    return pdf.output('blob');
  }

  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}
