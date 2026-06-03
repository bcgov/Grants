import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError, Subject } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';

import { PaymentsComponent } from './payments.component';
import { WorkspaceService } from '../../core/services/workspace.service';
import { ApplicantInfoService } from '../../core/services/applicant-info.service';
import { WorkspaceState } from '../../shared/models/workspace.interface';
import { PaymentsResponse, PaymentData } from '../../shared/models/applicant-info.interface';

const defaultWorkspaceState: WorkspaceState = {
  selectedWorkspace: null,
  selectedProvider: null,
  selectedProviderName: null,
  availableWorkspaces: [],
  isWorkspaceSelected: false,
  isProviderSelected: false,
  hasMultipleOrgs: false,
  applicantId: null,
  applicantRefId: null,
  applicantName: '',
  orgNumber: '',
  orgName: '',
  tenantEmail: null,
};

const stateWithWorkspace: WorkspaceState = {
  ...defaultWorkspaceState,
  selectedWorkspace: { pluginId: 'plugin-1', description: 'WS', features: [], providers: [] },
  selectedProvider: 'prov-1',
  isWorkspaceSelected: true,
  isProviderSelected: true,
  orgNumber: 'BC1234',
  orgName: 'Test Org',
};

const mockPayments: PaymentData[] = [
  {
    id: 'p1',
    paymentNumber: 'PAY-001',
    referenceNo: 'SUB-001',
    amount: 1000,
    paymentDate: '2024-01-15',
    paymentStatus: 'Fully Paid',
  },
];

describe('PaymentsComponent', () => {
  let component: PaymentsComponent;
  let fixture: ComponentFixture<PaymentsComponent>;
  let workspaceServiceSpy: jasmine.SpyObj<WorkspaceService>;
  let applicantInfoServiceSpy: jasmine.SpyObj<ApplicantInfoService>;
  let workspaceStateSubject: Subject<WorkspaceState>;

  beforeEach(async () => {
    workspaceStateSubject = new Subject<WorkspaceState>();

    workspaceServiceSpy = jasmine.createSpyObj<WorkspaceService>('WorkspaceService', [], {
      currentWorkspaceState$: workspaceStateSubject.asObservable(),
    });

    applicantInfoServiceSpy = jasmine.createSpyObj<ApplicantInfoService>(
      'ApplicantInfoService',
      ['getPaymentsInfo']
    );
    applicantInfoServiceSpy.getPaymentsInfo.and.returnValue(of({ paymentsData: [] }));

    await TestBed.configureTestingModule({
      imports: [PaymentsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
        { provide: ApplicantInfoService, useValue: applicantInfoServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(PaymentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('starts with empty paymentsData', () => {
    expect(component.paymentsData).toEqual([]);
  });

  it('starts with null error', () => {
    expect(component.error).toBeNull();
  });

  describe('when workspace and provider are selected', () => {
    it('loads payments on workspace state emission', () => {
      const response: PaymentsResponse = { paymentsData: mockPayments };
      applicantInfoServiceSpy.getPaymentsInfo.and.returnValue(of(response));

      workspaceStateSubject.next(stateWithWorkspace);

      expect(applicantInfoServiceSpy.getPaymentsInfo).toHaveBeenCalledWith('plugin-1', 'prov-1');
      expect(component.paymentsData).toEqual(mockPayments);
      expect(component.isLoading).toBeFalse();
    });

    it('sets error when getPaymentsInfo fails', () => {
      applicantInfoServiceSpy.getPaymentsInfo.and.returnValue(
        throwError(() => new Error('Network error'))
      );

      workspaceStateSubject.next(stateWithWorkspace);

      expect(component.error).toBe('Failed to load payments data');
      expect(component.isLoading).toBeFalse();
    });

    it('reads org state from workspace state', () => {
      applicantInfoServiceSpy.getPaymentsInfo.and.returnValue(of({ paymentsData: [] }));

      workspaceStateSubject.next(stateWithWorkspace);

      expect(component.orgNumber).toBe('BC1234');
      expect(component.orgName).toBe('Test Org');
    });
  });

  describe('when no workspace is selected', () => {
    it('does not call getPaymentsInfo when state has no workspace', () => {
      workspaceStateSubject.next(defaultWorkspaceState);
      expect(applicantInfoServiceSpy.getPaymentsInfo).not.toHaveBeenCalled();
    });
  });

  describe('paymentsTableConfig', () => {
    it('has the correct tableId', () => {
      expect(component.paymentsTableConfig.tableId).toBe('payments-table');
    });

    it('has columns defined', () => {
      expect(component.paymentsTableConfig.columns.length).toBeGreaterThan(0);
    });
  });

  it('cleans up on destroy', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
