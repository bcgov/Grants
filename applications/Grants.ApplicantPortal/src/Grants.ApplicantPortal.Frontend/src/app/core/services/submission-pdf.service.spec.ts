import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { SubmissionPdfService } from './submission-pdf.service';
import { environment } from '../../../environments/environment';

const BASE = environment.apiUrl;

/**
 * The REAL wire shape of `GET /Submissions/:pluginId/:provider/:submissionId/Form` —
 * a project-wide plugin-data envelope whose own `data` field is itself the
 * `{ schema, data }` submission form payload (double-nested). Mirrors
 * `RetrieveSubmissionFormResponse` on the backend.
 */
function makeFormEnvelope(): Record<string, unknown> {
  return {
    profileId: 'profile-1',
    pluginId: 'plugin-1',
    provider: 'prov-1',
    submissionId: 'sub-1',
    data: { schema: { display: 'form', components: [] }, data: { field1: 'value1' } },
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
      const envelope = {
        ...makeFormEnvelope(),
        data: { schema: { display: 'form' }, data: { name: 'Org A' } },
      };

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

  // Rendering (real DOM/formio) and printing now happen entirely in `SubmissionPrintComponent`,
  // opened as its own tab/route — see submission-print.component.spec.ts for that coverage
  // (route param handling, schema patching, submission-data application, and that window.print()
  // fires once rendering settles). This service's remaining responsibility is just opening that
  // route in a new tab.

  describe('viewSubmissionPdf', () => {
    it('opens the submission print route in a new tab, synchronously, so it stays inside the click gesture', async () => {
      const openSpy = spyOn(window, 'open').and.returnValue({} as Window);

      await service.viewSubmissionPdf('plugin-1', 'prov-1', 'sub-1');

      expect(openSpy).toHaveBeenCalledWith('/submission-print/plugin-1/prov-1/sub-1', '_blank');
      expect(openSpy).toHaveBeenCalledTimes(1);
    });

    it('does not touch the HTTP client at all — fetching the form is SubmissionPrintComponent’s job now', async () => {
      spyOn(window, 'open').and.returnValue({} as Window);

      await service.viewSubmissionPdf('plugin-1', 'prov-1', 'sub-1');

      httpMock.expectNone(`${BASE}/Submissions/plugin-1/prov-1/sub-1/Form`);
    });

    it('rejects when the popup is blocked (window.open returns null)', async () => {
      spyOn(window, 'open').and.returnValue(null);

      await expectAsync(service.viewSubmissionPdf('plugin-1', 'prov-1', 'sub-1')).toBeRejected();
    });
  });
});
