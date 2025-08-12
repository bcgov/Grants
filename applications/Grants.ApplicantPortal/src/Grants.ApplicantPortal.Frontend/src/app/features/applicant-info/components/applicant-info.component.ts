import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { ApplicantService } from '../../../core/services/applicant.service';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ApplicantInfo } from '../../../shared/models/applicant.interface';
import { OrganizationInfoComponent } from './organization.component';
import { SubmissionsComponent } from './submissions.component';

import {
  OrganizationData,
  OrgSearchResult,
} from '../../../shared/models/applicant-info.interface';
import {
  Key,
  PluginId,
  ProfileId,
  Provider,
} from '../../../shared/models/applicantion-info.enums';

@Component({
  selector: 'app-applicant-info',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    OrganizationInfoComponent,
    SubmissionsComponent,
  ],
  templateUrl: './applicant-info.component.html',
  styleUrls: ['./applicant-info.component.scss'],
})
export class ApplicantInfoComponent implements OnInit, OnDestroy {
  // Profile properties
  profileId = ProfileId.DEFAULT;
  pluginId = PluginId.DEMO;
  provider = Provider.DEMO;
  keyOrgInfo = Key.ORGINFO;
  keySubmissions = Key.SUBMISSIONS;

  // Data properties
  applicantInfo: ApplicantInfo | null = null;
  organizationInfo: OrganizationData | null = null;

  isLoading = true;
  isHydratingOrgInfo = false;

  // Subjects for cleanup
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly applicantService: ApplicantService,
    private readonly applicantInfoService: ApplicantInfoService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.loadApplicantInfo();
    this.loadOrganizationInfo();
  }

  private loadApplicantInfo(): void {
    this.applicantService
      .getApplicantInfo()
      .pipe(takeUntil(this.destroy$))
      .subscribe((data) => (this.applicantInfo = data));
  }

  private loadOrganizationInfo(): void {
    this.isHydratingOrgInfo = true;

    this.applicantInfoService
      .hydrateAndGetOrganizationInfo(
        ProfileId.DEFAULT,
        PluginId.DEMO,
        Provider.DEMO,
        Key.ORGINFO,
        {}
      )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.isHydratingOrgInfo = false;
          this.organizationInfo = result.organizationData;
          console.log('Organization data loaded:', this.organizationInfo);
          this.isLoading = false;
        },
        error: (error) => {
          this.isHydratingOrgInfo = false;
          console.error('Hydration failed:', error);
        },
      });
  }

  // Event handlers for organization info component
  onSaveOrganization(): void {
    console.log('Saving organization...', this.organizationInfo);
    // Implement save logic
  }

  onSearchResultSelected(result: OrgSearchResult): void {
    this.updateOrganizationFromSearch(result);
  }

  private updateOrganizationFromSearch(result: OrgSearchResult): void {
    if (!this.organizationInfo) return;

    this.organizationInfo = {
      ...this.organizationInfo,
      orgName: result.orgName,
      orgNumber: result.orgNumber,
      orgStatus: result.orgStatus,
      organizationType: result.organizationType,
    };
  }
}
