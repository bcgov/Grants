import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, switchMap, takeUntil, timer, retry, catchError, EMPTY } from 'rxjs';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { Plugin, Provider, WorkspaceState } from '../../shared/models/workspace.interface';

@Component({
  selector: 'app-workspace-selector',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './workspace-selector.component.html',
  styleUrls: ['./workspace-selector.component.scss']
})
export class WorkspaceSelectorComponent implements OnInit, OnDestroy {
  availableWorkspaces: Plugin[] = [];
  availableProviders: Provider[] = [];
  selectedWorkspace: Plugin | null = null;
  selectedWorkspaceForProvider: Plugin | null = null;
  selectedDropdownWorkspace: Plugin | null = null;
  selectedDropdownProvider: Provider | null = null;
  isLoading = true;
  isLoadingProviders = false;
  isAutoSelecting = false;
  autoSelectingWorkspace: Plugin | null = null;
  hasError = false;
  isRetrying = false;
  showWorkspaceSelection = false;
  showSelectedWorkspace = false;
  showProviderSelection = false;
  showNoApplicationsMessage = false;
  private returnUrl: string = '/app/applicant-info';
  private destroy$ = new Subject<void>();

  constructor(
    private readonly workspaceService: WorkspaceService,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {}

  private hasFetchedWorkspaces = false;

  ngOnInit(): void {
    // Read return URL from query params
    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
      if (params['returnUrl']) {
        this.returnUrl = this.sanitizeReturnUrl(params['returnUrl']);
      }
    });

