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
  OrgbookResponse,
} from '../../shared/models/applicant-info.interface';
import {
  Key,
} from '../../shared/models/application-info.enums';

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
  keySubmissions = Key.SUBMISSIONINFO;
  keyContacts = Key.CONTACTINFO;
  keyAddresses = Key.ADDRESSINFO;

  // Data properties
  applicantInfo: ApplicantInfo | null = null;
  organizationInfo: OrganizationData | null = null;
  orgbookResponse: OrgbookResponse | null = null;

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
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.loadApplicantInfo();
    this.loadOrganizationInfo();
    this.loadOrgbookData(); // Add orgbook data loading
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

  private loadOrgbookData(): void {
    console.log('Loading orgbook data for provider:', this.provider);
    
    // Mock different responses based on provider
    if (this.provider === 'PROGRAM1') {
      this.orgbookResponse = {
        profileId: "019b4788-d7a7-7c40-b25e-98a361adbbfc",
        pluginId: this.pluginId,
        provider: this.provider,
        data: {
          organizations: [
            {
              id: "6CEE6704-16C2-4D8B-8575-57D5F25B40D9",
              orgName: "Cowichan Exhibition",
              organizationType: "Society",
              orgNumber: "S0003748",
              orgStatus: "Active",
              nonRegOrgName: "Shrine Org",
              fiscalMonth: "Aug",
              fiscalDay: 1,
              organizationSize: 50,
              sector: "Agriculture",
              subSector: "Livestock"
            },
            {
              id: "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
              orgName: "",
              organizationType: "",
              orgNumber: "",
              orgStatus: "",
              nonRegOrgName: "",
              fiscalMonth: "",
              fiscalDay: 0,
              organizationSize: null,
              sector: null,
              subSector: null
            }
          ]
        },
        populatedAt: "2026-03-11T17:01:09.1826045Z",
        cacheStatus: "HIT",
        cacheStore: "MemoryDistributedCache"
      };
    } else if (this.provider === 'PROGRAM2') {
      this.orgbookResponse = {
        profileId: "019b4788-d7a7-7c40-b25e-98a361adbbfc",
        pluginId: this.pluginId,
        provider: this.provider,
        data: {
          organizations: [
            {
              id: "7DEF7815-27D3-5E9C-9686-68E6F36C51EA",
              orgName: "Hub Tech Solutions",
              organizationType: "Educational Nonprofit",
              orgNumber: "S1113734",
              orgStatus: "Active",
              nonRegOrgName: "Digital Innovation Org",
              fiscalMonth: "Jul",
              fiscalDay: 23,
              organizationSize: 30,
              sector: "Technology",
              subSector: "Education"
            }
          ]
        },
        populatedAt: "2026-03-11T17:01:09.2131011Z",
        cacheStatus: "HIT",
        cacheStore: "MemoryDistributedCache"
      };
    } else if (this.provider === '3a186f5b-505c-9e2f-18e1-033541752785') {
      // UNITY plugin response
      this.orgbookResponse = {
        profileId: "019b4788-d7a7-7c40-b25e-98a361adbbfc",
        pluginId: this.pluginId,
        provider: this.provider,
        data: {
          organizations: [
            {
              id: "3a1ed607-cd57-5710-2990-f7a210b0b150",
              orgName: null,
              organizationType: null,
              orgNumber: null,
              orgStatus: null,
              nonRegOrgName: null,
              fiscalMonth: null,
              fiscalDay: null,
              organizationSize: null,
              sector: null,
              subSector: null
            },
            {
              id: "3a1eac9f-dac9-827d-45bf-760f3976ead3",
              orgName: "BOBB & VANDERGAAG",
              organizationType: "GP",
              orgNumber: "FM0130995",
              orgStatus: "ACTIVE",
              nonRegOrgName: null,
              fiscalMonth: "Jan",
              fiscalDay: 16,
              organizationSize: null,
              sector: null,
              subSector: null
            },
            {
              id: "3a1f420f-0845-460e-d8b1-208a87196ce4",
              orgName: null,
              organizationType: null,
              orgNumber: null,
              orgStatus: null,
              nonRegOrgName: null,
              fiscalMonth: null,
              fiscalDay: null,
              organizationSize: null,
              sector: null,
              subSector: null
            },
            {
              id: "3a1f55a5-3544-3693-44e4-fa4454a8756d",
              orgName: null,
              organizationType: null,
              orgNumber: null,
              orgStatus: null,
              nonRegOrgName: null,
              fiscalMonth: null,
              fiscalDay: null,
              organizationSize: null,
              sector: null,
              subSector: null
            }
          ]
        },
        populatedAt: "2026-03-11T17:19:28.5728592Z",
        cacheStatus: "HIT",
        cacheStore: "MemoryDistributedCache"
      };
    } else {
      // Default empty response for unknown providers
      this.orgbookResponse = {
        profileId: "019b4788-d7a7-7c40-b25e-98a361adbbfc",
        pluginId: this.pluginId,
        provider: this.provider,
        data: {
          organizations: []
        },
        populatedAt: new Date().toISOString(),
        cacheStatus: "MISS",
        cacheStore: "MemoryDistributedCache"
      };
    }
    
    console.log('Orgbook data loaded for provider', this.provider, ':', this.orgbookResponse);
  }

  // Event handler for contacts component
  onAddContact(): void {
    console.log('Adding new contact...');
    // TODO: Implement add contact logic
  }
}
