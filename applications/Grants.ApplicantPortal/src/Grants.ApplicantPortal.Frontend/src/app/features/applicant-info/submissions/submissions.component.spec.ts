import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';

import { SubmissionsComponent } from './submissions.component';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { SubmissionPdfService } from '../../../core/services/submission-pdf.service';
import { ToastService } from '../../../shared/services/toast.service';
import { SubmissionsData, SubmissionsResponse } from '../../../shared/models/applicant-info.interface';
import { DatatableActionEvent, DatatableCellActionEvent } from '../../../shared/components/datatable/datatable.models';

// ── helpers ──────────────────────────────────────────────────────────────────

function makeSubmission(overrides: Partial<SubmissionsData> = {}): SubmissionsData {
  return {
    id: '1',
    linkId: 'abc-123',
    receivedTime: '2026-01-01',
    submissionTime: '2026-01-01',
    referenceNo: 'REF-001',
    type: 'Grant Application',
    status: 'Submitted',
    renewalLink: { uri: 'https://example.gov.bc.ca/renewal', title: 'Start Renewal Application', description: '', order: 0 },
    relatedLinks: [
      { uri: 'https://example.gov.bc.ca/guidelines', title: 'Program Guidelines', description: '', order: 0 },
      { uri: 'https://example.gov.bc.ca/faq', title: 'FAQ', description: '', order: 1 },
    ],
    applicantMessage: 'Your report has been reviewed and approved.',
    eligibleForRenewal: true,
    ...overrides,
  };
}

function makeResponse(overrides: Partial<SubmissionsResponse> = {}): SubmissionsResponse {
  return {
    metadata: { pluginId: 'plugin-1', provider: 'provider-1', key: 'key-1', populatedAt: '2026-01-01' },
    submissionsData: [makeSubmission()],
    linkSource: 'https://chefs.example.com/form/',
    ...overrides,
  };
}

// ── suite ─────────────────────────────────────────────────────────────────────

