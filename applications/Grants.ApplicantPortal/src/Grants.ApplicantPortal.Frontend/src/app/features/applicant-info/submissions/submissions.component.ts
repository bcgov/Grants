import { Component, OnInit, Input, OnChanges, SimpleChanges, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  SubmissionsData,
} from '../../../shared/models/applicant-info.interface';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { DatatableConfig, DatatableActionEvent, DatatableCellActionEvent } from '../../../shared/components/datatable/datatable.models';
import { LoadingOverlayComponent } from '../../../shared/components/loading-overlay/loading-overlay.component';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { SubmissionPdfService } from '../../../core/services/submission-pdf.service';
import { ToastService } from '../../../shared/services/toast.service';
@Component({
  selector: 'app-applicant-info-submissions',
  standalone: true,
  imports: [DatatableComponent, LoadingOverlayComponent],
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
  isGeneratingPdf = false;

  showRelatedLinksModal = false;
  selectedSubmission: SubmissionsData | null = null;

  // Mobile responsive detection — follows the same matchMedia convention as DatatableComponent.
  isMobile = false;
  private mobileQuery!: MediaQueryList;
  private readonly mobileQueryHandler = (e: MediaQueryListEvent): void => {
    this.isMobile = e.matches;
  };

  // Datatable configuration
  submissionsTableConfig: DatatableConfig = {
    tableId: 'submissions-table',
    defaultSortField: 'receivedTime',
    enableSortPersistence: true,
    columns: [
      { key: 'referenceNo', label: 'Confirmation No', sortable: true, cssClass: 'date-column' },
      { key: 'submissionTime', label: 'Submitted', sortable: true, type: 'date', cssClass: 'submission-date-column' },
      { key: 'type', label: 'Submission', sortable: true, type: 'action-link', cssClass: 'submission-type-column' },
      { key: 'status', label: 'Status', sortable: true, type: 'badge', cssClass: 'status-column' }
    ],
    actionsType: 'dropdown',
    actionItems: [
      { label: 'View Related Links', icon: 'fa-link', iconSrc: 'images/icons/si_link-fill.svg', action: 'viewRelatedLinks' }
    ],
    actionsVisibilityField: 'hasRelatedLinks',
    actionLinkConfig: { ariaLabelField: 'type', ariaLabelPrefix: 'Download PDF for' },
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
    private readonly applicantInfoService: ApplicantInfoService,
    private readonly submissionPdfService: SubmissionPdfService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    if (this.pluginId && this.provider) {
      this.loadSubmissions();
    }

    if (globalThis.window !== undefined) {
      this.mobileQuery = globalThis.matchMedia('(max-width: 768px)');
      this.isMobile = this.mobileQuery.matches;
      this.mobileQuery.addEventListener('change', this.mobileQueryHandler);
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
    this.showRelatedLinksModal = false;
    this.selectedSubmission = null;

    this.applicantInfoService.getSubmissionsInfo(this.pluginId, this.provider)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.linkSource = response.linkSource;
          let submissionsArray = response.submissionsData;
          
          if (!Array.isArray(submissionsArray)) {
            submissionsArray = submissionsArray ? [submissionsArray] : [];
          }
          
          this.submissionsData = submissionsArray.map((submission) => ({
            ...submission,
            hasRelatedLinks: !!(
              submission.renewalLink ||
              (submission.relatedLinks?.length ?? 0) > 0 ||
              submission.applicantMessage
            )
          }));
          this.isLoading = false;
        },
        error: () => {
          this.toastService.error('Failed to load submissions data.');
          this.error = 'Failed to load submissions data';
          this.submissionsData = [];
          this.isLoading = false;
        }
      });
  }

  ngOnDestroy(): void {
    this.mobileQuery?.removeEventListener('change', this.mobileQueryHandler);
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

  async onCellAction(event: DatatableCellActionEvent): Promise<void> {
    if (event.column.key !== 'type') {
      return;
    }
    const submission = event.row as SubmissionsData;
    await this.generateSubmissionPdf(submission);
  }

  private async generateSubmissionPdf(submission: SubmissionsData): Promise<void> {
    if (this.isGeneratingPdf || !this.pluginId || !this.provider) {
      return;
    }

    this.isGeneratingPdf = true;
    try {
      if (this.isMobile) {
        await this.submissionPdfService.downloadSubmissionPdf(this.pluginId, this.provider, submission.id);
      } else {
        await this.submissionPdfService.viewSubmissionPdf(this.pluginId, this.provider, submission.id);
      }
    } catch {
      this.toastService.error('Unable to generate PDF for this submission.');
    } finally {
      this.isGeneratingPdf = false;
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
