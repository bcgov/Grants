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
      date: '28/02/2024',
      id: '368GBJ783',
      projectName: 'Your project name here',
      title: 'Community Development Grant',
      submissionDate: new Date('2024-02-28'),
      status: 'In Progress',
    },
    {
      date: '21/01/2024',
      id: '237456DDD',
      projectName: 'Your project name here',
      title: 'Educational Support Initiative',
      submissionDate: new Date('2024-01-21'),
      status: 'Approved',
    },
    {
      date: '22/12/2023',
      id: '16IHND333',
      projectName: 'Your project name here',
      title: 'Infrastructure Enhancement Project',
      submissionDate: new Date('2023-12-22'),
      status: 'Declined',
    },
    {
      date: '22/12/2023',
      id: '16IHND333',
      projectName: 'Your project name here',
      title: 'Infrastructure Enhancement Project',
      submissionDate: new Date('2023-12-22'),
      status: 'Submitted',
    },
    {
      date: '22/12/2023',
      id: '16IHND333',
      projectName: 'Your project name here',
      title: 'Infrastructure Enhancement Project',
      submissionDate: new Date('2023-12-22'),
      status: 'Under Review',
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
      case 'Submitted':
        return 'status-submitted';
      case 'Under Review':
        return 'status-under-review';
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
