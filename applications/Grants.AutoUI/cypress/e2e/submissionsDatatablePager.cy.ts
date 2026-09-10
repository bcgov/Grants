/**
 * Spec stub: Submissions Datatable — Pager
 *
 * Introduced by: AB#32560 — the shared datatable component's pager no longer
 * renders one button per page. It now shows a sliding, centred, clamped
 * 5-number window (e.g. page 13 of 30 shows 11 12 13 14 15; page 1 shows
 * 1..5; the last page shows the final five) plus two new buttons: "first
 * page" (<<) before the existing prev button, and "last page" (>>) after
 * the existing next button.
 *
 * Affected components
 *   shared/components/datatable/datatable.component.html — pager markup,
 *     new datatable-pager-first-{idSuffix} / datatable-pager-last-{idSuffix}
 *     buttons, windowed page-number list
 *   shared/components/datatable/datatable.component.ts   — windowing logic
 *
 * Requirements before implementing
 *   1. A workspace/provider combination with enough submissions to force
 *      multiple pager pages. The DEMO plugin's "Program Three (Large Data
 *      Set)" provider (PROGRAM3) yields 120 submissions, which is enough to
 *      exercise the windowed page-number list (see DemoPlugin.cs /
 *      SubmissionsData.cs on the backend — AutoUI targets deployed
 *      environments, not localhost, so confirm this provider is seeded
 *      there before implementing).
 *   2. Add credentials/provider name to cypress.env.json as needed, e.g.:
 *        "largeDataSetProviderName": "PROGRAM3"
 *
 * TODO: Implement these scenarios. Assign to QA before merging to production.
 */

import { landingPage } from '../pages/LandingPage';

describe('Submissions Datatable — Pager', { testIsolation: false }, () => {
  before(() => {
    // TODO: authenticate (mirror the login flow from loginByBCSCFlow.cy.ts),
    //       select the workspace/provider that yields a large submissions
    //       data set (e.g. PROGRAM3), and land on the Landing page with
    //       landingPage.submissionsTable visible.
  });

  it.skip('shows the first/prev buttons disabled and last/next enabled on page 1', () => {
    // TODO: on page 1, assert submissionsPagerFirst and submissionsPagerPrev
    //       are disabled, and submissionsPagerNext / submissionsPagerLast are
    //       not disabled.
  });

  it.skip('shows a bounded 5-number window centred on a middle page for a large data set', () => {
    // TODO: navigate to a middle page (e.g. page 13 of ~24 for 120 rows) and
    //       assert exactly 5 page-number buttons are present, centred on the
    //       current page (e.g. 11 12 13 14 15). Assert pages outside the
    //       window (e.g. page 1 or page 24) are NOT present in the DOM.
  });

  it.skip('shows the final five page numbers when on the last page', () => {
    // TODO: use submissionsPagerLast to jump to the last page and assert the
    //       page-number window shows the final five pages, ending at the
    //       last page number.
  });

  it.skip('shows the first/prev buttons enabled and last/next disabled on the last page', () => {
    // TODO: on the last page, assert submissionsPagerLast and
    //       submissionsPagerNext are disabled, and submissionsPagerFirst /
    //       submissionsPagerPrev are not disabled.
  });

  it.skip('allows the user to jump directly to the first and last page', () => {
    // TODO: from a middle page, click submissionsPagerFirst and assert page 1
    //       content loads; then click submissionsPagerLast and assert the
    //       last page's content loads.
  });

  // Add further scenarios identified during QA
});
