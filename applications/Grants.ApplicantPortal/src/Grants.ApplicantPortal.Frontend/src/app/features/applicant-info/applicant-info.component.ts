import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, filter } from 'rxjs/operators';

import { ApplicantService } from '../../core/services/applicant.service';
import { ApplicantInfoService } from '../../core/services/applicant-info.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { ApplicantInfo } from '../../shared/models/applicant.interface';
import { WorkspaceState } from '../../shared/models/workspace.interface';
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
  pluginId: string = '';
  provider: string = '';
  keyOrgInfo = Key.ORGINFO;
  keySubmissions = Key.SUBMISSIONS;
  keyContacts = Key.CONTACTS;
  keyAddresses = Key.ADDRESSES;

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
    private readonly applicantInfoService: ApplicantInfoService,
    private readonly workspaceService: WorkspaceService
  ) {}

  ngOnInit(): void {
    // Subscribe to workspace changes and update pluginId and provider
    this.workspaceService.currentWorkspaceState$
      .pipe(
        takeUntil(this.destroy$),
        filter((state: WorkspaceState) => {
          const hasWorkspace = state.selectedWorkspace !== null;
          const hasProvider = state.selectedProvider !== null;
          console.log('ApplicantInfo filter check:', {
            hasWorkspace,
            hasProvider,
            isWorkspaceSelected: state.isWorkspaceSelected,
            isProviderSelected: state.isProviderSelected,
            workspace: state.selectedWorkspace?.pluginId,
            provider: state.selectedProvider
          });
          return hasWorkspace && hasProvider;
        })
      )
      .subscribe((state: WorkspaceState) => {
        if (state.selectedWorkspace && state.selectedProvider) {
          const oldPluginId = this.pluginId;
          const oldProvider = this.provider;
          
          console.log('ApplicantInfoComponent - Workspace and provider changed:', {
            oldPluginId,
            oldProvider,
            newWorkspace: state.selectedWorkspace.pluginId,
            newProvider: state.selectedProvider
          });
          
          this.pluginId = state.selectedWorkspace.pluginId;
          this.provider = state.selectedProvider;
          
          // Only reload data if workspace or provider actually changed
          if (oldPluginId !== this.pluginId || oldProvider !== this.provider) {
            console.log('Data reload triggered due to workspace/provider change');
            this.loadData(); // Reload data with new workspace and provider
          }
        }
      });

    // Initial load
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

    console.log('ApplicantInfoComponent - Loading organization info with:', {
      pluginId: this.pluginId,
      provider: this.provider
    });

    this.applicantInfoService
      .getOrganizationInfo(
        this.pluginId,
        this.provider
      )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.isHydratingOrgInfo = false;
          console.log('Service result received:', result);
          this.organizationInfo = result.organizationData;
          console.log('Organization data assigned to component:', this.organizationInfo);
          console.log('Organization info type:', typeof this.organizationInfo);
          console.log('Organization info keys:', this.organizationInfo ? Object.keys(this.organizationInfo) : 'null');
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
    
    this.applicantInfoService.saveOrganizationInfo(
      organizationData,
      this.pluginId,
      this.provider
    )
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
