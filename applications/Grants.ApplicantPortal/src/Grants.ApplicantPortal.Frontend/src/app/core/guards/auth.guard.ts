import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { CanActivateFn } from '@angular/router';
import { map, take, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { WorkspaceService } from '../services/workspace.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const workspaceService = inject(WorkspaceService);
  const router = inject(Router);

  return authService.isAuthenticated$.pipe(
    take(1),
    switchMap((isAuthenticated) => {
      console.log('Auth Guard - isAuthenticated:', isAuthenticated);
      console.log('Auth Guard - current route:', state.url);
      
      if (!isAuthenticated) {
        console.log('Auth Guard - User not authenticated, redirecting to login');
        router.navigate(['/login']);
        return of(false);
      }

      // If accessing workspace selector, allow
      if (state.url.includes('/workspace-selector')) {
        return of(true);
      }

      // Check workspace selection
      return workspaceService.currentWorkspaceState$.pipe(
        take(1),
        map(workspaceState => {
          console.log('Auth Guard - workspace state:', workspaceState);
          
          // If no workspaces available yet, fetch them
          if (workspaceState.availableWorkspaces.length === 0) {
            workspaceService.getAvailableWorkspaces().subscribe(response => {
              // After fetching, the service will automatically handle saved workspace restoration
              // Only redirect to selector if selection is still required
              setTimeout(() => {
                if (workspaceService.isWorkspaceSelectionRequired()) {
                  router.navigate(['/workspace-selector']);
                }
              }, 100); // Small delay to allow service to process
            });
            return true; // Allow current navigation, redirect will happen after fetch if needed
          }
          
          // If workspace selection is required, redirect to selector
          if (workspaceService.isWorkspaceSelectionRequired()) {
            console.log('Auth Guard - Workspace selection required');
            router.navigate(['/workspace-selector']);
            return false;
          }
          
          console.log('Auth Guard - User authenticated and workspace selected, allowing access');
          return true;
        })
      );
    })
  );
};
