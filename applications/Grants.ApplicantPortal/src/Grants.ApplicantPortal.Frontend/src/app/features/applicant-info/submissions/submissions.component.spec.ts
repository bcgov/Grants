import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';

import { SubmissionsComponent } from './submissions.component';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { SubmissionsData, SubmissionsResponse } from '../../../shared/models/applicant-info.interface';

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

  beforeEach(async () => {
    applicantInfoServiceSpy = jasmine.createSpyObj<ApplicantInfoService>('ApplicantInfoService', [
      'getSubmissionsInfo',
    ]);
    applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(of(makeResponse()));

    await TestBed.configureTestingModule({
      imports: [SubmissionsComponent],
      providers: [{ provide: ApplicantInfoService, useValue: applicantInfoServiceSpy }],
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
    it('renders the submission column as a link with the "Submission" label', () => {
      fixture.detectChanges();
      const submissionColumn = component.submissionsTableConfig.columns.find((c) => c.key === 'type');
      expect(submissionColumn?.label).toBe('Submission');
      expect(submissionColumn?.type).toBe('link');
    });

    it('does not render an actions/chevron column', () => {
      fixture.detectChanges();
      expect(component.submissionsTableConfig.actionsType).toBe('none');
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

    it('wires linkConfig to the CHEFS baseUrl and linkId field when linkSource is present', () => {
      fixture.detectChanges();
      expect(component.submissionsTableConfig.linkConfig).toEqual({
        baseUrl: 'https://chefs.example.com/form/',
        linkField: 'linkId',
      });
    });

    it('does not set linkConfig when linkSource is absent', () => {
      applicantInfoServiceSpy.getSubmissionsInfo.and.returnValue(
        of(makeResponse({ linkSource: undefined }))
      );
      fixture.detectChanges();
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
});
