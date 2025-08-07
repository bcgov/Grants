import { Component, OnInit, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Submission } from '../../../shared/models/applicant.model';

@Component({
  selector: 'app-submissions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './submissions.component.html',
  styleUrls: ['./submissions.component.scss'],
})
export class SubmissionsComponent implements OnInit {
  @Input() submissions: Submission[] = [];

  // Sample data - replace with actual service call
  mockSubmissions: Submission[] = [
    {
      id: '368GBJ783',
      confirmationNo: '123456789',
      projectName: 'Your project name here',
      submissionDate: new Date('2024-02-28'),
      status: 'In Progress',
      updatedOn: '28/02/2024',
      paidAmount: 5000,
      submissionLink: 'https://example.com/submission/368GBJ783',
    },
    {
      id: '237456DDD',
      confirmationNo: '3453463463',
      projectName: 'Your project name here',
      submissionDate: new Date('2024-01-21'),
      status: 'Approved',
      updatedOn: '28/02/2024',
      paidAmount: 9000,
      submissionLink: 'https://example.com/submission/237456DDD',
    },
    {
      id: '16IHND333',
      confirmationNo: '897856754',
      projectName: 'Infrastructure Enhancement Project',
      submissionDate: new Date('2023-12-22'),
      status: 'Declined',
      updatedOn: '28/06/2025',
      paidAmount: 10000,
      submissionLink: 'https://example.com/submission/16IHND333',
    },
    {
      id: '985789DDD',
      confirmationNo: '887564738',
      projectName: 'Test project name',
      submissionDate: new Date('2024-01-21'),
      status: 'In Progress',
      updatedOn: '07/08/2025',
      paidAmount: 150000,
      submissionLink: 'https://example.com/submission/985789DDD',
    },
    {
      id: '98IHNA444',
      confirmationNo: '173646726',
      projectName: 'Project portal',
      submissionDate: new Date('2023-12-22'),
      status: 'Approved',
      updatedOn: '28/06/2025',
      paidAmount: 20000,
      submissionLink: 'https://example.com/submission/98IHNA444',
    },
  ];

  ngOnInit(): void {
    // If no submissions are passed in, use mock data
    if (this.submissions.length === 0) {
      this.submissions = this.mockSubmissions;
    }
  }

  onSubmissionClick(submission: Submission): void {
    // Handle submission click - navigate to detail view
    console.log('Clicked submission:', submission);
    // Navigate to submission detail page
    // this.router.navigate(['/submissions', submission.id]);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'In Progress':
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