    // Subscribe to workspace state changes
    this.workspaceService.currentWorkspaceState$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state: WorkspaceState) => {
        this.availableWorkspaces = state.availableWorkspaces;
        this.selectedWorkspace = state.selectedWorkspace;

        // Fetch workspaces if none are loaded (e.g. navigated back via "Change Workspace")
        if (state.availableWorkspaces.length === 0 && this.isLoading && !this.hasFetchedWorkspaces) {
          this.hasFetchedWorkspaces = true;
          this.fetchWorkspacesWithRetry();
          return;
        }

        this.handleWorkspaceState(state);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private fetchWorkspacesWithRetry(): void {
    this.hasError = false;
    this.isLoading = true;

    this.workspaceService.getAvailableWorkspaces().pipe(
      retry({
        count: 3,
        delay: (_error, retryCount) => timer(retryCount * 3000) // 3s, 6s, 9s
      }),
      takeUntil(this.destroy$),
      catchError(error => {
        console.error('Failed to fetch workspaces after retries:', error);
        this.isLoading = false;
        this.hasError = true;
        return EMPTY;
      })
    ).subscribe();
  }

  retryFetch(): void {
    this.isRetrying = true;
    this.hasError = false;
    this.isLoading = true;

    this.workspaceService.getAvailableWorkspaces().pipe(
      retry({
        count: 2,
        delay: (_error, retryCount) => timer(retryCount * 3000)
      }),
      takeUntil(this.destroy$),
      catchError(error => {
        console.error('Retry failed:', error);
        this.isLoading = false;
        this.hasError = true;
        this.isRetrying = false;
        return EMPTY;
      })
    ).subscribe({
      next: () => {
        this.isRetrying = false;
      }
    });
  }

  private handleWorkspaceState(state: WorkspaceState): void {
    // If workspace and provider already selected, navigate away
    if (state.isWorkspaceSelected && state.isProviderSelected && state.selectedWorkspace) {
      this.router.navigateByUrl(this.returnUrl);
      return;
    }

    // If we have workspaces available
    if (state.availableWorkspaces.length > 0) {
      this.isLoading = false;

      // Auto-select if only one workspace with one provider
      if (state.availableWorkspaces.length === 1) {
        const workspace = state.availableWorkspaces[0];
        if (workspace.providers && workspace.providers.length === 1) {
          this.autoSelectSingleWorkspaceWithProvider(workspace, workspace.providers[0]);
        } else {
          this.autoSelectSingleWorkspace(workspace);
        }
      } else {
        // Show selection UI for multiple workspaces
        this.showWorkspaceSelection = true;
      }
    }

    this.updateSelectionVisibility();
  }

  private updateSelectionVisibility(): void {
    const isWorkspaceSelected = this.workspaceService.isWorkspaceSelected();
    const isProviderSelected = this.workspaceService.isProviderSelected();
    
    this.showSelectedWorkspace = isWorkspaceSelected && isProviderSelected;
    this.showProviderSelection = isWorkspaceSelected && !isProviderSelected && !!this.selectedWorkspaceForProvider;
  }

  private autoSelectSingleWorkspace(workspace: Plugin): void {
    this.isAutoSelecting = true;
    this.autoSelectingWorkspace = workspace;
    
    // Fetch providers from API then decide
    timer(800).pipe(
      takeUntil(this.destroy$),
      switchMap(() => this.workspaceService.getProviders(workspace.pluginId))
    ).subscribe({
      next: (response) => {
        if (response.providers.length === 1) {
          this.selectWorkspaceWithProviderDetails(workspace, response.providers[0]);
        } else if (response.providers.length === 0) {
          this.isAutoSelecting = false;
          this.showNoApplicationsMessage = true;
        } else {
          // Multiple providers - show selection
          this.isAutoSelecting = false;
          this.selectedWorkspaceForProvider = workspace;
          this.availableProviders = response.providers;
          this.showProviderSelection = true;
        }
      },
      error: () => {
        this.isAutoSelecting = false;
        this.selectWorkspace(workspace);
      }
    });
  }

  private autoSelectSingleWorkspaceWithProvider(workspace: Plugin, provider: string): void {
    this.isAutoSelecting = true;
    this.autoSelectingWorkspace = workspace;
    
    timer(800).pipe(
      takeUntil(this.destroy$),
      switchMap(() => this.workspaceService.getProviders(workspace.pluginId))
    ).subscribe({
      next: (response) => {
        if (response.providers.length === 1) {
          this.selectWorkspaceWithProviderDetails(workspace, response.providers[0]);
        } else if (response.providers.length === 0) {
          this.isAutoSelecting = false;
          this.showNoApplicationsMessage = true;
        } else {
          // Multiple providers from API - show selection
          this.isAutoSelecting = false;
          this.selectedWorkspaceForProvider = workspace;
          this.availableProviders = response.providers;
          this.showProviderSelection = true;
        }
      },
      error: () => {
        this.isAutoSelecting = false;
        // Fallback to the static provider string
        this.selectWorkspaceWithProvider(workspace, provider);
      }
    });
  }

  onWorkspaceClick(workspace: Plugin): void {
    // Fetch providers from the API
    this.selectedWorkspaceForProvider = workspace;
    this.showWorkspaceSelection = false;
    this.showProviderSelection = true;
    this.isLoadingProviders = true;
    this.availableProviders = [];
    
    this.workspaceService.getProviders(workspace.pluginId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.isLoadingProviders = false;
          this.availableProviders = response.providers;
          
          if (response.providers.length === 1) {
            // Auto-select single provider
            this.selectWorkspaceWithProviderDetails(workspace, response.providers[0]);
          } else if (response.providers.length === 0) {
            // No providers - show no applications message
            this.showProviderSelection = false;
            this.showNoApplicationsMessage = true;
          }
        },
        error: (error) => {
          this.isLoadingProviders = false;
          console.error('Error fetching providers:', error);
          // Fallback to workspace-only selection
          this.selectWorkspace(workspace);
        }
      });
  }

  selectWorkspace(workspace: Plugin): void {
    this.workspaceService.selectWorkspace(workspace);
    
    // Navigate to the return URL or default
    this.router.navigateByUrl(this.returnUrl);
  }

  selectWorkspaceWithProvider(workspace: Plugin, provider: string): void {
    this.workspaceService.selectWorkspace(workspace, provider);
    
    // Navigate to the return URL or default
    this.router.navigateByUrl(this.returnUrl);
  }

  onDropdownWorkspaceChange(workspace: Plugin): void {
    this.selectedDropdownWorkspace = workspace;
  }

  confirmWorkspaceSelection(): void {
    if (!this.selectedDropdownWorkspace) return;
    this.onWorkspaceClick(this.selectedDropdownWorkspace);
  }

  confirmProviderSelection(): void {
    if (!this.selectedDropdownProvider || !this.selectedWorkspaceForProvider) return;
    this.selectWorkspaceWithProviderDetails(this.selectedWorkspaceForProvider, this.selectedDropdownProvider);
  }

  selectWorkspaceWithProviderDetails(workspace: Plugin, provider: Provider): void {
    this.workspaceService.selectWorkspaceWithProviderDetails(workspace, provider);
    this.router.navigateByUrl(this.returnUrl);
  }

  goBackToWorkspaceSelection(): void {
    this.selectedWorkspaceForProvider = null;
    this.selectedDropdownProvider = null;
    this.availableProviders = [];
    this.showProviderSelection = false;
    this.showWorkspaceSelection = true;
  }

  backToLogin(): void {
    this.authService.logout();
  }

  private sanitizeReturnUrl(url: string): string {
    const DEFAULT_URL = '/app/applicant-info';
    const trimmed = url?.trim();
    if (!trimmed || !trimmed.startsWith('/app/')) {
      return DEFAULT_URL;
    }
    return trimmed;
  }
}