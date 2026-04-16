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
      if (!isAuthenticated) {
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
          // If no workspaces available yet, fetch them
          if (workspaceState.availableWorkspaces.length === 0) {
            workspaceService.getAvailableWorkspaces().subscribe(response => {
              // Always redirect to workspace selector for consistent UX
              // It will handle auto-selection with proper loading states
              router.navigate(['/workspace-selector'], {
                queryParams: { returnUrl: state.url }
              });
            });
            return false; // Block navigation until workspace is handled
          }
          
          // If workspace selection is required, redirect to selector
          if (workspaceService.isWorkspaceSelectionRequired() || !workspaceState.isWorkspaceSelected) {
            router.navigate(['/workspace-selector'], {
              queryParams: { returnUrl: state.url }
            });
            return false;
          }
          
          return true;
        })
      );
    })
  );
};
