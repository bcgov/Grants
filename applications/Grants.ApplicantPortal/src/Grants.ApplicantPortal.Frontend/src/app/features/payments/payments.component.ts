import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil, filter } from 'rxjs/operators';
import { PaymentData } from '../../shared/models/applicant-info.interface';
import { DatatableComponent } from '../../shared/components/datatable/datatable.component';
import { OrgHeaderComponent } from '../../shared/components/org-header/org-header.component';
import {
  DatatableConfig,
  DatatableSortEvent,
} from '../../shared/components/datatable/datatable.models';
import { ApplicantInfoService } from '../../core/services/applicant-info.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { WorkspaceState } from '../../shared/models/workspace.interface';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule, DatatableComponent, OrgHeaderComponent],
  templateUrl: './payments.component.html',
})
export class PaymentsComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();

  paymentsData: PaymentData[] = [];
  isLoading = true;
  error: string | null = null;

  orgNumber: string = '';
  orgName: string = '';
  hasMultipleOrgs: boolean = false;

  paymentsTableConfig: DatatableConfig = {
    tableId: 'payments-table',
    defaultSortField: 'paymentDate',
    enableSortPersistence: true,
    enableSearch: true,
    searchMinChars: 1,
    searchPlaceholder: 'Search payments...',
    columns: [
      { key: 'paymentNumber', label: 'Payment ID', sortable: true },
      { key: 'referenceNo', label: 'Submission #', sortable: true },
      { key: 'paymentStatus', label: 'Payment Status', sortable: true, type: 'badge', cssClass: 'status-column' },
      { key: 'amount', label: 'Paid Amount', sortable: true, type: 'currency' },
      { key: 'paymentDate', label: 'Paid Date', sortable: true, type: 'date' },
    ],
    actionsType: 'none',
    badgeConfig: {
      field: 'paymentStatus',
      displayField: 'paymentStatus',
      badgeClassPrefix: 'status-badge',
      badgeClasses: {
        'L1Pending': 'status-pending',
        'L1Declined': 'status-declined',
        'L2Pending': 'status-pending',
        'L2Declined': 'status-declined',
        'L3Pending': 'status-pending',
        'L3Declined': 'status-declined',
        'Submitted': 'status-submitted',
        'Validated': 'status-grant-approved',
        'NotValidated': 'status-not-validated',
        'Paid': 'status-paid',
        'Failed': 'status-failed',
        'FSB': 'status-fsb',
      },
      fallbackClass: 'status-inactive',
    },
    noDataMessage: 'No payments were found.',
    loadingMessage: 'Loading your payments...',
    pageSize: 5
  };

  constructor(
    private readonly workspaceService: WorkspaceService,
    private readonly applicantInfoService: ApplicantInfoService
  ) {}

  ngOnInit(): void {
    this.workspaceService.currentWorkspaceState$
      .pipe(
        takeUntil(this.destroy$),
        filter(
          (state: WorkspaceState) =>
            state.selectedWorkspace !== null && state.selectedProvider !== null
        )
      )
      .subscribe((state: WorkspaceState) => {
        if (state.selectedWorkspace && state.selectedProvider) {
          const pluginId = state.selectedWorkspace.pluginId;
          const provider = state.selectedProvider;
          this.loadPayments(pluginId, provider);
          this.loadOrgInfo(pluginId, provider);
        }
      });
  }

  private loadPayments(pluginId: string, provider: string): void {
    this.isLoading = true;
    this.error = null;
    this.paymentsData = [];

    this.applicantInfoService
      .getPaymentsInfo(pluginId, provider)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (Array.isArray(response.paymentsData)) {
            this.paymentsData = response.paymentsData;
          } else if (response.paymentsData) {
            this.paymentsData = [response.paymentsData];
          } else {
            this.paymentsData = [];
          }
          this.isLoading = false;
        },
        error: (error) => {
          console.error('PaymentsComponent - Error loading payments:', error);
          this.error = 'Failed to load payments data';
          this.paymentsData = [];
          this.isLoading = false;
        },
      });
  }

  onPaymentSort(event: DatatableSortEvent): void {
    console.log('Payments sorted by:', event.column, event.direction);
  }

  private loadOrgInfo(pluginId: string, provider: string): void {
    this.applicantInfoService
      .getOrganizationInfo(pluginId, provider)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          const organizations = result.organizationsData ?? [];
          if (organizations.length > 1) {
            this.hasMultipleOrgs = true;
            this.orgNumber = '';
            this.orgName = '';
          } else if (organizations.length === 1) {
            this.hasMultipleOrgs = false;
            this.orgNumber = organizations[0].orgNumber ?? organizations[0].businessNumber ?? '';
            this.orgName = organizations[0].orgName ?? organizations[0].legalName ?? '';
          } else {
            this.hasMultipleOrgs = false;
            this.orgNumber = '';
            this.orgName = '';
          }
        },
        error: () => {
          this.hasMultipleOrgs = false;
          this.orgNumber = '';
          this.orgName = '';
        },
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
