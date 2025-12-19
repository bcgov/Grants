import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { take, takeUntil, Subject } from 'rxjs';
import {
  Submission,
  SubmissionsData,
} from '../../../shared/models/applicant-info.interface';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { 
  DatatableConfig, 
  DatatableColumn,
  DatatableActionEvent,
  DatatableRowClickEvent,
  DatatableSortEvent
} from '../../../shared/components/datatable/datatable.models';
@Component({
  selector: 'app-applicant-info-submissions',
  standalone: true,
  imports: [CommonModule, DatatableComponent],
  templateUrl: './submissions.component.html',
  styleUrls: ['./submissions.component.scss'],
})
export class SubmissionsComponent implements OnInit, OnDestroy, OnChanges {
  // @Input() submissions: Submission[] = [];
  @Input() profileId!: string;
  @Input() pluginId!: string;
  @Input() provider!: string;
  @Input() key!: string;

  private readonly destroy$ = new Subject<void>();

  @Input() data?: any;

  submissionsInfo: SubmissionsData[] = [];
  isLoading = true;
  isHydratingSubmissionsInfo = false;
  error: string | null = null;

  // Datatable configuration
  submissionsTableConfig: DatatableConfig = {
    tableId: 'submissions-table',
    defaultSortField: 'lastModified',
    enableSortPersistence: true,
    columns: [
      { key: 'submissionId', label: 'Confirmation No', sortable: true, cssClass: 'date-column' },
      { key: 'submissionDate', label: 'Date', sortable: true, type: 'date', cssClass: 'submission-date-column' },
      { key: 'projectName', label: 'Project Name', sortable: true, cssClass: 'project-name-column' },
      { key: 'status', label: 'Status', sortable: true, type: 'badge', cssClass: 'status-column' },
      { key: 'lastModified', label: 'Updated On', sortable: true, type: 'date', cssClass: 'updated-on-column' },
      { key: 'paidAmount', label: 'Paid Amount', sortable: true, type: 'currency', cssClass: 'paid-amount-column' }
    ],
    actionsType: 'chevron',
    badgeConfig: {
      field: 'statusCode', // Field used for styling
      displayField: 'status', // Field displayed as text
      badgeClassPrefix: 'status-badge',
      badgeClasses: {
        'ASSIGNED': 'status-assigned',
        'WITHDRAWN': 'status-withdrawn',
        'CLOSED': 'status-closed',
        'UNDER_INITIAL_REVIEW': 'status-under-initial-review',
        'INITIAL_REVIEW_COMPLETED': 'status-initial-review-completed',
        'ON_HOLD': 'status-on-hold',
        'DEFER': 'status-defer',
        'ASSESSMENT_COMPLETED': 'status-assessment-completed',
        'GRANT_APPROVED': 'status-grant-approved',
        'UNDER_ASSESSMENT': 'status-under-assessment',
        'SUBMITTED': 'status-submitted',
        'GRANT_NOT_APPROVED': 'status-grant-not-approved'
      },
      fallbackClass: 'status-unknown'
    },

    noDataMessage: 'No submissions were found with your BCeID.',
    loadingMessage: 'Loading your submissions...'
  };

  constructor(
    private readonly applicantInfoService: ApplicantInfoService
  ) {}

  ngOnInit(): void {
    if (this.profileId && this.pluginId && this.provider) {
      this.loadSubmissions();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Reload data when pluginId changes (workspace switch)
    if (changes['pluginId'] && !changes['pluginId'].firstChange) {
      console.log('SubmissionsComponent - Plugin ID changed, reloading data');
      this.loadSubmissions();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadSubmissions(): void {
    this.isHydratingSubmissionsInfo = true;
    this.error = null;

    this.applicantInfoService
      .getSubmissionsInfo(
        this.profileId,
        this.pluginId,
        this.provider,
        this.data
      )
      .pipe(take(1), takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.isHydratingSubmissionsInfo = false;
          this.submissionsInfo = Array.isArray(result.submissionsData)
            ? result.submissionsData
            : [result.submissionsData];

          console.log('Submissions data loaded:', this.submissionsInfo);
          this.isLoading = false;
        },
        error: (error) => {
          this.isHydratingSubmissionsInfo = false;
          this.error = 'Failed to load submissions data';
          console.error('Error loading submissions:', error);
        },
      });
  }

  onSubmissionClick(submission: Submission): void {
    // Handle submission click - navigate to detail view
    console.log('Clicked submission:', submission);
    // Navigate to submission detail page
    // this.router.navigate(['/submissions', submission.id]);
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
    console.log('Sort by:', column);
    // Example sorting implementation:
    // this.submissions.sort((a, b) => {
    //   const aValue = a[column as keyof Submission];
    //   const bValue = b[column as keyof Submission];
    //   return aValue > bValue ? 1 : -1;
    // });
  }

  // Datatable event handlers
  onSubmissionRowClick(event: DatatableRowClickEvent): void {
    console.log('Clicked submission:', event.row);
    // TODO: Navigate to submission detail view
  }

  onSubmissionAction(event: DatatableActionEvent): void {
    if (event.action === 'view') {
      this.onSubmissionClick(event.row);
    }
  }

  onSubmissionSort(event: DatatableSortEvent): void {
    console.log('Submissions sorted by:', event.column, event.direction);
    // The datatable component now handles all sorting internally
    // This event is emitted for any additional logic you might need
  }
}
