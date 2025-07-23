import { Component, OnInit, Inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ApplicantService } from '../../../core/services/applicant.service';
import {
  ApplicantInfo,
  OrganizationInfo,
  ContactInfo,
  AddressInfo,
  Submission,
} from '../../../shared/models/applicant.model';

@Component({
  selector: 'app-applicant-info',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="applicant-info-content">
      <h2>Applicant Info</h2>
      <p>This page will contain applicant info content.</p>
    </div>
  `,
  styles: [
    `
      .applicant-info-content {
        padding: 2rem;
      }
    `,
  ],
})
export class ApplicantInfoComponent implements OnInit {
  applicantInfo: ApplicantInfo | null = null;
  organizationInfo: OrganizationInfo | null = null;
  contactInfo: ContactInfo[] = [];
  addressInfo: AddressInfo[] = [];
  submissions: Submission[] = [];
  isBrowser: boolean;

  constructor(
    private readonly applicantService: ApplicantService,
    @Inject(PLATFORM_ID) private readonly platformId: Object
  ) {
    this.isBrowser = isPlatformBrowser(this.platformId);
  }

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.applicantService.getApplicantInfo().subscribe((data) => {
      this.applicantInfo = data;
    });

    this.applicantService.getOrganizationInfo().subscribe((data) => {
      this.organizationInfo = data;
    });

    this.applicantService.getContactInfo().subscribe((data) => {
      this.contactInfo = data;
    });

    this.applicantService.getAddressInfo().subscribe((data) => {
      this.addressInfo = data;
    });

    this.applicantService.getSubmissions().subscribe((data) => {
      this.submissions = data;
    });
  }

  saveOrganization(): void {
    console.log('Save organization clicked');
  }

  addContact(): void {
    console.log('Add contact clicked');
  }

  addAddress(): void {
    console.log('Add address clicked');
  }

  getStatusSeverity(status: string): string {
    switch (status.toLowerCase()) {
      case 'submitted':
        return 'success';
      case 'under review':
        return 'warning';
      case 'approved':
        return 'info';
      default:
        return 'secondary';
    }
  }
}
