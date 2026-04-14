import { Component, OnInit, Input, OnChanges, SimpleChanges, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  SubmissionsData,
} from '../../../shared/models/applicant-info.interface';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { 
  DatatableConfig,
  DatatableActionEvent,
  DatatableRowClickEvent,
  DatatableSortEvent
} from '../../../shared/components/datatable/datatable.models';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
@Component({
  selector: 'app-applicant-info-submissions',
  standalone: true,
  imports: [CommonModule, DatatableComponent],
  templateUrl: './submissions.component.html',
  styleUrls: ['./submissions.component.scss'],
})
export class SubmissionsComponent implements OnInit, OnChanges, OnDestroy {
  @Input() pluginId!: string;
  @Input() provider!: string;
  @Input() key!: string;

  private destroy$ = new Subject<void>();

  submissionsData: SubmissionsData[] = [];
  linkSource?: string;
  isLoading = true;
  error: string | null = null;

  // Datatable configuration
  submissionsTableConfig: DatatableConfig = {
    tableId: 'submissions-table',
    defaultSortField: 'receivedTime',
    enableSortPersistence: true,
    columns: [
      { key: 'referenceNo', label: 'Confirmation No', sortable: true, cssClass: 'date-column' },
      { key: 'submissionTime', label: 'Submitted', sortable: true, type: 'date', cssClass: 'submission-date-column' },
      { key: 'type', label: 'Submission Title', sortable: true, cssClass: 'submission-type-column' },
      { key: 'status', label: 'Status', sortable: true, type: 'badge', cssClass: 'status-column' },
      { key: 'receivedTime', label: 'Received', sortable: true, type: 'date', cssClass: 'updated-on-column' }
    ],
    actionsType: 'chevron',
    badgeConfig: {
      field: 'status',
      displayField: 'status',
      badgeClassPrefix: 'status-badge',
      badgeClasses: {
        'Submitted': 'status-submitted',
        'Under Review': 'status-under-initial-review',
        'Approved': 'status-grant-approved',
        'Declined': 'status-grant-not-approved',
        'On Hold': 'status-on-hold',
        'Withdrawn': 'status-withdrawn',
        'Closed': 'status-closed'
      },
      fallbackClass: 'status-unknown'
    },

    noDataMessage: 'No submissions were found with your BCeID.',
    loadingMessage: 'Loading your submissions...'
  };

  constructor(
    private applicantInfoService: ApplicantInfoService
  ) {}

  ngOnInit(): void {
    if (this.pluginId && this.provider) {
      this.loadSubmissions();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    const pluginIdChanged = changes['pluginId'] && !changes['pluginId'].firstChange;
    const providerChanged = changes['provider'] && !changes['provider'].firstChange;
    
    if (pluginIdChanged || providerChanged) {
      if (this.pluginId && this.provider) {
        this.loadSubmissions();
      }
    }
  }

  private loadSubmissions(): void {
    this.isLoading = true;
    this.error = null;
    this.submissionsData = [];

    this.applicantInfoService.getSubmissionsInfo(this.pluginId, this.provider)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.linkSource = response.linkSource;
          // Set linkConfig on datatable so chevrons render as <a> tags
          if (this.linkSource) {
            this.submissionsTableConfig = {
              ...this.submissionsTableConfig,
              linkConfig: {
                baseUrl: this.linkSource,
                linkField: 'linkId'
              }
            };
          }
          const submissionsArray = Array.isArray(response.submissionsData) 
            ? response.submissionsData 
            : (response.submissionsData ? [response.submissionsData] : []);
          this.submissionsData = submissionsArray;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('SubmissionsComponent - Error loading submissions:', error);
          this.error = 'Failed to load submissions data';
          this.submissionsData = [];
          this.isLoading = false;
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSubmissionClick(submission: SubmissionsData): void {
    if (this.linkSource && submission.linkId) {
      const url = `${this.linkSource}${submission.linkId}`;
      window.open(url, '_blank', 'noopener,noreferrer');
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'In progress':
        return 'status-in-progress';
      case 'Approved':
        return 'status-approved';
      case 'Declined':
        return 'status-declined';
      default:
        return '';
    }
  }

  sortSubmissions(column: string): void {
    // Implement sorting functionality
    // Example sorting implementation:
    // this.submissions.sort((a, b) => {
    //   const aValue = a[column as keyof Submission];
    //   const bValue = b[column as keyof Submission];
    //   return aValue > bValue ? 1 : -1;
    // });
  }

  // Datatable event handlers
  onSubmissionRowClick(event: DatatableRowClickEvent): void {
    // TODO: Navigate to submission detail view
  }

  onSubmissionAction(event: DatatableActionEvent): void {
    if (event.action === 'view') {
      this.onSubmissionClick(event.row);
    }
  }

  onSubmissionSort(event: DatatableSortEvent): void {
    // The datatable component now handles all sorting internally
    // This event is emitted for any additional logic you might need
  }
}
