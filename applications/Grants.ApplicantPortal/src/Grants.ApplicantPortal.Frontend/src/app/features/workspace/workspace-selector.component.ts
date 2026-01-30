import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject, takeUntil, timer } from 'rxjs';
import { WorkspaceService } from '../../core/services/workspace.service';
import { Plugin, WorkspaceState } from '../../shared/models/workspace.interface';

@Component({
  selector: 'app-workspace-selector',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="workspace-selector-container">
      <div class="workspace-selector-modal">
        <div class="modal-content">
          
          <!-- Loading State -->
          <div *ngIf="isLoading" class="loading-container">
            <div class="loading-spinner">
              <div class="spinner"></div>
            </div>
            <h2 class="loading-title">Setting up your workspace...</h2>
            <p class="loading-description">Please wait while we prepare your environment.</p>
          </div>

          <!-- Auto-selecting Single Workspace -->
          <div *ngIf="isAutoSelecting" class="loading-container">
            <div class="loading-spinner">
              <div class="spinner"></div>
            </div>
            <h2 class="loading-title">Accessing {{ autoSelectingWorkspace?.description }}</h2>
            <p class="loading-description">Automatically selecting your workspace...</p>
          </div>

          <!-- Multiple Workspaces Selection -->
          <div *ngIf="showWorkspaceSelection">
            <h2 class="modal-title">Select Workspace</h2>
            <p class="modal-description">Please select a workspace to continue:</p>
            
            <div class="workspace-list">
              <div 
                *ngFor="let workspace of availableWorkspaces" 
                class="workspace-item"
                (click)="onWorkspaceClick(workspace)"
                (keydown.enter)="onWorkspaceClick(workspace)"
                (keydown.space)="onWorkspaceClick(workspace)"
                tabindex="0"
                role="button"
                [attr.aria-label]="'Select ' + workspace.description + ' workspace'"
              >
                <div class="workspace-info">
                  <h3 class="workspace-title">{{ workspace.description }}</h3>
                  <p class="workspace-id">ID: {{ workspace.pluginId }}</p>
                  <div class="workspace-providers" *ngIf="workspace.providers && workspace.providers.length > 1">
                    <span class="providers-label">Providers:</span>
                    <span class="provider-tag" *ngFor="let provider of workspace.providers">
                      {{ provider }}
                    </span>
                  </div>
                  <div class="workspace-features" *ngIf="workspace.features?.length">
                    <span class="features-label">Features:</span>
                    <span class="feature-tag" *ngFor="let feature of workspace.features">
                      {{ feature }}
                    </span>
                  </div>
                </div>
                <i class="fas fa-chevron-right workspace-arrow" aria-hidden="true"></i>
              </div>
            </div>
          </div>

          <!-- Provider Selection -->
          <div *ngIf="showProviderSelection && selectedWorkspaceForProvider">
            <h2 class="modal-title">Select Provider</h2>
            <p class="modal-description">Select a provider for {{ selectedWorkspaceForProvider.description }}:</p>
            
            <div class="provider-list">
              <div 
                *ngFor="let provider of selectedWorkspaceForProvider.providers" 
                class="provider-item"
                (click)="selectWorkspaceWithProvider(selectedWorkspaceForProvider, provider)"
                (keydown.enter)="selectWorkspaceWithProvider(selectedWorkspaceForProvider, provider)"
                (keydown.space)="selectWorkspaceWithProvider(selectedWorkspaceForProvider, provider)"
                tabindex="0"
                role="button"
                [attr.aria-label]="'Select ' + provider + ' provider'"
              >
                <div class="provider-info">
                  <h3 class="provider-title">{{ provider }}</h3>
                </div>
                <i class="fas fa-chevron-right provider-arrow" aria-hidden="true"></i>
              </div>
            </div>
            
            <button 
              class="btn btn-secondary mt-3"
              (click)="goBackToWorkspaceSelection()"
            >
              <i class="fas fa-arrow-left me-2"></i>
              Back to Workspaces
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .workspace-selector-container {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background-color: rgba(0, 0, 0, 0.5);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 1000;
    }

    .workspace-selector-modal {
      background: var(--bc-white);
      border-radius: 8px;
      padding: 32px;
      max-width: 600px;
      width: 90%;
      max-height: 80vh;
      overflow-y: auto;
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
    }

    .loading-container {
      text-align: center;
      padding: 20px 0;
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      margin-bottom: 24px;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 3px solid var(--bc-gray-20);
      border-top: 3px solid var(--bc-blue);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .loading-title {
      font-size: var(--bc-font-size-24);
      font-weight: 600;
      color: var(--bc-primary);
      margin-bottom: 8px;
    }

    .loading-description {
      font-size: var(--bc-font-size-14);
      color: var(--bc-primary);
    }

    .modal-title {
      font-size: var(--bc-font-size-24);
      font-weight: 600;
      color: var(--bc-primary);
      margin-bottom: 8px;
      text-align: center;
    }

    .modal-description {
      font-size: var(--bc-font-size-14);
      color: var(--bc-primary);
      text-align: center;
      margin-bottom: 24px;
    }

    .workspace-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .workspace-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 20px;
      border: 2px solid var(--bc-gray-50);
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s ease;
      background: var(--bc-white);

      &:hover {
        border-color: var(--bc-blue);
        background-color: var(--bc-blue-10);
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 51, 102, 0.1);
      }

      &:focus {
        outline: 2px solid var(--bc-blue);
        outline-offset: 2px;
        border-color: var(--bc-blue);
      }

      &:active {
        transform: translateY(0);
      }
    }

    .workspace-info {
      flex: 1;
    }

    .workspace-title {
      font-size: var(--bc-font-size-18);
      font-weight: 600;
      color: var(--bc-primary);
      margin-bottom: 4px;
    }

    .workspace-id {
      font-size: var(--bc-font-size-13);
      color: var(--bc-primary);
      margin-bottom: 12px;
    }

    .workspace-features {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
    }

    .features-label {
      font-size: var(--bc-font-size-12);
      color: var(--bc-primary);
      font-weight: 500;
    }

    .feature-tag {
      background-color: var(--bc-gray-20);
      color: var(--bc-primary);
      padding: 4px 8px;
      border-radius: 4px;
      font-size: var(--bc-font-size-12);
      font-weight: 500;
    }

    .workspace-providers {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
    }

    .providers-label {
      font-size: var(--bc-font-size-12);
      color: var(--bc-primary);
      font-weight: 500;
    }

    .provider-tag {
      background-color: var(--bc-blue-light);
      color: var(--bc-blue-dark);
      padding: 4px 8px;
      border-radius: 4px;
      font-size: var(--bc-font-size-12);
      font-weight: 500;
    }

    .provider-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .provider-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px;
      border: 1px solid var(--bc-gray-20);
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s ease;
      background-color: var(--bc-white);
    }

    .provider-item:hover {
      border-color: var(--bc-blue);
      background-color: var(--bc-gray-05);
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    .provider-item:focus {
      outline: none;
      border-color: var(--bc-blue);
      box-shadow: 0 0 0 3px rgba(66, 165, 245, 0.2);
    }

    .provider-info {
      flex: 1;
    }

    .provider-title {
      font-size: var(--bc-font-size-18);
      font-weight: 600;
      color: var(--bc-primary);
      margin: 0;
    }

    .provider-arrow {
      color: var(--bc-primary);
      font-size: var(--bc-font-size-16);
      transition: color 0.2s ease, transform 0.2s ease;
    }

    .provider-item:hover .provider-arrow {
      color: var(--bc-blue);
      transform: translateX(4px);
    }

    .workspace-arrow {
      color: var(--bc-primary);
      font-size: var(--bc-font-size-16);
      transition: color 0.2s ease, transform 0.2s ease;
    }

    .workspace-item:hover .workspace-arrow {
      color: var(--bc-blue);
      transform: translateX(4px);
    }

    @media (max-width: 768px) {
      .workspace-selector-modal {
        padding: 24px;
        margin: 16px;
        max-width: none;
        width: calc(100% - 32px);
      }

      .workspace-item {
        padding: 16px;
      }

      .workspace-title {
        font-size: var(--bc-font-size-16);
      }
    }
  `]
})
export class WorkspaceSelectorComponent implements OnInit, OnDestroy {
  availableWorkspaces: Plugin[] = [];
  selectedWorkspace: Plugin | null = null;
  selectedWorkspaceForProvider: Plugin | null = null;
  isLoading = true;
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

  ngOnInit(): void {
    // Subscribe to workspace state changes
    this.workspaceService.currentWorkspaceState$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state: WorkspaceState) => {
        this.availableWorkspaces = state.availableWorkspaces;
        this.selectedWorkspace = state.selectedWorkspace;
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
    
    // Add a small delay for better UX - user sees the intentional selection
    timer(1200).subscribe(() => {
      this.selectWorkspace(workspace);
    });
  }

  private autoSelectSingleWorkspaceWithProvider(workspace: Plugin, provider: string): void {
    this.isAutoSelecting = true;
    this.autoSelectingWorkspace = workspace;
    
    console.log('Auto-selecting single workspace with provider:', workspace, provider);
    
    // Add a small delay for better UX - user sees the intentional selection
    timer(1200).subscribe(() => {
      this.selectWorkspaceWithProvider(workspace, provider);
    });
  }

  onWorkspaceClick(workspace: Plugin): void {
    console.log('Workspace clicked:', workspace);
    
    // If workspace has only one provider, auto-select it
    if (workspace.providers && workspace.providers.length === 1) {
      this.selectWorkspaceWithProvider(workspace, workspace.providers[0]);
    } else if (workspace.providers && workspace.providers.length > 1) {
      // Show provider selection
      this.selectedWorkspaceForProvider = workspace;
      this.showWorkspaceSelection = false;
      this.showProviderSelection = true;
    } else {
      // No providers, select workspace directly (fallback)
      this.selectWorkspace(workspace);
    }
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

  goBackToWorkspaceSelection(): void {
    this.selectedWorkspaceForProvider = null;
    this.showProviderSelection = false;
    this.showWorkspaceSelection = true;
  }
}