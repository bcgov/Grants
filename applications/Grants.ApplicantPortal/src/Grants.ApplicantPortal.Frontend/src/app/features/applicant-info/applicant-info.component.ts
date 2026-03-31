import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil, filter } from 'rxjs/operators';

import { WorkspaceService } from '../../core/services/workspace.service';
import { WorkspaceState } from '../../shared/models/workspace.interface';
import { OrgHeaderComponent } from '../../shared/components/org-header/org-header.component';
import { OrganizationInfoComponent } from './organization/organization.component';
import { SubmissionsComponent } from './submissions/submissions.component';
import { ContactsComponent } from './contacts/contacts.component';
import { AddressesComponent } from './addresses/addresses.component';

@Component({
  selector: 'app-applicant-info',
  standalone: true,
  imports: [
    CommonModule,
    OrgHeaderComponent,
    OrganizationInfoComponent,
    SubmissionsComponent,
    ContactsComponent,
    AddressesComponent
  ],
  templateUrl: './applicant-info.component.html',
  styleUrls: ['./applicant-info.component.scss'],
})
export class ApplicantInfoComponent implements OnInit, OnDestroy {
  // Workspace properties
  pluginId: string = '';
  provider: string = '';
  
  // Organization header info (driven by global workspace state)
  orgNumber: string = '';
  orgName: string = '';
  hasMultipleOrgs: boolean = false;
  applicantId: string | null = null;

  // Cleanup subject
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly workspaceService: WorkspaceService
  ) {}

  ngOnInit(): void {
    // Subscribe to workspace changes to provide pluginId and provider to child components
    this.workspaceService.currentWorkspaceState$
      .pipe(
        takeUntil(this.destroy$),
        filter((state: WorkspaceState) => {
          const hasWorkspace = state.selectedWorkspace !== null;
          const hasProvider = state.selectedProvider !== null;
          return hasWorkspace && hasProvider;
        })
      )
      .subscribe((state: WorkspaceState) => {
        if (state.selectedWorkspace && state.selectedProvider) {
          this.pluginId = state.selectedWorkspace.pluginId;
          this.provider = state.selectedProvider;

          // Read org state from centralized workspace state
          this.hasMultipleOrgs = state.hasMultipleOrgs;
          this.applicantId = state.applicantId;
          this.orgNumber = state.orgNumber;
          this.orgName = state.orgName;
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Still accept org events from the organization child for backward compat / initial load */
  onOrganizationLoaded(info: { orgNumber: string; orgName: string } | null): void {
    if (info) {
      this.orgNumber = info.orgNumber;
      this.orgName = info.orgName;
    } else {
      this.orgNumber = '';
      this.orgName = '';
    }
  }

  onMultipleOrganizationsDetected(hasMultiple: boolean): void {
    this.hasMultipleOrgs = hasMultiple;
  }
}
