import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { take, timeout } from 'rxjs/operators';
import { WorkspaceService } from '../../../core/services/workspace.service';

@Component({
  selector: 'app-auth-callback',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './callback.component.html',
  styleUrls: ['./callback.component.scss'],
})
export class CallbackComponent implements OnInit {
  isProcessing = true;
  errorMessage: string | null = null;

  constructor(
    private readonly oidcSecurityService: OidcSecurityService,
    private readonly router: Router,
    private readonly workspaceService: WorkspaceService
  ) {}

  ngOnInit(): void {
    // Add a timeout to prevent indefinite waiting
    this.handleAuthCallback();
  }

  private handleAuthCallback(): void {
    // Clear any existing auth state that might be causing conflicts
    this.clearAuthStorage();
    
    // Handle the callback from the identity provider
    this.oidcSecurityService
      .checkAuth()
      .pipe(
        take(1),
        timeout(15000) // Increased timeout to 15 seconds
      )
      .subscribe({
        next: (result) => {
          this.isProcessing = false;
          
          if (result.isAuthenticated && result.accessToken) {
            // Restore any previously selected workspace
            this.workspaceService.restoreWorkspaceFromStorage();
            
            // Fetch available workspaces
            this.workspaceService.getAvailableWorkspaces().subscribe({
              next: (workspacesResponse) => {
                const currentState = this.workspaceService.currentWorkspaceState$;
                currentState.pipe(take(1)).subscribe(state => {
                  // Always go to workspace selector - let it handle auto-selection with proper UX
                  this.router.navigate(['/workspace-selector']);
                });
              },
              error: (workspaceError) => {
                console.error('Error fetching workspaces:', workspaceError);
                // Continue to app even if workspace fetch fails
                this.router.navigate(['/app']);
              }
            });
          } else {
            this.errorMessage = 'Authentication failed. Please try logging in again.';
            this.redirectToLoginAfterDelay();
          }
        },
        error: (error) => {
          this.isProcessing = false;
          console.error('Auth check error:', error);
          
          // Handle specific error types
          if (error.name === 'TimeoutError') {
            this.errorMessage = 'Authentication timed out. Please try again.';
          } else if (error.message?.includes('could not find matching config for state')) {
            this.errorMessage = 'Authentication state mismatch. Please try logging in again.';
            // Clear all auth storage for state mismatch errors
            this.clearAllStorage();
          } else {
            this.errorMessage = 'Authentication error. Please try logging in again.';
          }
          
          this.redirectToLoginAfterDelay();
        },
      });
  }

  private clearAuthStorage(): void {
    try {
      // Clear OIDC specific storage keys but preserve workspace selection
      const keysToRemove = [
        'angular-auth-oidc-client',
        'oidc.user',
        'oidc.id_token',
        'oidc.access_token',
        'oidc.refresh_token',
        'oidc.state',
        'oidc.nonce'
      ];
      
      keysToRemove.forEach(key => {
        localStorage.removeItem(key);
        sessionStorage.removeItem(key);
      });
    } catch (error) {
      console.warn('Error clearing auth storage:', error);
    }
  }

  private clearAllStorage(): void {
    try {
      localStorage.clear();
      sessionStorage.clear();
    } catch (error) {
      console.warn('Error clearing all storage:', error);
    }
  }

  private redirectToLoginAfterDelay(): void {
    setTimeout(() => {
      this.router.navigate(['/login']);
    }, 3000); // Increased delay to 3 seconds to show error message
  }
}
