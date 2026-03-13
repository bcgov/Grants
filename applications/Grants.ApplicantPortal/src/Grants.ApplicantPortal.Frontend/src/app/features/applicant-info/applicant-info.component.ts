import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil, filter } from 'rxjs/operators';

import { WorkspaceService } from '../../core/services/workspace.service';
import { WorkspaceState } from '../../shared/models/workspace.interface';
import { OrganizationInfoComponent } from './organization/organization.component';
import { SubmissionsComponent } from './submissions/submissions.component';
import { ContactsComponent } from './contacts/contacts.component';
import { AddressesComponent } from './addresses/addresses.component';

@Component({
  selector: 'app-applicant-info',
  standalone: true,
  imports: [
    CommonModule,
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
  
  // Organization header info
  orgNumber: string = '';
  orgName: string = '';
  hasMultipleOrgs: boolean = false;

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
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onOrganizationLoaded(info: { orgNumber: string; orgName: string } | null): void {
    this.orgNumber = info?.orgNumber ?? '';
    this.orgName = info?.orgName ?? '';
  }

  onMultipleOrganizationsDetected(hasMultiple: boolean): void {
    this.hasMultipleOrgs = hasMultiple;
  }
}
