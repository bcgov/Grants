import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil, filter } from 'rxjs/operators';

import { ApplicantService } from '../../core/services/applicant.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { ApplicantInfo } from '../../shared/models/applicant.interface';
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
  
  // Basic applicant data
  applicantInfo: ApplicantInfo | null = null;

  // Cleanup subject
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly applicantService: ApplicantService,
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
          
          // Load basic applicant info
          this.loadApplicantInfo();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadApplicantInfo(): void {
    this.applicantService
      .getApplicantInfo()
      .pipe(takeUntil(this.destroy$))
      .subscribe((data) => (this.applicantInfo = data));
  }
}
