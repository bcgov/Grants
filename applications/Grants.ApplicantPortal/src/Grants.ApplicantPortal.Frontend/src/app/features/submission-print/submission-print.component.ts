import { Component, ElementRef, OnDestroy, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { Formio } from 'formiojs';
import { SubmissionPdfService } from '../../core/services/submission-pdf.service';

/**
 * Minimal shape of the form.io form instance we interact with — formiojs ships `Promise<any>` from `createForm`.
 * Mirrors the interface of the same name that used to live in `submission-pdf.service.ts` before the
 * html2canvas/jsPDF rasterization pipeline was replaced by this native print-to-PDF page.
 */
interface FormioFormInstance {
  ready: Promise<unknown>;
  submission: { data: Record<string, unknown> };
  setSubmission?: (submission: { data: Record<string, unknown> }, flags?: Record<string, unknown>) => Promise<unknown>;
  destroy?: (deleteState?: boolean) => void;
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

/**
 * Renders a single submission as a real, visible, print-ready page and hands it to the browser's
 * own print engine (`window.print()`) instead of rasterizing it with html2canvas + jsPDF. Opened
 * as its own tab (via `SubmissionPdfService.viewSubmissionPdf`/`downloadSubmissionPdf`) outside the
 * main app shell — same pattern as the sibling Unity/UGM system uses for the same form.io data, and
 * mirrors how `workspace-selector` is routed outside `LayoutComponent` in `app.routes.ts`.
 *
 * `fetchSubmissionForm` is unchanged from the old pipeline. Everything downstream of it (the CHEFS
 * `htmlelement` tag patch, submission-data application, interactive-chrome stripping) is ported here
 * essentially as-is — see each method's doc comment for the original "why".
 */
@Component({
  selector: 'app-submission-print',
  standalone: true,
  imports: [],
  templateUrl: './submission-print.component.html',
  styleUrls: ['./submission-print.component.scss'],
  // Formio.createForm inserts real DOM nodes into `formContainer` directly (not through Angular's
  // template compiler), so those nodes never receive Angular's `_ngcontent-*` scoping attribute —
  // emulated encapsulation's rewritten selectors (`button[_ngcontent-c123]`) would silently fail to
  // match them. This page has no shared layout to protect against style leakage, so encapsulation is
  // disabled and every rule in the stylesheet is scoped manually via the `.submission-print-container` class.
  encapsulation: ViewEncapsulation.None,
})
export class SubmissionPrintComponent implements OnInit, OnDestroy {
  @ViewChild('formContainer', { static: true }) private readonly formContainerRef!: ElementRef<HTMLDivElement>;

  isLoading = true;
  hasError = false;

  private readonly destroy$ = new Subject<void>();
  private formInstance: FormioFormInstance | null = null;
  // `renderAndPrint` is fire-and-forget (kicked off from a subscription callback, not awaited by
  // any caller) and has no way to be cancelled once started — without this guard, a component
  // destroyed mid-render (tab closed, navigated away) would still call `window.print()` later,
  // against a torn-down page. Checked after every meaningful await inside `renderAndPrint`.
  private destroyed = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly submissionPdfService: SubmissionPdfService
  ) {}

  ngOnInit(): void {
    const pluginId = this.route.snapshot.paramMap.get('pluginId') ?? '';
    const provider = this.route.snapshot.paramMap.get('provider') ?? '';
    const submissionId = this.route.snapshot.paramMap.get('submissionId') ?? '';

    if (!pluginId || !provider || !submissionId) {
      this.hasError = true;
      this.isLoading = false;
      return;
    }

    // The browser's own Print/"Save as PDF" dialog suggests the current tab's document.title as
    // the default filename — without this it would suggest the app's generic index.html title.
    document.title = `submission-${submissionId}`;

    this.submissionPdfService
      .fetchSubmissionForm(pluginId, provider, submissionId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          void this.renderAndPrint(response.schema, response.data);
        },
        error: (error: unknown) => {
          // eslint-disable-next-line no-console
          console.error('Failed to fetch submission form for printing', error);
          this.hasError = true;
          this.isLoading = false;
        },
      });
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.destroy$.next();
    this.destroy$.complete();
    this.formInstance?.destroy?.(true);
  }

  /** Bound to the page's Print button — the applicant decides when to open the print dialog. */
  printSubmission(): void {
    window.print();
  }

  /**
   * Renders the form.io schema + data into the real, visible page container and waits for formio
   * to settle — printing itself is deferred to the applicant clicking the page's Print button
   * (`printSubmission()`) rather than firing automatically, so they can review the rendered
   * submission first. Not meaningfully unit-testable as a whole (real DOM/formio rendering) — see
   * the spec file for the module-boundary spy strategy. The individually-testable pieces (schema
   * patching, submission-data application) are extracted into their own methods below, same
   * philosophy as the old `generatePdf`.
   */
  private async renderAndPrint(schema: Record<string, unknown>, submission: Record<string, unknown>): Promise<void> {
    const container = this.formContainerRef.nativeElement;

    try {
      this.patchHtmlElementTags(schema as FormioComponentSchema);
      // `flatten: true` renders every component in one pass, including ones inside tabs/wizard
      // pages — without it, a multi-tab CHEFS-style form would only render whichever tab happened
      // to be active. `renderMode: 'form'` (the default, made explicit here) keeps normal form.io
      // templates so the print stylesheet targets predictable markup.
      const form = (await Formio.createForm(container, schema, {
        readOnly: true,
        renderMode: 'form',
        flatten: true,
      })) as unknown as FormioFormInstance;
      if (this.destroyed) {
        return;
      }
      this.formInstance = form;
      await form.ready;
      if (this.destroyed) {
        return;
      }
      await this.applySubmissionData(form, submission);
      if (this.destroyed) {
        return;
      }

      // Read-only mode disables the submit/next/previous buttons, it doesn't remove them — hide
      // anything the stylesheet missed and neutralize link clicks, scoped to this container only.
      this.stripInteractiveChrome(container);
      this.removeFormioRenderArtifacts(container);

      this.isLoading = false;
    } catch (error) {
      // eslint-disable-next-line no-console
      console.error('Failed to render submission for printing', error);
      this.hasError = true;
      this.isLoading = false;
    }
  }

  /**
   * form.io's `htmlelement` component (CHEFS's authored heading/content blocks) wraps its HTML in a
   * `<p ref="html">` tag by default. When that authored content itself contains a block-level element
   * (a `<div>`, `<h3>`, etc. — common for heading markup authored in CHEFS), the browser can't nest a
   * block element inside a `<p>`, so its HTML parser auto-closes the `<p>` early and re-parses the
   * remainder as a sibling node — one authored heading becomes two DOM nodes, both rendered (e.g.
   * "C.1 Overview of Funding Request" appearing twice). Forcing the tag to `div` before the schema
   * reaches `Formio.createForm` sidesteps the auto-repair entirely, since a `<div>` can legally
   * contain block children. Matches the fix Unity applies in its own `Details.js` before rendering
   * the same schemas. Mutates `schema` in place and recurses into containers/columns/table rows so it
   * catches `htmlelement` components at any nesting depth.
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
   * Removes (not just CSS-hides) form.io/Choices.js rendering artifacts that shouldn't appear in a
   * printed page: the select widget's closed dropdown option list, its search/filter box, and
   * form.io's own `.formio-select-autocomplete-input` helper (a plain input it renders as a sibling
   * after every select field, unrelated to Choices.js, for browser autofill/accessibility). Applied
   * directly to the real rendered container — unlike the old html2canvas pipeline, there is no cloned
   * document to operate on here. The `display: none` rules in the component stylesheet target the
   * same selectors and should be sufficient on their own; removing the nodes outright is a second,
   * independent guarantee.
   */
  private removeFormioRenderArtifacts(container: HTMLElement): void {
    try {
      container
        .querySelectorAll('.choices__list--dropdown, .choices__input, .formio-select-autocomplete-input')
        .forEach((el) => el.remove());
    } catch {
      // Leave whatever's there — the CSS rules in the component stylesheet are a (weaker) fallback.
    }
  }

  /** Belt-and-suspenders JS pass backing the component stylesheet — scoped to `container` only. */
  private stripInteractiveChrome(container: HTMLElement): void {
    container.querySelectorAll<HTMLButtonElement>('button[disabled="disabled"]').forEach((btn) => {
      btn.style.display = 'none';
    });
    container.querySelectorAll<HTMLAnchorElement>('a').forEach((link) => {
      link.addEventListener('click', (event) => event.preventDefault());
    });
  }

  /**
   * Applies a submission to a rendered form instance and waits for formiojs to actually finish the
   * resulting redraw before returning. Prefers `setSubmission`, which returns the promise formiojs
   * resolves once the new data has been applied and re-rendered; falls back to the bare `submission`
   * property setter (fire-and-forget) only if a given form instance doesn't expose `setSubmission`.
   *
   * `submission` is already `{ data: { <componentKey>: value } }` — it must be passed straight
   * through, not re-wrapped as `{ data: submission }`, which would double-nest it and leave every
   * component reading `undefined`.
   */
  private async applySubmissionData(form: FormioFormInstance, submission: Record<string, unknown>): Promise<void> {
    if (typeof form.setSubmission === 'function') {
      await form.setSubmission(submission as { data: Record<string, unknown> });
    } else {
      form.submission = submission as { data: Record<string, unknown> };
    }
  }
}
