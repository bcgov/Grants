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
        'Paid': 'status-grant-approved',
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
        // Read org state from the centralized workspace state
        this.hasMultipleOrgs = state.hasMultipleOrgs;
        this.orgNumber = state.orgNumber;
        this.orgName = state.orgName;

        if (state.selectedWorkspace && state.selectedProvider) {
          const pluginId = state.selectedWorkspace.pluginId;
          const provider = state.selectedProvider;
          this.loadPayments(pluginId, provider);
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
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
