import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { take, takeUntil, Subject } from 'rxjs';
import {
  Submission,
  SubmissionsData,
} from '../../../shared/models/applicant-info.interface';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
@Component({
  selector: 'app-submissions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './submissions.component.html',
  styleUrls: ['./submissions.component.scss'],
})
export class SubmissionsComponent implements OnInit, OnDestroy {
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

  constructor(
    private readonly applicantInfoService: ApplicantInfoService
  ) {}

  ngOnInit(): void {
    if (this.profileId && this.pluginId && this.provider && this.key) {
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
        this.key,
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
}
