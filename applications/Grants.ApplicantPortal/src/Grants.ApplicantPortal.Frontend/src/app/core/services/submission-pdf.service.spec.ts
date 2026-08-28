import { TestBed, fakeAsync, tick, flushMicrotasks } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { SubmissionPdfService } from './submission-pdf.service';
import { SubmissionFormResponse } from '../models/submission-form.model';
import { environment } from '../../../environments/environment';

const BASE = environment.apiUrl;

/** Minimal shape of the form.io form instance the service interacts with — mirrors `FormioFormInstance`. */
interface MockFormioFormInstance {
  ready: Promise<unknown>;
  submission: { data: Record<string, unknown> };
  setSubmission?: jasmine.Spy;
}

/** Narrow accessor for the private, canvas/formio-heavy rendering step — see spec notes below. */
interface SubmissionPdfServiceInternals {
  generatePdf(schema: Record<string, unknown>, data: Record<string, unknown>): Promise<Blob>;
  applySubmissionData(form: MockFormioFormInstance, data: Record<string, unknown>): Promise<void>;
  removeFormioRenderArtifacts(root: HTMLElement): void;
  patchHtmlElementTags(schema: Record<string, unknown>): void;
}

function toInternals(service: SubmissionPdfService): SubmissionPdfServiceInternals {
  return service as unknown as SubmissionPdfServiceInternals;
}

/** The unwrapped `{ schema, data }` shape callers of the service should receive. */
function makeFormResponse(overrides: Partial<SubmissionFormResponse> = {}): SubmissionFormResponse {
  return {
    schema: { display: 'form', components: [] },
    data: { field1: 'value1' },
    ...overrides,
  };
}

/**
 * The REAL wire shape of `GET /Submissions/:pluginId/:provider/:submissionId/Form` —
 * a project-wide plugin-data envelope whose own `data` field is itself the
 * `{ schema, data }` submission form payload (double-nested). Mirrors
 * `RetrieveSubmissionFormResponse` on the backend.
 */
function makeFormEnvelope(formOverrides: Partial<SubmissionFormResponse> = {}): Record<string, unknown> {
  return {
    profileId: 'profile-1',
    pluginId: 'plugin-1',
    provider: 'prov-1',
    submissionId: 'sub-1',
    data: makeFormResponse(formOverrides),
    populatedAt: '2026-01-01T00:00:00Z',
    cacheStatus: 'Hit',
    cacheStore: 'Redis',
  };
}

