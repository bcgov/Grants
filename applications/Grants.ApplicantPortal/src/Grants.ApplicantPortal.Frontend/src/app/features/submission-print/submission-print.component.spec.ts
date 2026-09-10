import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Formio } from 'formiojs';

import { SubmissionPrintComponent } from './submission-print.component';
import { SubmissionPdfService } from '../../core/services/submission-pdf.service';
import { SubmissionFormResponse } from '../../core/models/submission-form.model';

/** Minimal shape of the form.io form instance the component interacts with. */
interface MockFormioFormInstance {
  ready: Promise<unknown>;
  submission: { data: Record<string, unknown> };
  setSubmission?: jasmine.Spy;
  destroy?: jasmine.Spy;
}

/** Narrow accessor for the private, directly-testable pieces of rendering logic. */
interface SubmissionPrintComponentInternals {
  patchHtmlElementTags(schema: Record<string, unknown>): void;
  applySubmissionData(form: MockFormioFormInstance, data: Record<string, unknown>): Promise<void>;
  removeFormioRenderArtifacts(container: HTMLElement): void;
}

function toInternals(component: SubmissionPrintComponent): SubmissionPrintComponentInternals {
  return component as unknown as SubmissionPrintComponentInternals;
}

function makeFormResponse(overrides: Partial<SubmissionFormResponse> = {}): SubmissionFormResponse {
  return {
    schema: { display: 'form', components: [] },
    data: { data: { field1: 'value1' } },
    ...overrides,
  };
}

function makeMockForm(overrides: Partial<MockFormioFormInstance> = {}): MockFormioFormInstance {
  return {
    ready: Promise.resolve(),
    submission: { data: {} },
    setSubmission: jasmine.createSpy('setSubmission').and.resolveTo(undefined),
    destroy: jasmine.createSpy('destroy'),
    ...overrides,
  };
}

/**
 * Polls with real timers until `predicate` is true or `timeoutMs` elapses. `renderAndPrint`'s
 * async chain (`Formio.createForm` / `form.ready` / `applySubmissionData`) isn't reliably tracked
 * by Angular's zone-based `fixture.whenStable()` in the test harness — formiojs does its own
 * DOM/promise work outside Angular's zone — so tests wait on an observable side effect instead of
 * relying on zone stability.
 */
async function waitFor(predicate: () => boolean, timeoutMs = 3000): Promise<void> {
  const start = Date.now();
  while (!predicate()) {
    if (Date.now() - start > timeoutMs) {
      throw new Error('waitFor: timed out waiting for condition');
    }
    await new Promise((resolve) => setTimeout(resolve, 10));
  }
}

