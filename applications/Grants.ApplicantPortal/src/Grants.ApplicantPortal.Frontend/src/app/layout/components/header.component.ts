import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { UserDropdownComponent } from '../../shared/components/user-dropdown/user-dropdown.component';
import { ApplicantInfo } from '../../shared/models/applicant.interface';
import { AuthService } from '../../core/services/auth.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { ApiService } from '../../api.service';
import { Plugin, Provider } from '../../shared/models/workspace.interface';
import { filter, switchMap, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';

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
  selectedProvider: string | null = null;
  selectedProviderName: string | null = null;
  availableWorkspaces: Plugin[] = [];
  currentProviders: Provider[] = [];
  isChangingWorkspace = false;
  isLoadingProviders = false;
  private readonly destroy$ = new Subject<void>();
  private readonly fetchProviders$ = new Subject<string>();

  get currentWorkspace(): Plugin | null {
    return this.selectedWorkspace;
  }

  constructor(
    private router: Router,
    private authService: AuthService,
    private readonly workspaceService: WorkspaceService,
    private readonly apiService: ApiService
  ) {}

  get hasMultipleProviders(): boolean {
    return this.currentProviders.length > 1;
  }

  get displayText(): string {
    if (!this.selectedWorkspace) return 'No Workspace';
    if (!this.selectedProviderName || !this.hasMultipleProviders) {
      return this.selectedWorkspace.description;
    }
    return `${this.selectedWorkspace.description} > ${this.selectedProviderName}`;
  }

  ngOnInit(): void {
    // Set initial title
    this.updateTitle();

    // Listen to route changes
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.updateTitle();
      });

    // Cancel any in-flight provider request when a new pluginId is emitted
    this.fetchProviders$
      .pipe(
        switchMap((pluginId) => {
          this.isLoadingProviders = true;
          this.currentProviders = [];
          return this.workspaceService.getProviders(pluginId);
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => {
          this.currentProviders = response.providers;
          this.isLoadingProviders = false;
        },
        error: () => {
          this.currentProviders = [];
          this.isLoadingProviders = false;
        },
      });

    // Subscribe to workspace changes
    this.workspaceService.currentWorkspaceState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(state => {
        const workspaceChanged = state.selectedWorkspace?.pluginId !== this.selectedWorkspace?.pluginId;
        this.selectedWorkspace = state.selectedWorkspace;
        this.selectedProvider = state.selectedProvider;
        this.selectedProviderName = state.selectedProviderName;
        this.availableWorkspaces = state.availableWorkspaces;

        // Fetch providers from API when workspace changes
        if (workspaceChanged && state.selectedWorkspace) {
          this.fetchProviders$.next(state.selectedWorkspace.pluginId);
        }
      });

    // Subscribe to workspace changing state
    this.workspaceService.isChangingWorkspace$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isChanging => {
        this.isChangingWorkspace = isChanging;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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

  selectProviderById(provider: Provider): void {
    if (this.selectedWorkspace && provider.id !== this.selectedProvider) {
      this.isChangingWorkspace = true;
      this.workspaceService.selectWorkspaceWithProviderDetails(this.selectedWorkspace, provider);
      
      setTimeout(() => {
        this.isChangingWorkspace = false;
      }, 500);
    }
  }

  changeWorkspace(): void {
    // Clear selection but keep available workspaces, then redirect to selector
    this.workspaceService.clearSelection();
    this.router.navigate(['/workspace-selector']);
  }

  selectWorkspace(workspace: Plugin): void {
    console.log('HeaderComponent - Workspace selected:', workspace);
    if (workspace.pluginId !== this.selectedWorkspace?.pluginId) {
      this.isChangingWorkspace = true;
      // Provider selection is handled by fetchProviders$ after the workspace state updates
      this.workspaceService.selectWorkspace(workspace);
      
      setTimeout(() => {
        this.isChangingWorkspace = false;
      }, 500);
    }
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

  onClearCache(): void {
    const pluginId = this.selectedWorkspace?.pluginId;
    if (!pluginId || pluginId !== 'DEMO') {
      return;
    }

    this.apiService.clearMyCache(pluginId).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: () => {
        console.log('Cache cleared successfully for:', pluginId);
        setTimeout(() => {
          window.location.reload();
        }, 3000);
      },
      error: (error) => {
        console.error('Failed to clear cache:', error);
      }
    });
  }
}