describe('SubmissionPdfService', () => {
  let service: SubmissionPdfService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SubmissionPdfService],
    });

    service = TestBed.inject(SubmissionPdfService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('fetchSubmissionForm', () => {
    it('sends a GET request to /Submissions/:pluginId/:provider/:submissionId/Form', () => {
      service.fetchSubmissionForm('plugin-1', 'prov-1', 'sub-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
      expect(req.request.method).toBe('GET');
      req.flush(makeFormEnvelope());
    });

    it('unwraps the double-nested envelope, returning the inner schema and data', (done) => {
      const envelope = makeFormEnvelope({ schema: { display: 'form' }, data: { name: 'Org A' } });

      service.fetchSubmissionForm('plugin-1', 'prov-1', 'sub-1').subscribe((result) => {
        // Proves the unwrap actually happened — result must equal envelope.data (the inner
        // { schema, data } payload), not the envelope itself or any of its other fields.
        expect(result).toEqual({ schema: { display: 'form' }, data: { name: 'Org A' } });
        expect(result.schema).toEqual({ display: 'form' });
        expect(result.data).toEqual({ name: 'Org A' });
        expect((result as unknown as Record<string, unknown>)['pluginId']).toBeUndefined();
        done();
      });

      const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
      req.flush(envelope);
    });

    it('propagates an error after retries are exhausted', fakeAsync(() => {
      let receivedError: unknown;
      service.fetchSubmissionForm('plugin-1', 'prov-1', 'sub-1').subscribe({
        next: () => fail('expected an error, got a value'),
        error: (err) => {
          receivedError = err;
        },
      });

      // retry({ count: 2, delay: 1000 }) => 1 initial attempt + 2 retries = 3 total
      // requests, each retry gated behind the 1000ms delay timer.
      for (let i = 0; i < 3; i++) {
        const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
        req.flush('upstream error', { status: 502, statusText: 'Bad Gateway' });
        if (i < 2) {
          tick(1000);
        }
      }

      expect(receivedError).toBeTruthy();
    }));
  });

  describe('applySubmissionData', () => {
    it('prefers setSubmission and awaits its promise when the form instance exposes it', async () => {
      const setSubmissionSpy = jasmine.createSpy('setSubmission').and.resolveTo(undefined);
      const form: MockFormioFormInstance = {
        ready: Promise.resolve(),
        submission: { data: {} },
        setSubmission: setSubmissionSpy,
      };
      // `data` here IS the full form.io submission object — { data: { <fields> } } — exactly as
      // SubmissionFormResponse.data arrives from the backend. It must reach formio unwrapped.
      const data = { data: { organizationName: 'Fraser Valley Youth Support Association' } };

      await toInternals(service).applySubmissionData(form, data);

      expect(setSubmissionSpy).toHaveBeenCalledWith(data);
      expect(setSubmissionSpy).toHaveBeenCalledTimes(1);
      // Regression guard: must NOT be double-wrapped as { data } (a bug that shipped once already —
      // it silently left every form.io component reading undefined for its value).
      expect(setSubmissionSpy).not.toHaveBeenCalledWith({ data });
      // The setter fallback must not also be exercised when setSubmission is available.
      expect(form.submission).toEqual({ data: {} });
    });

    it('waits for the setSubmission promise to resolve before returning', async () => {
      let resolveSetSubmission!: () => void;
      const setSubmissionSpy = jasmine
        .createSpy('setSubmission')
        .and.returnValue(new Promise<void>((resolve) => (resolveSetSubmission = resolve)));
      const form: MockFormioFormInstance = {
        ready: Promise.resolve(),
        submission: { data: {} },
        setSubmission: setSubmissionSpy,
      };

      let resolved = false;
      const applyPromise = toInternals(service)
        .applySubmissionData(form, { field1: 'value1' })
        .then(() => (resolved = true));

      // Give any stray microtasks a chance to run — applySubmissionData must still be pending.
      await Promise.resolve();
      expect(resolved).toBeFalse();

      resolveSetSubmission();
      await applyPromise;
      expect(resolved).toBeTrue();
    });

    it('falls back to the submission property setter when setSubmission is not available', async () => {
      const form: MockFormioFormInstance = {
        ready: Promise.resolve(),
        submission: { data: {} },
      };
      const data = { data: { organizationName: 'Northern Digital Access Cooperative' } };

      await toInternals(service).applySubmissionData(form, data);

      expect(form.submission).toEqual(data);
      expect(form.submission).not.toEqual({ data });
    });
  });

  describe('patchHtmlElementTags', () => {
    it('changes an htmlelement component with no tag to div', () => {
      const schema = { components: [{ type: 'htmlelement', key: 'heading1' }] };

      toInternals(service).patchHtmlElementTags(schema);

      expect((schema.components[0] as unknown as Record<string, unknown>)['tag']).toBe('div');
    });

    it('changes an htmlelement component explicitly tagged p to div', () => {
      const schema = { components: [{ type: 'htmlelement', tag: 'p', key: 'heading1' }] };

      toInternals(service).patchHtmlElementTags(schema);

      expect((schema.components[0] as unknown as Record<string, unknown>)['tag']).toBe('div');
    });

    it('leaves an htmlelement component with a non-p tag untouched', () => {
      const schema = { components: [{ type: 'htmlelement', tag: 'span', key: 'heading1' }] };

      toInternals(service).patchHtmlElementTags(schema);

      expect((schema.components[0] as unknown as Record<string, unknown>)['tag']).toBe('span');
    });

    it('leaves non-htmlelement components untouched', () => {
      const schema = { components: [{ type: 'textfield', key: 'name' }] };

      toInternals(service).patchHtmlElementTags(schema);

      expect((schema.components[0] as unknown as Record<string, unknown>)['tag']).toBeUndefined();
    });

    it('recurses into nested containers, columns, and table rows', () => {
      const schema = {
        components: [
          {
            type: 'panel',
            components: [{ type: 'htmlelement', key: 'nestedHeading' }],
          },
          {
            type: 'columns',
            columns: [{ components: [{ type: 'htmlelement', key: 'columnHeading' }] }],
          },
          {
            type: 'table',
            rows: [[{ components: [{ type: 'htmlelement', key: 'cellHeading' }] }]],
          },
        ],
      };

      toInternals(service).patchHtmlElementTags(schema);

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
      expect(() => toInternals(service).patchHtmlElementTags({})).not.toThrow();
    });
  });

  describe('removeFormioRenderArtifacts', () => {
    let root: HTMLElement;

    beforeEach(() => {
      root = document.createElement('div');
      document.body.appendChild(root);
    });

    afterEach(() => {
      root.remove();
    });

    it('removes the closed dropdown option list, the search input, and the select autocomplete helper', () => {
      root.innerHTML = `
        <div class="choices">
          <div class="choices__inner">
            <div class="choices__list choices__list--single">Selected Value</div>
            <input type="search" class="choices__input" />
          </div>
          <div class="choices__list choices__list--dropdown">
            <div class="choices__item">Option A</div>
            <div class="choices__item">Option B</div>
          </div>
        </div>
        <input type="text" class="formio-select-autocomplete-input" tabindex="-1" aria-label="autocomplete" />
      `;

      toInternals(service).removeFormioRenderArtifacts(root);

      expect(root.querySelector('.choices__list--dropdown')).toBeNull();
      expect(root.querySelector('.choices__input')).toBeNull();
      expect(root.querySelector('.formio-select-autocomplete-input')).toBeNull();
      // The selected value display and its wrapper are untouched.
      expect(root.querySelector('.choices__list--single')?.textContent).toBe('Selected Value');
    });

    it('does nothing when no formio select widget is present', () => {
      root.innerHTML = '<div class="plain-field">Some Value</div>';

      expect(() => toInternals(service).removeFormioRenderArtifacts(root)).not.toThrow();
      expect(root.textContent).toContain('Some Value');
    });
  });

  // The formio/html2canvas/jspdf rendering pipeline is real-DOM/canvas heavy and not
  // meaningfully unit-testable, so `generatePdf` is stubbed at the method boundary below.
  // The submission-application decision itself (setSubmission vs. the property-setter
  // fallback) is covered directly above via `applySubmissionData`.

  /**
   * A `window.open` return value stub — a real (detached) `Document`, so the service's plain
   * DOM manipulation (`document.title`, `document.body.appendChild`, etc.) works exactly as
   * it would against a real popup, plus a settable `location.href`.
   */
  function makeFakePopup(): Window {
    return {
      document: document.implementation.createHTMLDocument(''),
      location: { href: '' },
    } as unknown as Window;
  }

  describe('viewSubmissionPdf', () => {
    it('opens the tab synchronously (before the PDF is ready) so it stays inside the click gesture, writes a placeholder into it, then navigates it to the blob URL', async () => {
      const blob = new Blob(['pdf-bytes'], { type: 'application/pdf' });
      const fakePopup = makeFakePopup();
      spyOn(toInternals(service), 'generatePdf').and.returnValue(Promise.resolve(blob));
      spyOn(URL, 'createObjectURL').and.returnValue('blob:mock-url');
      const openSpy = spyOn(window, 'open').and.returnValue(fakePopup);
      spyOn(URL, 'revokeObjectURL');

      const promise = service.viewSubmissionPdf('plugin-1', 'prov-1', 'sub-1');

      // The popup must already be open at this point — before the PDF fetch/generation
      // has resolved — otherwise the browser would treat it as no longer user-initiated.
      expect(openSpy).toHaveBeenCalledWith('', '_blank');
      expect(openSpy).toHaveBeenCalledTimes(1);
      // A placeholder is written into the tab immediately so it doesn't sit blank/white
      // for the whole generation time (which reads as broken/frozen to the applicant).
      expect(fakePopup.document.title).toBe('Generating PDF…');
      expect(fakePopup.document.body.textContent).toContain('Generating PDF');

      const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
      req.flush(makeFormEnvelope());
      await promise;

      // Once the blob is ready, the already-open tab is navigated to it — no second
      // window.open call, which would be blocked as an out-of-gesture popup.
      expect(openSpy).toHaveBeenCalledTimes(1);
      expect(fakePopup.location.href).toBe('blob:mock-url');
    });

    it('falls back to a second window.open attempt if the initial popup was blocked', async () => {
      const blob = new Blob(['pdf-bytes'], { type: 'application/pdf' });
      spyOn(toInternals(service), 'generatePdf').and.returnValue(Promise.resolve(blob));
      spyOn(URL, 'createObjectURL').and.returnValue('blob:mock-url');
      const openSpy = spyOn(window, 'open').and.returnValue(null);
      spyOn(URL, 'revokeObjectURL');

      const promise = service.viewSubmissionPdf('plugin-1', 'prov-1', 'sub-1');
      const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
      req.flush(makeFormEnvelope());
      await promise;

      expect(openSpy).toHaveBeenCalledTimes(2);
      expect(openSpy.calls.argsFor(0)).toEqual(['', '_blank']);
      expect(openSpy.calls.argsFor(1)).toEqual(['blob:mock-url', '_blank']);
    });

    it('revokes the object URL after a delay, once the opened tab has had time to load it', fakeAsync(() => {
      const blob = new Blob(['pdf-bytes'], { type: 'application/pdf' });
      const fakePopup = makeFakePopup();
      spyOn(toInternals(service), 'generatePdf').and.returnValue(Promise.resolve(blob));
      spyOn(URL, 'createObjectURL').and.returnValue('blob:mock-url');
      const revokeSpy = spyOn(URL, 'revokeObjectURL');
      spyOn(window, 'open').and.returnValue(fakePopup);

      service.viewSubmissionPdf('plugin-1', 'prov-1', 'sub-1');
      const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
      req.flush(makeFormEnvelope());
      flushMicrotasks();

      expect(revokeSpy).not.toHaveBeenCalled();

      tick(60000);

      expect(revokeSpy).toHaveBeenCalledWith('blob:mock-url');
    }));

    it('propagates the fetch error without attempting to generate a PDF', fakeAsync(() => {
      const generateSpy = spyOn(toInternals(service), 'generatePdf');
      let rejected = false;
      service.viewSubmissionPdf('plugin-1', 'prov-1', 'sub-1').catch(() => (rejected = true));

      for (let i = 0; i < 3; i++) {
        const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
        req.flush('not found', { status: 404, statusText: 'Not Found' });
        if (i < 2) {
          tick(1000);
        }
      }
      flushMicrotasks();

      expect(rejected).toBeTrue();
      expect(generateSpy).not.toHaveBeenCalled();
    }));
  });

  describe('downloadSubmissionPdf', () => {
    it('creates a temporary download link and revokes the object URL afterward', async () => {
      const blob = new Blob(['pdf-bytes'], { type: 'application/pdf' });
      spyOn(toInternals(service), 'generatePdf').and.returnValue(Promise.resolve(blob));
      spyOn(URL, 'createObjectURL').and.returnValue('blob:mock-url');
      const revokeSpy = spyOn(URL, 'revokeObjectURL');
      const clickSpy = spyOn(HTMLAnchorElement.prototype, 'click');

      let capturedLink: HTMLAnchorElement | undefined;
      const originalAppendChild = document.body.appendChild.bind(document.body);
      spyOn(document.body, 'appendChild').and.callFake(<T extends Node>(node: T): T => {
        if (node instanceof HTMLAnchorElement) {
          capturedLink = node;
        }
        return originalAppendChild(node);
      });

      const promise = service.downloadSubmissionPdf('plugin-1', 'prov-1', 'sub-1', 'my-submission.pdf');
      const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
      req.flush(makeFormEnvelope());
      await promise;

      expect(capturedLink?.download).toBe('my-submission.pdf');
      expect(capturedLink?.href).toContain('blob:mock-url');
      expect(clickSpy).toHaveBeenCalled();
      expect(revokeSpy).toHaveBeenCalledWith('blob:mock-url');
    });

    it('defaults the file name to submission-<id>.pdf when none is provided', async () => {
      const blob = new Blob(['pdf-bytes'], { type: 'application/pdf' });
      spyOn(toInternals(service), 'generatePdf').and.returnValue(Promise.resolve(blob));
      spyOn(URL, 'createObjectURL').and.returnValue('blob:mock-url');
      spyOn(URL, 'revokeObjectURL');
      spyOn(HTMLAnchorElement.prototype, 'click');

      let capturedLink: HTMLAnchorElement | undefined;
      const originalAppendChild = document.body.appendChild.bind(document.body);
      spyOn(document.body, 'appendChild').and.callFake(<T extends Node>(node: T): T => {
        if (node instanceof HTMLAnchorElement) {
          capturedLink = node;
        }
        return originalAppendChild(node);
      });

      const promise = service.downloadSubmissionPdf('plugin-1', 'prov-1', 'sub-1');
      const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
      req.flush(makeFormEnvelope());
      await promise;

      expect(capturedLink?.download).toBe('submission-sub-1.pdf');
    });

    it('propagates the fetch error without attempting to generate a PDF', fakeAsync(() => {
      const generateSpy = spyOn(toInternals(service), 'generatePdf');
      let rejected = false;
      service.downloadSubmissionPdf('plugin-1', 'prov-1', 'sub-1').catch(() => (rejected = true));

      for (let i = 0; i < 3; i++) {
        const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
        req.flush('server error', { status: 500, statusText: 'Internal Server Error' });
        if (i < 2) {
          tick(1000);
        }
      }
      flushMicrotasks();

      expect(rejected).toBeTrue();
      expect(generateSpy).not.toHaveBeenCalled();
    }));
  });
});
