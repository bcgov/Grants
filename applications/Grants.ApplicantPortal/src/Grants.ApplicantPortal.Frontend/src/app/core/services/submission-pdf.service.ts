import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map, retry } from 'rxjs/operators';
import { SubmissionFormResponse } from '../models/submission-form.model';
import { environment } from '../../../environments/environment';

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
 * Fetches the form.io schema + submission data for a single submission, and opens the
 * `SubmissionPrintComponent` route in a new tab so it can be rendered as real, visible DOM,
 * ready for the applicant to hand to the browser's native print-to-PDF (`window.print()`, via
 * that page's Print button) — matching how the sibling Unity/UGM system already does this for the
 * same kind of form.io data. This replaces a previous pipeline that rendered the form off-screen,
 * rasterized it with html2canvas, and reassembled the raster into a jsPDF document; that approach
 * produced multi-page PDFs that were tens-to-hundreds of MB and took seconds-to-minutes to
 * generate, because it screenshotted the whole form as one giant image and re-embedded that same
 * image once per page. Rendering the real DOM and letting the browser's own print engine (or its
 * "Save as PDF") produce the output is near-instant and vector/text-quality, with no raster size
 * blowup.
 */
@Injectable({
  providedIn: 'root',
})
export class SubmissionPdfService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  /**
   * Fetches the form.io schema + submission data for a single submission. Unchanged from the
   * old rasterization pipeline — `SubmissionPrintComponent` calls this directly itself.
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
   * Opens the submission's print-ready page in a new browser tab, where `SubmissionPrintComponent`
   * fetches the form and renders it as real DOM. Printing itself is deferred to the applicant
   * clicking that page's Print button, which then lets them choose "Save as PDF" (or an actual
   * printer) from the browser's own print dialog. The tab is opened synchronously, before any
   * `await`, so it stays inside the click's user-gesture context — most browsers treat a
   * `window.open` issued after an `await` (i.e. no longer synchronously inside the event handler)
   * as not user-initiated and silently block it as a popup.
   *
   * Rejects only when the popup itself was blocked (`window.open` returns `null`) — that's the one
   * failure mode this method can actually detect synchronously. A failure to fetch/render the
   * submission happens entirely inside the new tab's `SubmissionPrintComponent`, in a separate
   * browsing context this method has no visibility into, so that case surfaces via that tab's own
   * error state, not via this promise.
   */
  viewSubmissionPdf(pluginId: string, provider: string, submissionId: string): Promise<void> {
    const popup = window.open(`/submission-print/${pluginId}/${provider}/${submissionId}`, '_blank');
    if (!popup) {
      return Promise.reject(new Error('Unable to open the print tab — it may have been blocked by a popup blocker.'));
    }
    return Promise.resolve();
  }
}
