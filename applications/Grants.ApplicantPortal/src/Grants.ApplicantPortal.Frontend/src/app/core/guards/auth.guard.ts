import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
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

      // submission-print is self-contained — it reads pluginId/provider/submissionId directly
      // from the route and calls the backend with them, never touching WorkspaceService — so it
      // doesn't need a workspace pre-selected. Opened as a fresh tab (see
      // SubmissionPdfService.viewSubmissionPdf), it would otherwise always hit the
      // workspace-selection-required branch below (a brand-new tab always starts with empty
      // workspace state) and get redirected through /workspace-selector, whose returnUrl
      // sanitizer only allows /app/ URLs and silently falls back to the default applicant-info
      // page for anything else — which looked like the new tab had just duplicated the original.
      // Checked against the path only (before any `?`), not the raw `state.url` — a query param
      // on an unrelated route (e.g. `?returnUrl=/submission-print/...`) must not match this.
      const path = state.url.split('?')[0];
      if (path.startsWith('/submission-print/')) {
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
