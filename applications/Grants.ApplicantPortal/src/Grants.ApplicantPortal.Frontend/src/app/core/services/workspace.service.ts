import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, of, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Plugin, PluginsResponse, WorkspaceState } from '../../shared/models/workspace.interface';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {
  private readonly apiUrl = environment.apiUrl;
  
  private workspaceState$ = new BehaviorSubject<WorkspaceState>({
    selectedWorkspace: null,
    availableWorkspaces: [],
    isWorkspaceSelected: false
  });

  private changingWorkspace$ = new BehaviorSubject<boolean>(false);

  constructor(private http: HttpClient) {}

  get currentWorkspaceState$(): Observable<WorkspaceState> {
    return this.workspaceState$.asObservable();
  }

  get selectedWorkspace$(): Observable<Plugin | null> {
    return this.workspaceState$.pipe(
      map(state => state.selectedWorkspace)
    );
  }

  get currentPluginId(): string | null {
    return this.workspaceState$.value.selectedWorkspace?.pluginId || null;
  }

  get isChangingWorkspace$(): Observable<boolean> {
    return this.changingWorkspace$.asObservable();
  }

  /**
   * Fetch available workspaces/plugins from the API
   */
  getAvailableWorkspaces(): Observable<PluginsResponse> {
    return this.http.get<PluginsResponse>(`${this.apiUrl}/System/plugins`).pipe(
      tap(response => {
        console.log('WorkspaceService - Available workspaces:', response);
        
        const currentState = this.workspaceState$.value;
        this.workspaceState$.next({
          ...currentState,
          availableWorkspaces: response.plugins
        });

        // If there's a saved workspace, try to find it in the available workspaces
        const savedWorkspace = localStorage.getItem('selectedWorkspace');
        if (savedWorkspace && !currentState.isWorkspaceSelected) {
          try {
            const parsedWorkspace = JSON.parse(savedWorkspace) as Plugin;
            const matchingWorkspace = response.plugins.find(
              (workspace: Plugin) => workspace.pluginId === parsedWorkspace.pluginId
            );
            
            if (matchingWorkspace) {
              console.log('WorkspaceService - Found matching saved workspace:', matchingWorkspace);
              this.selectWorkspace(matchingWorkspace);
              return;
            } else {
              // Saved workspace is no longer available, remove it
              console.log('WorkspaceService - Saved workspace no longer available, removing from storage');
              localStorage.removeItem('selectedWorkspace');
            }
          } catch (error) {
            console.error('WorkspaceService - Error parsing saved workspace:', error);
            localStorage.removeItem('selectedWorkspace');
          }
        }

        // Auto-select workspace if only one available and no selection made
        if (response.plugins.length === 1 && !currentState.isWorkspaceSelected) {
          console.log('WorkspaceService - Auto-selecting single workspace:', response.plugins[0]);
          // Don't auto-select here - let the component handle it for better UX
          // This allows the component to show a proper loading state
        }
      }),
      catchError(error => {
        console.error('WorkspaceService - Error fetching workspaces:', error);
        return of({ plugins: [] });
      })
    );
  }

  /**
   * Select a workspace
   */
  selectWorkspace(workspace: Plugin): void {
    console.log('WorkspaceService - Selecting workspace:', workspace);
    
    // Set loading state
    this.changingWorkspace$.next(true);
    
    const currentState = this.workspaceState$.value;
    this.workspaceState$.next({
      ...currentState,
      selectedWorkspace: workspace,
      isWorkspaceSelected: true
    });

    // Store in localStorage for persistence
    localStorage.setItem('selectedWorkspace', JSON.stringify(workspace));

    // Clear loading state after a short delay to allow components to refresh
    setTimeout(() => {
      this.changingWorkspace$.next(false);
    }, 500);
  }

  /**
   * Clear workspace selection (for logout)
   */
  clearWorkspace(): void {
    console.log('WorkspaceService - Clearing workspace selection');
    
    this.changingWorkspace$.next(false);
    this.workspaceState$.next({
      selectedWorkspace: null,
      availableWorkspaces: [],
      isWorkspaceSelected: false
    });

    localStorage.removeItem('selectedWorkspace');
  }

  /**
   * Restore workspace selection from localStorage
   */
  restoreWorkspaceFromStorage(): void {
    const savedWorkspace = localStorage.getItem('selectedWorkspace');
    if (savedWorkspace) {
      try {
        const workspace = JSON.parse(savedWorkspace) as Plugin;
        console.log('WorkspaceService - Restored workspace from storage:', workspace);
        
        const currentState = this.workspaceState$.value;
        this.workspaceState$.next({
          ...currentState,
          selectedWorkspace: workspace,
          isWorkspaceSelected: true
        });
      } catch (error) {
        console.error('WorkspaceService - Error parsing saved workspace:', error);
        localStorage.removeItem('selectedWorkspace');
      }
    }
  }

  /**
   * Check if workspace selection is required
   */
  isWorkspaceSelectionRequired(): boolean {
    const state = this.workspaceState$.value;
    return state.availableWorkspaces.length > 1 && !state.isWorkspaceSelected;
  }

  /**
   * Get workspace by ID
   */
  getWorkspaceById(pluginId: string): Plugin | undefined {
    return this.workspaceState$.value.availableWorkspaces.find(
      (workspace: Plugin) => workspace.pluginId === pluginId
    );
  }
}