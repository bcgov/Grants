import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { ApplicantService } from '../../core/services/applicant.service';
import { ApplicantInfoService } from '../../core/services/applicant-info.service';
import { ApplicantInfo } from '../../shared/models/applicant.interface';
import { OrganizationInfoComponent } from './organization/organization.component';
import { SubmissionsComponent } from './submissions/submissions.component';
import { ContactsComponent } from './contacts/contacts.component';
import { AddressesComponent } from './addresses/addresses.component';

import {
  OrganizationData,
  OrgSearchResult,
} from '../../shared/models/applicant-info.interface';
import {
  Key,
  PluginId,
  ProfileId,
  Provider,
} from '../../shared/models/applicantion-info.enums';

@Component({
  selector: 'app-applicant-info',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    OrganizationInfoComponent,
    SubmissionsComponent,
    ContactsComponent,
    AddressesComponent
  ],
  templateUrl: './applicant-info.component.html',
  styleUrls: ['./applicant-info.component.scss'],
})
export class ApplicantInfoComponent implements OnInit, OnDestroy {
  // Profile properties
  profileId = ProfileId.DEFAULT;
  pluginId = PluginId.DEMO;
  provider = Provider.PROGRAM1;
  keyOrgInfo = Key.ORGINFO;
  keySubmissions = Key.SUBMISSIONS;
  keyContacts = Key.CONTACTS;

  // Data properties
  applicantInfo: ApplicantInfo | null = null;
  organizationInfo: OrganizationData | null = null;

  isLoading = true;
  isHydratingOrgInfo = false;
  isSavingOrganization = false;
  saveError: string | null = null;
  saveSuccess = false;

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
      .getOrganizationInfo(
        ProfileId.DEFAULT,
        PluginId.DEMO,
        Provider.PROGRAM1
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
  onSaveOrganization(organizationData: OrganizationData): void {
    console.log('Saving organization...', organizationData);
    
    this.isSavingOrganization = true;
    this.saveError = null;
    this.saveSuccess = false;
    
    this.applicantInfoService.saveOrganizationInfo(organizationData)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isSavingOrganization = false;
        })
      )
      .subscribe({
        next: (response) => {
          console.log('Organization saved successfully:', response);
          this.saveSuccess = true;
          this.organizationInfo = organizationData; // Update local state
          
          // Show success message briefly
          setTimeout(() => {
            this.saveSuccess = false;
          }, 3000);
        },
        error: (error) => {
          console.error('Failed to save organization:', error);
          this.saveError = 'Failed to save organization information. Please try again.';
          
          // Clear error message after 5 seconds
          setTimeout(() => {
            this.saveError = null;
          }, 5000);
        }
      });
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

  // Event handler for contacts component
  onAddContact(): void {
    console.log('Adding new contact...');
    // TODO: Implement add contact logic
  }
}
