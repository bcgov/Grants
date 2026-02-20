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
      { key: 'projectName', label: 'Project Name', sortable: true, cssClass: 'project-name-column' },
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
          this.linkSource = response.linkSource;
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
    console.log('Clicked submission:', submission);
    if (this.linkSource && submission.linkId) {
      try {
        const url = new URL(submission.linkId, this.linkSource);
        window.open(url.toString(), '_blank', 'noopener,noreferrer');
      } catch (e) {
        console.error('Invalid submission URL:', {
          linkSource: this.linkSource,
          linkId: submission.linkId,
          error: e
        });
      }
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
