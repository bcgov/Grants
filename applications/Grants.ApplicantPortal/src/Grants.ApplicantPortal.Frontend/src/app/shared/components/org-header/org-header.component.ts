import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type OrgHeaderDisplayMode = 'org' | 'applicant';

@Component({
  selector: 'app-org-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './org-header.component.html',
  styleUrls: ['./org-header.component.scss'],
})
export class OrgHeaderComponent {
  @Input() orgNumber: string = '';
  @Input() orgName: string = '';
  @Input() applicantRefId: string = '';
  @Input() applicantName: string = '';
  @Input() hasMultipleOrgs: boolean = false;
  @Input() tenantEmail: string | null = null;
  @Input() displayMode: OrgHeaderDisplayMode = 'applicant';

  get displayId(): string {
    return this.displayMode === 'applicant' ? this.applicantRefId : this.orgNumber;
  }

  get displayName(): string {
    return this.displayMode === 'applicant' ? this.applicantName : this.orgName;
  }
}
