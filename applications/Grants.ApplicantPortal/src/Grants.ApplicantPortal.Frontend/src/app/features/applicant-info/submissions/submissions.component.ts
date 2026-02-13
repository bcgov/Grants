import { Component, OnInit, Input, OnChanges, SimpleChanges, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  Submission,
  SubmissionsData,
} from '../../../shared/models/applicant-info.interface';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { 
  DatatableConfig, 
  DatatableColumn,
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
  isLoading = true;
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
    private applicantInfoService: ApplicantInfoService
  ) {}

  ngOnInit(): void {
    console.log('SubmissionsComponent ngOnInit called');
    if (this.pluginId && this.provider) {
      this.loadSubmissions();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    console.log('SubmissionsComponent ngOnChanges called:', changes);
    
    const pluginIdChanged = changes['pluginId'] && !changes['pluginId'].firstChange;
    const providerChanged = changes['provider'] && !changes['provider'].firstChange;
    
    if (pluginIdChanged || providerChanged) {
      console.log('SubmissionsComponent - Input changed, reloading submissions data:', {
        pluginIdChanged,
        providerChanged,
        pluginId: this.pluginId,
        provider: this.provider
      });
      
      if (this.pluginId && this.provider) {
        this.loadSubmissions();
      }
    }
  }

  private loadSubmissions(): void {
    this.isLoading = true;
    this.error = null;
    this.submissionsData = [];

    console.log('SubmissionsComponent - Loading submissions data for:', {
      pluginId: this.pluginId,
      provider: this.provider
    });

    this.applicantInfoService.getSubmissionsInfo(this.pluginId, this.provider)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          console.log('SubmissionsComponent - Received submissions response:', response);
          // Extract submissionsData from the response and convert to array
          const submissionsArray = Array.isArray(response.submissionsData) 
            ? response.submissionsData 
            : (response.submissionsData ? [response.submissionsData] : []);
          this.submissionsData = this.processSubmissionsData(submissionsArray);
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

  private processSubmissionsData(data: SubmissionsData[]): SubmissionsData[] {
    if (!Array.isArray(data)) {
      console.warn('SubmissionsComponent - Invalid data format, expected array:', data);
      return [];
    }

    return data.map(submission => ({
      ...submission,
      // Only convert to Date if the field exists and is valid
      submissionDate: submission.submissionDate ? new Date(submission.submissionDate) : undefined,
      lastModified: submission.lastModified ? new Date(submission.lastModified) : undefined
    }));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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