describe('SubmissionsComponent', () => {
  let component: SubmissionsComponent;
  let fixture: ComponentFixture<SubmissionsComponent>;
  let applicantInfoServiceSpy: jasmine.SpyObj<ApplicantInfoService>;
  let submissionPdfServiceSpy: jasmine.SpyObj<SubmissionPdfService>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    applicantInfoServiceSpy = jasmine.createSpyObj<ApplicantInfoService>('ApplicantInfoService', [
      'getSubmissionsInfo',
    ]);
    applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(of(makeResponse()));

    submissionPdfServiceSpy = jasmine.createSpyObj<SubmissionPdfService>('SubmissionPdfService', [
      'viewSubmissionPdf',
      'downloadSubmissionPdf',
    ]);
    submissionPdfServiceSpy.viewSubmissionPdf.and.returnValue(Promise.resolve());
    submissionPdfServiceSpy.downloadSubmissionPdf.and.returnValue(Promise.resolve());

    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'warning', 'info']);

    await TestBed.configureTestingModule({
      imports: [SubmissionsComponent],
      providers: [
        { provide: ApplicantInfoService, useValue: applicantInfoServiceSpy },
        { provide: SubmissionPdfService, useValue: submissionPdfServiceSpy },
        { provide: ToastService, useValue: toastServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(SubmissionsComponent);
    component = fixture.componentInstance;
    component.pluginId = 'plugin-1';
    component.provider = 'provider-1';
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  // ── table configuration (PR feedback regression coverage) ──────────────────
  // The submission title is now the clickable link and the chevron/actions
  // column has been removed — users open the CHEFS form from the link instead.

  describe('submissionsTableConfig', () => {
    it('renders the submission column as an action-link with the "Submission" label', () => {
      fixture.detectChanges();
      const submissionColumn = component.submissionsTableConfig.columns.find((c) => c.key === 'type');
      expect(submissionColumn?.label).toBe('Submission');
      expect(submissionColumn?.type).toBe('action-link');
    });

    it('renders a dropdown actions column with a "View Related Links" action', () => {
      fixture.detectChanges();
      expect(component.submissionsTableConfig.actionsType).toBe('dropdown');
      expect(component.submissionsTableConfig.actionItems).toEqual([
        jasmine.objectContaining({ label: 'View Related Links', action: 'viewRelatedLinks', iconSrc: 'images/icons/si_link-fill.svg' }),
      ]);
    });

    it('hides the actions menu for rows without related link info via actionsVisibilityField', () => {
      fixture.detectChanges();
      expect(component.submissionsTableConfig.actionsVisibilityField).toBe('hasRelatedLinks');
    });
  });

  // ── related links modal ──────────────────────────────────────────────────────

  describe('View Related Links', () => {
    it('opens the modal with the submission from the API response when the action is clicked', () => {
      fixture.detectChanges();
      const submission = component.submissionsData[0];

      component.onSubmissionAction({ action: 'viewRelatedLinks', row: submission, index: 0 } as DatatableActionEvent);

      expect(component.showRelatedLinksModal).toBeTrue();
      expect(component.selectedSubmission).toBe(submission);
      expect(component.selectedSubmission?.renewalLink?.uri).toBe('https://example.gov.bc.ca/renewal');
      expect(component.selectedSubmission?.relatedLinks?.length).toBe(2);
    });

    it('marks hasRelatedLinks true when only a renewal link is present', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        of(makeResponse({
          submissionsData: [makeSubmission({ relatedLinks: [], applicantMessage: null, eligibleForRenewal: false })],
        }))
      );
      fixture.detectChanges();
      expect(component.submissionsData[0].hasRelatedLinks).toBeTrue();
    });

    it('marks hasRelatedLinks true when only related links are present', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        of(makeResponse({
          submissionsData: [makeSubmission({ renewalLink: null, applicantMessage: null, eligibleForRenewal: false })],
        }))
      );
      fixture.detectChanges();
      expect(component.submissionsData[0].hasRelatedLinks).toBeTrue();
    });

    it('marks hasRelatedLinks true when only an applicant message is present', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        of(makeResponse({
          submissionsData: [makeSubmission({ renewalLink: null, relatedLinks: [], eligibleForRenewal: false })],
        }))
      );
      fixture.detectChanges();
      expect(component.submissionsData[0].hasRelatedLinks).toBeTrue();
    });

    it('marks hasRelatedLinks false when only eligibleForRenewal is true and there is nothing to view', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        of(makeResponse({
          submissionsData: [makeSubmission({ renewalLink: null, relatedLinks: [], applicantMessage: null, eligibleForRenewal: true })],
        }))
      );
      fixture.detectChanges();
      expect(component.submissionsData[0].hasRelatedLinks).toBeFalse();
    });

    it('closes the modal and clears the selection', () => {
      fixture.detectChanges();
      component.onViewRelatedLinks(component.submissionsData[0]);

      component.onCloseRelatedLinksModal();

      expect(component.showRelatedLinksModal).toBeFalse();
      expect(component.selectedSubmission).toBeNull();
    });

    it('ignores unknown actions', () => {
      fixture.detectChanges();
      component.onSubmissionAction({ action: 'unknown', row: component.submissionsData[0], index: 0 } as DatatableActionEvent);
      expect(component.showRelatedLinksModal).toBeFalse();
    });

    it('does not open the modal for a submission with no related link info', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        of(makeResponse({
          submissionsData: [
            makeSubmission({ id: '1' }),
            makeSubmission({
              id: '2',
              referenceNo: 'REF-002',
              renewalLink: null,
              relatedLinks: [],
              applicantMessage: null,
              eligibleForRenewal: false,
            }),
          ],
        }))
      );
      fixture.detectChanges();
      const submissionWithoutLinks = component.submissionsData[1];
      expect(submissionWithoutLinks.hasRelatedLinks).toBeFalse();

      component.onSubmissionAction({ action: 'viewRelatedLinks', row: submissionWithoutLinks, index: 1 } as DatatableActionEvent);

      expect(component.showRelatedLinksModal).toBeFalse();
      expect(component.selectedSubmission).toBeNull();
    });
  });

  // ── related links modal content (applicant message / eligible for renewal) ───

  describe('Related Links Modal content', () => {
    it('shows the applicant message when present', () => {
      fixture.detectChanges();
      component.onViewRelatedLinks(component.submissionsData[0]);
      fixture.detectChanges();

      const messageEl = fixture.nativeElement.querySelector('[data-cy="related-links-modal-applicant-message"]');
      expect(messageEl?.textContent).toContain('Your report has been reviewed and approved.');
    });

    it('shows a "—" placeholder when the applicant message is absent', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        of(makeResponse({ submissionsData: [makeSubmission({ applicantMessage: null })] }))
      );
      fixture.detectChanges();
      component.onViewRelatedLinks(component.submissionsData[0]);
      fixture.detectChanges();

      const messageEl = fixture.nativeElement.querySelector('[data-cy="related-links-modal-applicant-message"] p');
      expect(messageEl?.textContent?.trim()).toBe('—');
    });

    it('reflects eligibleForRenewal in the toggle before the renewal link section', () => {
      fixture.detectChanges();
      component.onViewRelatedLinks(component.submissionsData[0]);
      fixture.detectChanges();

      const modalBody = fixture.nativeElement.querySelector('.modal-body');
      const toggle = modalBody.querySelector('[data-cy="related-links-modal-eligible-for-renewal-toggle"]');
      expect(toggle.checked).toBeTrue();
      expect(toggle.disabled).toBeTrue();

      const eligibleSection = modalBody.querySelector('[data-cy="related-links-modal-eligible-for-renewal"]');
      const labels = Array.from(modalBody.querySelectorAll('.form-label')) as HTMLElement[];
      const eligibleIndex = labels.findIndex((l) => l.textContent?.includes('Eligible for Renewal'));
      const renewalIndex = labels.findIndex((l) => l.textContent?.includes('Renewal Link'));
      expect(eligibleSection).toBeTruthy();
      expect(eligibleIndex).toBeGreaterThanOrEqual(0);
      expect(eligibleIndex).toBeLessThan(renewalIndex);
    });

    it('unchecks the toggle when not eligible for renewal', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        of(makeResponse({ submissionsData: [makeSubmission({ eligibleForRenewal: false })] }))
      );
      fixture.detectChanges();
      component.onViewRelatedLinks(component.submissionsData[0]);
      fixture.detectChanges();

      const toggle = fixture.nativeElement.querySelector('[data-cy="related-links-modal-eligible-for-renewal-toggle"]');
      expect(toggle.checked).toBeFalse();
    });
  });

  // ── loadSubmissions ──────────────────────────────────────────────────────────

  describe('loadSubmissions (ngOnInit)', () => {
    it('populates submissionsData from the response', () => {
      fixture.detectChanges();
      expect(component.submissionsData.length).toBe(1);
      expect(component.submissionsData[0].referenceNo).toBe('REF-001');
      expect(component.isLoading).toBeFalse();
    });

    it('captures linkSource from the response without wiring it into the datatable (no longer a navigable link)', () => {
      fixture.detectChanges();
      expect(component.linkSource).toBe('https://chefs.example.com/form/');
      expect(component.submissionsTableConfig.linkConfig).toBeUndefined();
    });

    it('normalizes a single submission object into an array', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        of(makeResponse({ submissionsData: makeSubmission() as any }))
      );
      fixture.detectChanges();
      expect(Array.isArray(component.submissionsData)).toBeTrue();
      expect(component.submissionsData.length).toBe(1);
    });

    it('does not load when pluginId or provider is missing', () => {
      component.pluginId = '';
      fixture.detectChanges();
      expect(applicantInfoServiceSpy.getSubmissionsInfo).not.toHaveBeenCalled();
    });

    it('sets an error and clears data when the service call fails', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        throwError(() => new Error('network error'))
      );
      fixture.detectChanges();
      expect(component.error).toBe('Failed to load submissions data');
      expect(component.submissionsData).toEqual([]);
      expect(component.isLoading).toBeFalse();
    });
  });

  // ── ngOnChanges ──────────────────────────────────────────────────────────────

  describe('ngOnChanges', () => {
    it('reloads when pluginId changes after the first change', () => {
      fixture.detectChanges();
      applicantInfoServiceSpy.getSubmissionsInfo.calls.reset();

      component.pluginId = 'plugin-2';
      component.ngOnChanges({
        pluginId: { currentValue: 'plugin-2', previousValue: 'plugin-1', firstChange: false, isFirstChange: () => false },
      });

      expect(applicantInfoServiceSpy.getSubmissionsInfo).toHaveBeenCalledWith('plugin-2', 'provider-1');
    });

    it('does not reload on the first change', () => {
      component.ngOnChanges({
        pluginId: { currentValue: 'plugin-1', previousValue: undefined, firstChange: true, isFirstChange: () => true },
      });
      expect(applicantInfoServiceSpy.getSubmissionsInfo).not.toHaveBeenCalled();
    });
  });

  // ── getStatusClass ───────────────────────────────────────────────────────────

  describe('getStatusClass', () => {
    it('maps known statuses to their css class', () => {
      expect(component.getStatusClass('In progress')).toBe('status-in-progress');
      expect(component.getStatusClass('Approved')).toBe('status-approved');
      expect(component.getStatusClass('Declined')).toBe('status-declined');
    });

    it('returns an empty string for an unknown status', () => {
      expect(component.getStatusClass('Unknown')).toBe('');
    });
  });

  // ── PDF generation (cellAction) ────────────────────────────────────────────

  describe('onCellAction / generateSubmissionPdf', () => {
    it('ignores cellAction events for columns other than "type"', () => {
      fixture.detectChanges();
      const submission = component.submissionsData[0];

      component.onCellAction({ column: { key: 'status', label: 'Status' }, row: submission } as DatatableCellActionEvent);

      expect(submissionPdfServiceSpy.viewSubmissionPdf).not.toHaveBeenCalled();
      expect(submissionPdfServiceSpy.downloadSubmissionPdf).not.toHaveBeenCalled();
    });

    it('calls viewSubmissionPdf on desktop', async () => {
      fixture.detectChanges();
      component.isMobile = false;
      const submission = component.submissionsData[0];

      await component.onCellAction({ column: { key: 'type', label: 'Submission' }, row: submission } as DatatableCellActionEvent);

      expect(submissionPdfServiceSpy.viewSubmissionPdf).toHaveBeenCalledWith('plugin-1', 'provider-1', submission.id);
      expect(submissionPdfServiceSpy.downloadSubmissionPdf).not.toHaveBeenCalled();
    });

    it('calls downloadSubmissionPdf on mobile', async () => {
      fixture.detectChanges();
      component.isMobile = true;
      const submission = component.submissionsData[0];

      await component.onCellAction({ column: { key: 'type', label: 'Submission' }, row: submission } as DatatableCellActionEvent);

      expect(submissionPdfServiceSpy.downloadSubmissionPdf).toHaveBeenCalledWith('plugin-1', 'provider-1', submission.id);
      expect(submissionPdfServiceSpy.viewSubmissionPdf).not.toHaveBeenCalled();
    });

    it('sets isGeneratingPdf while the PDF is being generated and clears it afterward', async () => {
      let resolveView!: () => void;
      submissionPdfServiceSpy.viewSubmissionPdf.and.returnValue(new Promise((resolve) => (resolveView = resolve)));
      fixture.detectChanges();
      component.isMobile = false;
      const submission = component.submissionsData[0];

      const actionPromise = component.onCellAction({ column: { key: 'type', label: 'Submission' }, row: submission } as DatatableCellActionEvent);
      expect(component.isGeneratingPdf).toBeTrue();

      resolveView();
      await actionPromise;

      expect(component.isGeneratingPdf).toBeFalse();
    });

    it('shows a toast error and clears isGeneratingPdf when the PDF service rejects', async () => {
      // callFake (not returnValue) so the rejected promise is created only once the spy is
      // actually invoked — avoids a spurious unhandled-rejection race in the test harness.
      submissionPdfServiceSpy.viewSubmissionPdf.and.callFake(async () => {
        throw new Error('boom');
      });
      fixture.detectChanges();
      component.isMobile = false;
      const submission = component.submissionsData[0];

      await component.onCellAction({ column: { key: 'type', label: 'Submission' }, row: submission } as DatatableCellActionEvent);

      expect(toastServiceSpy.error).toHaveBeenCalledWith('Unable to generate PDF for this submission.');
      expect(component.isGeneratingPdf).toBeFalse();
    });

    it('does not start a new PDF generation while one is already in flight', () => {
      let resolveView!: () => void;
      submissionPdfServiceSpy.viewSubmissionPdf.and.returnValue(new Promise((resolve) => (resolveView = resolve)));
      fixture.detectChanges();
      component.isMobile = false;
      const submission = component.submissionsData[0];

      component.onCellAction({ column: { key: 'type', label: 'Submission' }, row: submission } as DatatableCellActionEvent);
      component.onCellAction({ column: { key: 'type', label: 'Submission' }, row: submission } as DatatableCellActionEvent);

      expect(submissionPdfServiceSpy.viewSubmissionPdf).toHaveBeenCalledTimes(1);
      resolveView();
    });
  });

  // ── isMobile (matchMedia convention) ────────────────────────────────────────

  describe('isMobile', () => {
    it('initializes from matchMedia on init and updates on change, then cleans up on destroy', () => {
      const listeners: Array<(e: MediaQueryListEvent) => void> = [];
      const mockMediaQueryList = {
        matches: true,
        addEventListener: (_: string, listener: (e: MediaQueryListEvent) => void) => listeners.push(listener),
        removeEventListener: jasmine.createSpy('removeEventListener'),
      } as unknown as MediaQueryList;
      spyOn(globalThis, 'matchMedia').and.returnValue(mockMediaQueryList);

      fixture.detectChanges();
      expect(component.isMobile).toBeTrue();

      listeners[0]({ matches: false } as MediaQueryListEvent);
      expect(component.isMobile).toBeFalse();

      component.ngOnDestroy();
      expect(mockMediaQueryList.removeEventListener).toHaveBeenCalled();
    });
  });
});
