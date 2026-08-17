import { Component, OnInit, Input, OnChanges, SimpleChanges, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  SubmissionsData,
} from '../../../shared/models/applicant-info.interface';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { DatatableConfig, DatatableActionEvent } from '../../../shared/components/datatable/datatable.models';
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

  private readonly destroy$ = new Subject<void>();

  submissionsData: SubmissionsData[] = [];
  linkSource?: string;
  isLoading = true;
  error: string | null = null;

  showRelatedLinksModal = false;
  selectedSubmission: SubmissionsData | null = null;

  // Datatable configuration
  submissionsTableConfig: DatatableConfig = {
    tableId: 'submissions-table',
    defaultSortField: 'receivedTime',
    enableSortPersistence: true,
    columns: [
      { key: 'referenceNo', label: 'Confirmation No', sortable: true, cssClass: 'date-column' },
      { key: 'submissionTime', label: 'Submitted', sortable: true, type: 'date', cssClass: 'submission-date-column' },
      { key: 'type', label: 'Submission', sortable: true, type: 'link', cssClass: 'submission-type-column' },
      { key: 'status', label: 'Status', sortable: true, type: 'badge', cssClass: 'status-column' }
    ],
    actionsType: 'dropdown',
    actionItems: [
      { label: 'View Related Links', icon: 'fa-link', iconSrc: 'images/icons/si_link-fill.svg', action: 'viewRelatedLinks' }
    ],
    actionsVisibilityField: 'hasRelatedLinks',
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
    private readonly applicantInfoService: ApplicantInfoService
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
          // Set linkConfig on datatable so the submission column renders as an <a> tag;
          // clear it when there's no linkSource so a stale baseUrl from a prior
          // plugin/provider doesn't leak into this load.
          this.submissionsTableConfig = {
            ...this.submissionsTableConfig,
            linkConfig: this.linkSource
              ? { baseUrl: this.linkSource, linkField: 'linkId' }
              : undefined
          };
          let submissionsArray = response.submissionsData;
          
          if (!Array.isArray(submissionsArray)) {
            submissionsArray = submissionsArray ? [submissionsArray] : [];
          }
          
          this.submissionsData = submissionsArray.map((submission) => ({
            ...submission,
            hasRelatedLinks: !!(
              submission.renewalLink ||
              (submission.relatedLinks?.length ?? 0) > 0 ||
              submission.applicantMessage ||
              submission.eligibleForRenewal
            )
          }));
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

  onSubmissionAction(event: DatatableActionEvent): void {
    const submission = event.row as SubmissionsData;

    if (event.action === 'viewRelatedLinks') {
      this.onViewRelatedLinks(submission);
    }
  }

  onViewRelatedLinks(submission: SubmissionsData): void {
    if (!submission.hasRelatedLinks) {
      return;
    }
    this.selectedSubmission = submission;
    this.showRelatedLinksModal = true;
  }

  onCloseRelatedLinksModal(): void {
    this.showRelatedLinksModal = false;
    this.selectedSubmission = null;
  }
}
