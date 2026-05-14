import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';

import { OrganizationInfoComponent } from './organization.component';
import { ENTITY_TYPE_LOOKUP } from '../../../shared/models/orgbook.constants';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ToastService } from '../../../shared/services/toast.service';

// ── helpers ──────────────────────────────────────────────────────────────────

function makeTopic(entityTypeCode: string | undefined) {
  return {
    names: [{ text: 'Acme Corp' }],
    source_id: 'BC0001234',
    inactive: false,
    attributes: entityTypeCode
      ? [{ type: 'entity_type', value: entityTypeCode }]
      : [],
  };
}

function makeSearchResponse(entityTypeCode: string | undefined) {
  return { results: [makeTopic(entityTypeCode)] };
}

// ── suite ─────────────────────────────────────────────────────────────────────

describe('OrganizationInfoComponent', () => {
  let component: OrganizationInfoComponent;
  let fixture: ComponentFixture<OrganizationInfoComponent>;
  let httpMock: HttpTestingController;
  let applicantInfoServiceSpy: jasmine.SpyObj<ApplicantInfoService>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    applicantInfoServiceSpy = jasmine.createSpyObj<ApplicantInfoService>(
      'ApplicantInfoService',
      ['getOrganizationInfo', 'saveOrganizationInfo']
    );
    applicantInfoServiceSpy.getOrganizationInfo.and.returnValue(
      of({ organizationsData: [], organizationData: null })
    );

    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', [
      'success',
      'error',
    ]);

    await TestBed.configureTestingModule({
      imports: [OrganizationInfoComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ApplicantInfoService, useValue: applicantInfoServiceSpy },
        { provide: ToastService, useValue: toastServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(OrganizationInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  // ── creation ────────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── ENTITY_TYPE_LOOKUP constant ─────────────────────────────────────────────

  describe('ENTITY_TYPE_LOOKUP', () => {
    it('maps GP to General Partnership', () => {
      expect(ENTITY_TYPE_LOOKUP['GP']).toBe('General Partnership');
    });

    it('maps BC to BC Company', () => {
      expect(ENTITY_TYPE_LOOKUP['BC']).toBe('BC Company');
    });

    it('maps S to Society', () => {
      expect(ENTITY_TYPE_LOOKUP['S']).toBe('Society');
    });

    it('maps ULC to BC Unlimited Liability Company', () => {
      expect(ENTITY_TYPE_LOOKUP['ULC']).toBe('BC Unlimited Liability Company');
    });

    it('maps XS to Extraprovincial Society', () => {
      expect(ENTITY_TYPE_LOOKUP['XS']).toBe('Extraprovincial Society');
    });

    it('maps BEN to Benefit Company', () => {
      expect(ENTITY_TYPE_LOOKUP['BEN']).toBe('Benefit Company');
    });

    it('maps SP to Sole Proprietorship', () => {
      expect(ENTITY_TYPE_LOOKUP['SP']).toBe('Sole Proprietorship');
    });

    it('has exactly 34 entries', () => {
      expect(Object.keys(ENTITY_TYPE_LOOKUP).length).toBe(34);
    });

    it('returns undefined (not a blank string) for an unknown code at the constant level', () => {
      expect(ENTITY_TYPE_LOOKUP['UNKNOWN']).toBeUndefined();
    });

    it('every defined code resolves to a non-empty string', () => {
      for (const [code, label] of Object.entries(ENTITY_TYPE_LOOKUP)) {
        expect(label.length).toBeGreaterThan(0, `Empty label for code "${code}"`);
      }
    });
  });

  // ── integration: fetchOrgDetails translates entityType code ─────────────────

  describe('onSearchResultSelect / fetchOrgDetails (Orgbook lookup)', () => {
    const searchResult = {
      id: 'BC0001234',
      orgName: 'Acme Corp',
      orgNumber: 'BC0001234',
      orgStatus: '',
      organizationType: '',
    };

    function flushTopicRequest(entityTypeCode: string | undefined): void {
      const req = httpMock.expectOne((r) => r.url.includes('/v4/search/topic'));
      req.flush(makeSearchResponse(entityTypeCode));
    }

    it('sets organizationType to "General Partnership" for code GP', fakeAsync(() => {
      component.onSearchResultSelect(searchResult);
      flushTopicRequest('GP');
      tick();

      expect(component.organizationInfo?.organizationType).toBe('General Partnership');
    }));

    it('sets organizationType to "BC Company" for code BC', fakeAsync(() => {
      component.onSearchResultSelect(searchResult);
      flushTopicRequest('BC');
      tick();

      expect(component.organizationInfo?.organizationType).toBe('BC Company');
    }));

    it('sets organizationType to "Society" for code S', fakeAsync(() => {
      component.onSearchResultSelect(searchResult);
      flushTopicRequest('S');
      tick();

      expect(component.organizationInfo?.organizationType).toBe('Society');
    }));

    it('sets organizationType to empty string for an unknown code', fakeAsync(() => {
      component.onSearchResultSelect(searchResult);
      flushTopicRequest('UNKNOWN');
      tick();

      expect(component.organizationInfo?.organizationType).toBe('');
    }));

    it('sets organizationType to empty string when entity_type attribute is absent', fakeAsync(() => {
      component.onSearchResultSelect(searchResult);
      flushTopicRequest(undefined);
      tick();

      expect(component.organizationInfo?.organizationType).toBe('');
    }));
  });
});
