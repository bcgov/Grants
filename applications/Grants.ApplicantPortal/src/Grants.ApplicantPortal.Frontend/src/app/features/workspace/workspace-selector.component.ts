import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
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
          <h2 class="modal-title">Select Workspace</h2>
          <p class="modal-description">Please select a workspace to continue:</p>
          
          <div class="workspace-list">
            <div 
              *ngFor="let workspace of availableWorkspaces" 
              class="workspace-item"
              (click)="selectWorkspace(workspace)"
              (keydown.enter)="selectWorkspace(workspace)"
              (keydown.space)="selectWorkspace(workspace)"
              tabindex="0"
              role="button"
              [attr.aria-label]="'Select ' + workspace.description + ' workspace'"
            >
              <div class="workspace-info">
                <h3 class="workspace-title">{{ workspace.description }}</h3>
                <p class="workspace-id">ID: {{ workspace.pluginId }}</p>
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

    .modal-title {
      font-size: var(--bc-font-size-24);
      font-weight: 600;
      color: var(--bc-primary);
      margin-bottom: 8px;
      text-align: center;
    }

    .modal-description {
      font-size: var(--bc-font-size-14);
      color: var(--bc-gray);
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
      color: var(--bc-gray);
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
      color: var(--bc-gray);
      font-weight: 500;
    }

    .feature-tag {
      background-color: var(--bc-gray-20);
      color: var(--bc-primary);
      padding: 2px 8px;
      border-radius: 4px;
      font-size: var(--bc-font-size-11);
      font-weight: 500;
    }

    .workspace-arrow {
      color: var(--bc-gray);
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
export class WorkspaceSelectorComponent implements OnInit {
  availableWorkspaces: Plugin[] = [];

  constructor(
    private workspaceService: WorkspaceService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Get available workspaces from service
    this.workspaceService.currentWorkspaceState$.subscribe((state: WorkspaceState) => {
      this.availableWorkspaces = state.availableWorkspaces;
    });
  }

  selectWorkspace(workspace: Plugin): void {
    console.log('WorkspaceSelectorComponent - Workspace selected:', workspace);
    this.workspaceService.selectWorkspace(workspace);
    
    // Navigate to the main application
    this.router.navigate(['/applicant-info']);
  }
}