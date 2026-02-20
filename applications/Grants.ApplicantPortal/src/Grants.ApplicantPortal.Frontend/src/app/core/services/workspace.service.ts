import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, of, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Plugin, PluginsResponse, Provider, ProvidersResponse, WorkspaceState } from '../../shared/models/workspace.interface';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {
  private readonly apiUrl = environment.apiUrl;
  
  private workspaceState$ = new BehaviorSubject<WorkspaceState>({
    selectedWorkspace: null,
    selectedProvider: null,
    selectedProviderName: null,
    availableWorkspaces: [],
    isWorkspaceSelected: false,
    isProviderSelected: false
  });

  private changingWorkspace$ = new BehaviorSubject<boolean>(false);
  private workspaceProviderMemory = new Map<string, string>(); // workspace.pluginId -> last selected provider

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

  get currentProvider(): string | null {
    return this.workspaceState$.value.selectedProvider || null;
  }

  get isChangingWorkspace$(): Observable<boolean> {
    return this.changingWorkspace$.asObservable();
  }

  /**
   * Fetch providers for a specific plugin from the API
   */
  getProviders(pluginId: string): Observable<ProvidersResponse> {
    return this.http.get<ProvidersResponse>(`${this.apiUrl}/Plugins/${pluginId}/providers`).pipe(
      tap(response => {
        console.log('WorkspaceService - Providers for', pluginId, ':', response);
      }),
      catchError(error => {
        console.error('WorkspaceService - Error fetching providers for', pluginId, ':', error);
        return of({ pluginId, providers: [] });
      })
    );
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

        // Check if we can auto-select workspace and provider
        this.handleAutoSelection(response.plugins, currentState);
      }),
      catchError(error => {
        console.error('WorkspaceService - Error fetching workspaces:', error);
        return of({ plugins: [] });
      })
    );
  }

  /**
   * Select a workspace and optionally a provider
   */
  selectWorkspace(workspace: Plugin, provider?: string): void {
    console.log('WorkspaceService - Selecting workspace:', workspace.pluginId, 'provider:', provider);
    
    // Set loading state
    this.changingWorkspace$.next(true);
    
    // If no provider specified, try to use a previously remembered provider for this workspace.
    // Provider lists are now fetched from the API, so we never read workspace.providers here.
    if (!provider) {
      const rememberedProvider = this.workspaceProviderMemory.get(workspace.pluginId);
      if (rememberedProvider) {
        provider = rememberedProvider;
        console.log('WorkspaceService - Using remembered provider:', provider);
      }
    }
    
    // Store the provider selection in memory for this workspace
    if (provider && workspace.pluginId) {
      this.workspaceProviderMemory.set(workspace.pluginId, provider);
      console.log('WorkspaceService - Remembering provider selection:', { workspace: workspace.pluginId, provider });
    }
    
    const currentState = this.workspaceState$.value;
    const newState = {
      ...currentState,
      selectedWorkspace: workspace,
      selectedProvider: provider || null,
      selectedProviderName: currentState.selectedProviderName,
      isWorkspaceSelected: true,
      isProviderSelected: !!provider
    };
    
    console.log('WorkspaceService - Updating state to:', newState);
    this.workspaceState$.next(newState);

    // Store in localStorage for persistence
    const selectionData = { workspace, provider, providerName: newState.selectedProviderName };
    localStorage.setItem('selectedWorkspace', JSON.stringify(selectionData));
    console.log('WorkspaceService - Saved to localStorage:', selectionData);

    // Clear loading state after a short delay to allow components to refresh
    setTimeout(() => {
      this.changingWorkspace$.next(false);
    }, 500);
  }

  /**
   * Select a workspace with full provider details (id + name)
   */
  selectWorkspaceWithProviderDetails(workspace: Plugin, provider: Provider): void {
    console.log('WorkspaceService - Selecting workspace with provider details:', workspace.pluginId, provider);
    
    this.changingWorkspace$.next(true);
    
    if (provider && workspace.pluginId) {
      this.workspaceProviderMemory.set(workspace.pluginId, provider.id);
    }
    
    const currentState = this.workspaceState$.value;
    const newState = {
      ...currentState,
      selectedWorkspace: workspace,
      selectedProvider: provider.id,
      selectedProviderName: provider.name,
      isWorkspaceSelected: true,
      isProviderSelected: true
    };
    
    this.workspaceState$.next(newState);

    const selectionData = { workspace, provider: provider.id, providerName: provider.name };
    localStorage.setItem('selectedWorkspace', JSON.stringify(selectionData));
    console.log('WorkspaceService - Saved to localStorage:', selectionData);

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
      selectedProvider: null,
      selectedProviderName: null,
      availableWorkspaces: [],
      isWorkspaceSelected: false,
      isProviderSelected: false
    });

    localStorage.removeItem('selectedWorkspace');
  }

  /**
   * Clear only the workspace/provider selection but keep available workspaces (for switching)
   */
  clearSelection(): void {
    console.log('WorkspaceService - Clearing workspace selection (keeping available workspaces)');
    
    this.changingWorkspace$.next(false);
    const currentState = this.workspaceState$.value;
    this.workspaceState$.next({
      selectedWorkspace: null,
      selectedProvider: null,
      selectedProviderName: null,
      availableWorkspaces: currentState.availableWorkspaces,
      isWorkspaceSelected: false,
      isProviderSelected: false
    });

    localStorage.removeItem('selectedWorkspace');
  }

  /**
   * Restore workspace selection from localStorage
   */
  restoreWorkspaceFromStorage(): void {
    const savedSelection = localStorage.getItem('selectedWorkspace');
    if (savedSelection) {
      try {
        const parsed = JSON.parse(savedSelection);
        // Handle both old format (just workspace) and new format (workspace + provider)
        const workspace = parsed.workspace || parsed;
        const provider = parsed.provider;
        
        const providerName = parsed.providerName;
        console.log('WorkspaceService - Restored workspace from storage:', workspace?.pluginId, 'provider:', provider, 'providerName:', providerName);
        
        // Restore provider memory if available
        if (workspace?.pluginId && provider) {
          this.workspaceProviderMemory.set(workspace.pluginId, provider);
          console.log('WorkspaceService - Restored provider memory:', { workspace: workspace.pluginId, provider });
        }
        
        const currentState = this.workspaceState$.value;
        this.workspaceState$.next({
          ...currentState,
          selectedWorkspace: workspace,
          selectedProvider: provider ?? null,
          selectedProviderName: providerName ?? null,
          isWorkspaceSelected: true,
          isProviderSelected: !!provider
        });
      } catch (error) {
        console.error('WorkspaceService - Error parsing saved workspace:', error);
        localStorage.removeItem('selectedWorkspace');
      }
    }
  }

  /**
   * Check if workspace and provider selection is required
   */
  isWorkspaceSelectionRequired(): boolean {
    const state = this.workspaceState$.value;
    return !state.isWorkspaceSelected || !state.isProviderSelected;
  }

  /**
   * Check if workspace is selected
   */
  isWorkspaceSelected(): boolean {
    return this.workspaceState$.value.isWorkspaceSelected;
  }

  /**
   * Check if provider is selected
   */
  isProviderSelected(): boolean {
    return this.workspaceState$.value.isProviderSelected;
  }

  /**
   * Handle auto-selection logic for workspace and provider
   */
  private handleAutoSelection(plugins: Plugin[], currentState: WorkspaceState): void {
    // Try to restore from localStorage first
    const savedSelection = localStorage.getItem('selectedWorkspace');
    if (savedSelection && !currentState.isWorkspaceSelected) {
      try {
        const parsed = JSON.parse(savedSelection);
        const workspace = parsed.workspace || parsed;
        const provider = parsed.provider;
        
        const matchingWorkspace = plugins.find(
          (p: Plugin) => p.pluginId === workspace.pluginId
        );
        
        if (matchingWorkspace) {
          // Restore the saved workspace (and provider if one was persisted).
          // Provider validity is no longer checked against workspace.providers
          // because provider lists are now fetched from the API.
          if (provider) {
            console.log('WorkspaceService - Restored saved workspace and provider');
            this.selectWorkspace(matchingWorkspace, provider);
          } else {
            console.log('WorkspaceService - Restored workspace without provider');
            this.selectWorkspace(matchingWorkspace);
          }
          return;
        } else {
          console.log('WorkspaceService - Saved workspace no longer available');
          localStorage.removeItem('selectedWorkspace');
        }
      } catch (error) {
        console.error('WorkspaceService - Error parsing saved selection:', error);
        localStorage.removeItem('selectedWorkspace');
      }
    }

    // Auto-selection logic for new sessions.
    // Provider counts are no longer available on the Plugin object;
    // components fetch providers from the API and handle selection.
    if (plugins.length === 1) {
      console.log('WorkspaceService - Single workspace available; component will handle provider selection');
    }
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