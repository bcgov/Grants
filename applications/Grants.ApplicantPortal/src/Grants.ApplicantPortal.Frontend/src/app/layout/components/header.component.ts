import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { UserDropdownComponent } from '../../shared/components/user-dropdown/user-dropdown.component';
import { ApplicantInfo } from '../../shared/models/applicant.interface';
import { AuthService } from '../../core/services/auth.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { Plugin } from '../../shared/models/workspace.interface';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, UserDropdownComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
})
export class HeaderComponent implements OnInit, OnDestroy {
  @Input() applicantInfo: ApplicantInfo | null = null;
  pageTitle = 'Applicant Info';
  selectedWorkspace: Plugin | null = null;
  availableWorkspaces: Plugin[] = [];
  isChangingWorkspace = false;
  private routerSubscription?: Subscription;
  private workspaceSubscription?: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService,
    private workspaceService: WorkspaceService
  ) {}

  ngOnInit(): void {
    // Set initial title
    this.updateTitle();

    // Listen to route changes
    this.routerSubscription = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updateTitle();
      });

    // Subscribe to workspace changes
    this.workspaceSubscription = this.workspaceService.currentWorkspaceState$.subscribe(
      state => {
        this.selectedWorkspace = state.selectedWorkspace;
        this.availableWorkspaces = state.availableWorkspaces;
      }
    );

    // Subscribe to workspace changing state
    this.workspaceService.isChangingWorkspace$.subscribe(
      isChanging => {
        this.isChangingWorkspace = isChanging;
      }
    );
  }

  ngOnDestroy(): void {
    if (this.routerSubscription) {
      this.routerSubscription.unsubscribe();
    }
    if (this.workspaceSubscription) {
      this.workspaceSubscription.unsubscribe();
    }
  }

  private updateTitle(): void {
    const url = this.router.url;

    if (url.includes('applicant-info')) {
      this.pageTitle = 'Applicant Info';
    } else if (url.includes('submissions')) {
      this.pageTitle = 'Submissions';
    } else if (url.includes('payments')) {
      this.pageTitle = 'Payments';
    } else {
      this.pageTitle = 'Applicant Info'; // Default
    }
  }

  onLogout(event: Event): void {
    event.preventDefault();
    console.log('Desktop logout clicked');
    this.authService.logout();
  }

  selectWorkspace(workspace: Plugin): void {
    console.log('HeaderComponent - Workspace selected:', workspace);
    this.workspaceService.selectWorkspace(workspace);
  }

  private clearSession(): void {
    try {
      // Clear sessionStorage
      sessionStorage.clear();
      // Redirect to login page
      this.router.navigate(['/login']);
    } catch (error) {
      console.error('Error clearing session:', error);
      this.router.navigate(['/login']);
    }
  }
}
