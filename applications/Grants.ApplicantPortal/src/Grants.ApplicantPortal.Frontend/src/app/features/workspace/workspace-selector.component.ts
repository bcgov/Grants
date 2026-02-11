import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil, timer } from 'rxjs';
import { WorkspaceService } from '../../core/services/workspace.service';
import { Plugin, Provider, WorkspaceState } from '../../shared/models/workspace.interface';

@Component({
  selector: 'app-workspace-selector',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="selector-page">
      <div class="selector-background">
        <div class="bc-logo-header">
          <img
            src="images/logo/BCID_H_rgb_pos.png"
            alt="British Columbia Government"
            class="bc-header-logo"
          />
        </div>

        <!-- Card -->
        <div class="selector-card-container">
          <div class="selector-card">
            <div class="selector-header">
              <h1>Enterprise Grant</h1>
              <h2>Management System Portal</h2>
            </div>

            <div class="mobile-preview-image">
              <img
                src="images/dashboard-preview.png"
                alt="Dashboard Preview"
                class="dashboard-preview"
              />
            </div>

            <!-- Loading State -->
            <div *ngIf="isLoading || isAutoSelecting" class="selector-loading">
              <div class="spinner"></div>
              <p class="loading-text" *ngIf="isLoading">Setting up your workspace...</p>
              <p class="loading-text" *ngIf="isAutoSelecting">Accessing {{ autoSelectingWorkspace?.description }}...</p>
            </div>

            <!-- Workspace Selection -->
            <div *ngIf="showWorkspaceSelection && !isLoading && !isAutoSelecting">
              <div class="selector-description">
                <p>Please select a workspace to continue.</p>
              </div>

              <div class="dropdown-group">
                <label for="workspaceSelect" class="dropdown-label">Workspace</label>
                <select
                  id="workspaceSelect"
                  class="dropdown-select"
                  [(ngModel)]="selectedDropdownWorkspace"
                  (ngModelChange)="onDropdownWorkspaceChange($event)"
                >
                  <option [ngValue]="null" disabled>-- Select a workspace --</option>
                  <option *ngFor="let workspace of availableWorkspaces" [ngValue]="workspace">
                    {{ workspace.description }}
                  </option>
                </select>
              </div>

              <button
                type="button"
                class="btn btn-primary selector-btn"
                [disabled]="!selectedDropdownWorkspace"
                (click)="confirmWorkspaceSelection()"
              >
                Continue
              </button>
            </div>

            <!-- Program Selection -->
            <div *ngIf="showProviderSelection && selectedWorkspaceForProvider && !isLoading && !isAutoSelecting">
              <div *ngIf="isLoadingProviders" class="selector-loading">
                <div class="spinner"></div>
                <p class="loading-text">Loading providers...</p>
              </div>

              <div *ngIf="!isLoadingProviders">
                <div class="selector-description">
                  <p>Select a grant program for <strong>{{ selectedWorkspaceForProvider.description }}</strong>.</p>
                </div>

                <div class="dropdown-group">
                  <label for="providerSelect" class="dropdown-label">Provider</label>
                  <select
                    id="providerSelect"
                    class="dropdown-select"
                    [(ngModel)]="selectedDropdownProvider"
                  >
                    <option [ngValue]="null" disabled>-- Select a program --</option>
                    <option *ngFor="let provider of availableProviders" [ngValue]="provider">
                      {{ provider.name }}
                    </option>
                  </select>
                </div>

                <button
                  type="button"
                  class="btn btn-primary selector-btn"
                  [disabled]="!selectedDropdownProvider"
                  (click)="confirmProviderSelection()"
                >
                  Continue
                </button>

                <button
                  type="button"
                  class="btn-back"
                  (click)="goBackToWorkspaceSelection()"
                >
                  &larr; Back to Workspaces
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Right side preview -->
      <div class="preview-section">
        <div class="bc-logo-corner">
          <img
            src="images/logo/BCID_H_rgb_pos.png"
            alt="British Columbia"
            class="bc-corner-logo"
          />
        </div>

        <div class="preview-image">
          <img
            src="images/dashboard-preview.png"
            alt="Dashboard Preview"
            class="dashboard-preview"
          />
        </div>

        <div class="footer-link">
          <a href="https://grants.gov.bc.ca" target="_blank">grants.gov.bc.ca</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    /* ===== Page Layout (mirrors login) ===== */
    .selector-page {
      display: flex;
      min-height: 100vh;
      width: 100%;
    }

    .selector-background {
      flex: 1;
      background: var(--bc-bg-info);
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      padding: 2rem;
      position: relative;
    }

    .bc-logo-header {
      display: none;
    }

    /* ===== Card ===== */
    .selector-card-container {
      display: flex;
      justify-content: center;
      align-items: center;
      width: 100%;
      max-width: 500px;
    }

    .selector-card {
      background: var(--bc-white);
      border-radius: 8px;
      padding: 3rem 2.5rem;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
      text-align: center;
      width: 100%;
    }

    .selector-header {
      margin-bottom: 1.5rem;

      h1 {
        color: var(--bc-bg-info);
        font-size: 1.75rem;
        font-weight: 700;
        margin: 0 0 0.25rem 0;
        line-height: 1.2;
      }

      h2 {
        color: var(--bc-primary);
        font-size: 1.1rem;
        font-weight: 400;
        margin: 0;
        line-height: 1.3;
      }
    }

    .selector-description {
      margin-bottom: 1.5rem;

      p {
        color: var(--bc-primary);
        font-size: var(--bc-font-size-14);
        line-height: 1.5;
        margin: 0;
      }
    }

    /* ===== Loading ===== */
    .selector-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 1.5rem 0;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 3px solid var(--bc-gray-20);
      border-top: 3px solid var(--bc-blue);
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 1rem;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .loading-text {
      color: var(--bc-primary);
      font-size: var(--bc-font-size-14);
      margin: 0;
    }

    /* ===== Dropdown ===== */
    .dropdown-group {
      text-align: left;
      margin-bottom: 1.5rem;
    }

    .dropdown-label {
      display: block;
      font-size: var(--bc-font-size-14);
      font-weight: 600;
      color: var(--bc-primary);
      margin-bottom: 0.5rem;
    }

    .dropdown-select {
      width: 100%;
      padding: 0.75rem 1rem;
      font-size: var(--bc-font-size-14);
      color: var(--bc-primary);
      background-color: var(--bc-white);
      border: 2px solid var(--bc-gray-50, #606060);
      border-radius: 4px;
      appearance: none;
      -webkit-appearance: none;
      -moz-appearance: none;
      background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23333' d='M6 8.825L0.375 3.2l0.85-0.85L6 7.125l4.775-4.775 0.85 0.85z'/%3E%3C/svg%3E");
      background-repeat: no-repeat;
      background-position: right 1rem center;
      cursor: pointer;
      transition: border-color 0.2s ease, box-shadow 0.2s ease;

      &:focus {
        outline: none;
        border-color: var(--bc-blue);
        box-shadow: 0 0 0 3px rgba(0, 51, 102, 0.15);
      }

      &:hover {
        border-color: var(--bc-blue);
      }
    }

    /* ===== Buttons ===== */
    .selector-btn {
      padding: 10px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      width: 100%;

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .btn-back {
      display: block;
      width: 100%;
      margin-top: 0.75rem;
      padding: 0.5rem;
      background: none;
      border: none;
      color: var(--bc-primary);
      font-size: var(--bc-font-size-14);
      font-weight: 500;
      cursor: pointer;
      transition: color 0.2s ease;

      &:hover {
        color: var(--bc-blue);
        text-decoration: underline;
      }
    }

    /* ===== Right Preview (mirrors login) ===== */
    .preview-section {
      flex: 1;
      background: #f8f9fa;
      display: flex;
      flex-direction: column;
      position: relative;
      padding: 2rem;
    }

    .bc-logo-corner {
      display: flex;
      justify-content: center;
      align-items: center;

      .bc-corner-logo {
        height: 120px;
        width: auto;
      }
    }

    .preview-image {
      flex: 1;
      display: flex;
      justify-content: center;
      align-items: center;

      .dashboard-preview {
        max-width: 100%;
        height: auto;
      }
    }

    .mobile-preview-image {
      display: none;
    }

    .footer-link {
      display: flex;
      justify-content: center;
      align-items: center;

      a {
        color: var(--bc-bg-info);
        text-decoration: none;
        font-size: 0.9rem;
        font-weight: 500;

        &:hover {
          text-decoration: underline;
        }
      }
    }

    /* ===== Responsive (mirrors login) ===== */
    @media (max-width: 768px) {
      .preview-section {
        display: none;
      }

      .selector-page {
        flex-direction: column;
        background: var(--bc-white);
      }

      .selector-background {
        background: var(--bc-white);
        padding: 2rem 1rem;
        min-height: 100vh;
        justify-content: flex-start;
      }

      .bc-logo-header {
        display: block !important;
        position: static;
        margin-bottom: 1rem;
        justify-content: center;

        .bc-header-logo {
          height: 80px;
          width: auto;
        }
      }

      .selector-card-container {
        max-width: 100%;
        width: 100%;
      }

      .selector-card {
        background: var(--bc-white);
        box-shadow: none;
        border-radius: 0;
        padding: 0;
      }

      .selector-header {
        text-align: center;
        margin-bottom: 3rem;

        h1 {
          color: var(--bc-bg-info);
          font-size: 2rem;
          font-weight: 700;
          margin-bottom: 0.5rem;
          line-height: 1.2;
        }

        h2 {
          color: var(--bc-primary);
          font-size: var(--bc-font-size-20);
          font-weight: 400;
          margin: 0;
          line-height: 1.3;
        }
      }

      .selector-description {
        text-align: center;
        margin-bottom: 3rem;
        padding: 0 1rem;

        p {
          color: var(--bc-primary);
          font-size: var(--bc-font-size-16);
          line-height: 1.6;
          margin: 0;
        }
      }

      .selector-btn {
        padding: 1rem 2rem;
        font-size: var(--bc-font-size-16);
        font-weight: 600;
        border-radius: 8px;
        margin: 0 1rem;
        background-color: var(--bc-bg-info);
        border: none;
        color: var(--bc-white);
        width: calc(100% - 2rem);
      }

      .mobile-preview-image {
        display: block;
        margin: 2rem 0;

        .dashboard-preview {
          max-width: 100%;
          height: auto;
        }
      }
    }
  `]
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
  showWorkspaceSelection = false;
  showSelectedWorkspace = false;
  showProviderSelection = false;
  private destroy$ = new Subject<void>();

  constructor(
    private workspaceService: WorkspaceService,
    private router: Router
  ) {}

  private hasFetchedWorkspaces = false;

  ngOnInit(): void {
    // Subscribe to workspace state changes
    this.workspaceService.currentWorkspaceState$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state: WorkspaceState) => {
        this.availableWorkspaces = state.availableWorkspaces;
        this.selectedWorkspace = state.selectedWorkspace;

        // Fetch workspaces if none are loaded (e.g. navigated back via "Change Workspace")
        if (state.availableWorkspaces.length === 0 && this.isLoading && !this.hasFetchedWorkspaces) {
          this.hasFetchedWorkspaces = true;
          this.workspaceService.getAvailableWorkspaces()
            .pipe(takeUntil(this.destroy$))
            .subscribe();
          return;
        }

        this.handleWorkspaceState(state);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private handleWorkspaceState(state: WorkspaceState): void {
    // If workspace and provider already selected, navigate away
    if (state.isWorkspaceSelected && state.isProviderSelected && state.selectedWorkspace) {
      console.log('Workspace and provider already selected, navigating to app');
      this.router.navigate(['/applicant-info']);
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
    
    console.log('Show workspace selection:', this.showWorkspaceSelection);
    console.log('Show selected workspace:', this.showSelectedWorkspace);
    console.log('Show provider selection:', this.showProviderSelection);
  }

  private autoSelectSingleWorkspace(workspace: Plugin): void {
    this.isAutoSelecting = true;
    this.autoSelectingWorkspace = workspace;
    
    console.log('Auto-selecting single workspace:', workspace);
    
    // Fetch providers from API then decide
    timer(800).subscribe(() => {
      this.workspaceService.getProviders(workspace.pluginId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.providers.length === 1) {
              this.selectWorkspaceWithProviderDetails(workspace, response.providers[0]);
            } else if (response.providers.length === 0) {
              this.selectWorkspace(workspace);
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
    });
  }

  private autoSelectSingleWorkspaceWithProvider(workspace: Plugin, provider: string): void {
    this.isAutoSelecting = true;
    this.autoSelectingWorkspace = workspace;
    
    console.log('Auto-selecting single workspace, fetching providers from API:', workspace);
    
    timer(800).subscribe(() => {
      this.workspaceService.getProviders(workspace.pluginId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.providers.length === 1) {
              this.selectWorkspaceWithProviderDetails(workspace, response.providers[0]);
            } else if (response.providers.length === 0) {
              this.selectWorkspace(workspace);
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
    });
  }

  onWorkspaceClick(workspace: Plugin): void {
    console.log('Workspace clicked:', workspace);
    
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
            // No providers, select workspace directly (fallback)
            console.warn('No providers returned for workspace:', workspace.pluginId);
            this.selectWorkspace(workspace);
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
    console.log('WorkspaceSelectorComponent - Workspace selected:', workspace);
    this.workspaceService.selectWorkspace(workspace);
    
    // Navigate to the main application
    this.router.navigate(['/applicant-info']);
  }

  selectWorkspaceWithProvider(workspace: Plugin, provider: string): void {
    console.log('Selecting workspace with provider:', workspace, provider);
    this.workspaceService.selectWorkspace(workspace, provider);
    
    // Navigate to the main application
    this.router.navigate(['/applicant-info']);
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
    console.log('Selecting workspace with provider details:', workspace, provider);
    this.workspaceService.selectWorkspaceWithProviderDetails(workspace, provider);
    this.router.navigate(['/applicant-info']);
  }

  goBackToWorkspaceSelection(): void {
    this.selectedWorkspaceForProvider = null;
    this.selectedDropdownProvider = null;
    this.availableProviders = [];
    this.showProviderSelection = false;
    this.showWorkspaceSelection = true;
  }
}