describe('SubmissionPrintComponent', () => {
  let fixture: ComponentFixture<SubmissionPrintComponent>;
  let component: SubmissionPrintComponent;
  let submissionPdfServiceSpy: jasmine.SpyObj<SubmissionPdfService>;

  const validParams = { pluginId: 'plugin-1', provider: 'prov-1', submissionId: 'sub-1' };

  // `renderAndPrint` is fire-and-forget and has no way to be cancelled once started — a test that
  // triggers it (via `detectChanges()`) but doesn't wait for it to settle leaves it running in the
  // background, where its eventual `window.print()` call can land on a LATER test's fresh spy
  // (confirmed: this caused real, order-dependent cross-test failures — "called 2 times" in one
  // test, "not to have been called" in another, depending on run order). Destroying the fixture
  // sets the component's `destroyed` guard, which the fire-and-forget chain checks after every
  // await, neutralizing any leak regardless of whether a given test bothered to wait for it.
  afterEach(() => {
    fixture?.destroy();
  });

  /**
   * Configures TestBed and creates the component but does NOT call `fixture.detectChanges()` —
   * callers that want `ngOnInit` to actually run (and therefore need `Formio.createForm` /
   * `window.print` stubbed first) trigger it themselves via `detectChanges()`. Tests that only
   * exercise a private, directly-testable method skip that entirely and never touch formio or
   * the browser's print engine — matching the module-boundary spy philosophy used in
   * `submission-pdf.service.spec.ts`.
   */
  function configureComponent(paramMap: Record<string, string>, formResponse = makeFormResponse()): void {
    submissionPdfServiceSpy = jasmine.createSpyObj<SubmissionPdfService>('SubmissionPdfService', [
      'fetchSubmissionForm',
    ]);
    submissionPdfServiceSpy.fetchSubmissionForm.and.returnValue(of(formResponse));

    TestBed.configureTestingModule({
      imports: [SubmissionPrintComponent],
      providers: [
        { provide: SubmissionPdfService, useValue: submissionPdfServiceSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap(paramMap) } },
        },
      ],
    });

    fixture = TestBed.createComponent(SubmissionPrintComponent);
    component = fixture.componentInstance;
  }

  describe('rendering flow', () => {
    it('should create', () => {
      spyOn(Formio, 'createForm').and.returnValue(Promise.resolve(makeMockForm()) as unknown as Promise<unknown>);
      spyOn(window, 'print');

      configureComponent(validParams);
      fixture.detectChanges();

      expect(component).toBeTruthy();
    });

    it('reads pluginId/provider/submissionId from the route and calls fetchSubmissionForm with them', () => {
      spyOn(Formio, 'createForm').and.returnValue(Promise.resolve(makeMockForm()) as unknown as Promise<unknown>);
      spyOn(window, 'print');

      configureComponent(validParams);
      fixture.detectChanges();

      expect(submissionPdfServiceSpy.fetchSubmissionForm).toHaveBeenCalledWith('plugin-1', 'prov-1', 'sub-1');
    });

    it('sets hasError and skips fetchSubmissionForm entirely when a route param is missing', () => {
      configureComponent({ pluginId: 'plugin-1', provider: 'prov-1' });

      fixture.detectChanges();

      expect(component.hasError).toBeTrue();
      expect(component.isLoading).toBeFalse();
      expect(submissionPdfServiceSpy.fetchSubmissionForm).not.toHaveBeenCalled();
    });

    it('sets hasError when fetchSubmissionForm errors', () => {
      configureComponent(validParams, makeFormResponse());
      submissionPdfServiceSpy.fetchSubmissionForm.and.returnValue(throwError(() => new Error('boom')));

      fixture.detectChanges();

      expect(component.hasError).toBeTrue();
      expect(component.isLoading).toBeFalse();
    });

    it('renders the form and applies the submission data, without printing automatically', async () => {
      const mockForm = makeMockForm();
      spyOn(Formio, 'createForm').and.returnValue(Promise.resolve(mockForm) as unknown as Promise<unknown>);
      const printSpy = spyOn(window, 'print');

      configureComponent(validParams);
      fixture.detectChanges();
      await waitFor(() => !component.isLoading);

      expect(mockForm.setSubmission).toHaveBeenCalledWith(makeFormResponse().data);
      expect(component.hasError).toBeFalse();
      // Printing is deferred to the applicant clicking the page's Print button — rendering
      // completing must never trigger window.print() on its own.
      expect(printSpy).not.toHaveBeenCalled();
    });

    it('printSubmission() calls window.print()', async () => {
      const mockForm = makeMockForm();
      spyOn(Formio, 'createForm').and.returnValue(Promise.resolve(mockForm) as unknown as Promise<unknown>);
      const printSpy = spyOn(window, 'print');

      configureComponent(validParams);
      fixture.detectChanges();
      await waitFor(() => !component.isLoading);

      component.printSubmission();

      expect(printSpy).toHaveBeenCalledTimes(1);
    });

    it('sets hasError and never calls window.print() if the form fails to render', async () => {
      spyOn(Formio, 'createForm').and.returnValue(Promise.reject(new Error('render failed')));
      const printSpy = spyOn(window, 'print');

      configureComponent(validParams);
      fixture.detectChanges();
      await waitFor(() => component.hasError);

      expect(component.hasError).toBeTrue();
      expect(component.isLoading).toBeFalse();
      expect(printSpy).not.toHaveBeenCalled();
    });

    it('destroys the formio instance when the component is destroyed', async () => {
      const mockForm = makeMockForm();
      spyOn(Formio, 'createForm').and.returnValue(Promise.resolve(mockForm) as unknown as Promise<unknown>);
      spyOn(window, 'print');

      configureComponent(validParams);
      fixture.detectChanges();
      await waitFor(() => !component.isLoading);

      fixture.destroy();

      expect(mockForm.destroy).toHaveBeenCalledWith(true);
    });
  });

  describe('patchHtmlElementTags', () => {
    beforeEach(() => configureComponent(validParams));

    it('changes an htmlelement component with no tag to div', () => {
      const schema = { components: [{ type: 'htmlelement', key: 'heading1' }] };

      toInternals(component).patchHtmlElementTags(schema);

      expect((schema.components[0] as unknown as Record<string, unknown>)['tag']).toBe('div');
    });

    it('changes an htmlelement component explicitly tagged p to div', () => {
      const schema = { components: [{ type: 'htmlelement', tag: 'p', key: 'heading1' }] };

      toInternals(component).patchHtmlElementTags(schema);

      expect((schema.components[0] as unknown as Record<string, unknown>)['tag']).toBe('div');
    });

    it('leaves an htmlelement component with a non-p tag untouched', () => {
      const schema = { components: [{ type: 'htmlelement', tag: 'span', key: 'heading1' }] };

      toInternals(component).patchHtmlElementTags(schema);

      expect((schema.components[0] as unknown as Record<string, unknown>)['tag']).toBe('span');
    });

    it('leaves non-htmlelement components untouched', () => {
      const schema = { components: [{ type: 'textfield', key: 'name' }] };

      toInternals(component).patchHtmlElementTags(schema);

      expect((schema.components[0] as unknown as Record<string, unknown>)['tag']).toBeUndefined();
    });

    it('recurses into nested containers, columns, and table rows', () => {
      const schema = {
        components: [
          { type: 'panel', components: [{ type: 'htmlelement', key: 'nestedHeading' }] },
          { type: 'columns', columns: [{ components: [{ type: 'htmlelement', key: 'columnHeading' }] }] },
          { type: 'table', rows: [[{ components: [{ type: 'htmlelement', key: 'cellHeading' }] }]] },
        ],
      };

      toInternals(component).patchHtmlElementTags(schema);

      const [panel, columns, table] = schema.components as unknown as Array<Record<string, unknown>>;
      expect(((panel['components'] as Record<string, unknown>[])[0])['tag']).toBe('div');
      expect(
        (((columns['columns'] as Record<string, unknown>[])[0]['components'] as Record<string, unknown>[])[0])['tag']
      ).toBe('div');
      expect(
        (((table['rows'] as Record<string, unknown>[][])[0][0]['components'] as Record<string, unknown>[])[0])['tag']
      ).toBe('div');
    });

    it('does nothing when the schema has no components array', () => {
      expect(() => toInternals(component).patchHtmlElementTags({})).not.toThrow();
    });
  });

  describe('applySubmissionData', () => {
    beforeEach(() => configureComponent(validParams));

    it('prefers setSubmission and awaits its promise when the form instance exposes it', async () => {
      const form = makeMockForm();
      const data = { data: { organizationName: 'Fraser Valley Youth Support Association' } };

      await toInternals(component).applySubmissionData(form, data);

      expect(form.setSubmission).toHaveBeenCalledWith(data);
      expect(form.setSubmission).toHaveBeenCalledTimes(1);
      expect(form.setSubmission).not.toHaveBeenCalledWith({ data });
      expect(form.submission).toEqual({ data: {} });
    });

    it('falls back to the submission property setter when setSubmission is not available', async () => {
      const form = makeMockForm({ setSubmission: undefined });
      const data = { data: { organizationName: 'Northern Digital Access Cooperative' } };

      await toInternals(component).applySubmissionData(form, data);

      expect(form.submission).toEqual(data);
      expect(form.submission).not.toEqual({ data });
    });
  });

  describe('removeFormioRenderArtifacts', () => {
    let root: HTMLElement;

    beforeEach(() => {
      configureComponent(validParams);
      root = document.createElement('div');
      document.body.appendChild(root);
    });

    afterEach(() => root.remove());

    it('removes the closed dropdown option list, the search input, and the select autocomplete helper', () => {
      root.innerHTML = `
        <div class="choices">
          <div class="choices__inner">
            <div class="choices__list choices__list--single">Selected Value</div>
            <input type="search" class="choices__input" />
          </div>
          <div class="choices__list choices__list--dropdown">
            <div class="choices__item">Option A</div>
          </div>
        </div>
        <input type="text" class="formio-select-autocomplete-input" tabindex="-1" aria-label="autocomplete" />
      `;

      toInternals(component).removeFormioRenderArtifacts(root);

      expect(root.querySelector('.choices__list--dropdown')).toBeNull();
      expect(root.querySelector('.choices__input')).toBeNull();
      expect(root.querySelector('.formio-select-autocomplete-input')).toBeNull();
      expect(root.querySelector('.choices__list--single')?.textContent).toBe('Selected Value');
    });

    it('does nothing when no formio select widget is present', () => {
      root.innerHTML = '<div class="plain-field">Some Value</div>';

      expect(() => toInternals(component).removeFormioRenderArtifacts(root)).not.toThrow();
      expect(root.textContent).toContain('Some Value');
    });
  });

});
