import { Injectable } from '@angular/core';
import {
  OidcClientNotification,
  OidcSecurityService,
  OpenIdConfiguration,
} from 'angular-auth-oidc-client';
import { Observable, map, catchError, of, BehaviorSubject, tap } from 'rxjs';
import { Router } from '@angular/router';
import { ErrorHandlerService } from './error-handler.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private authState$ = new BehaviorSubject<{ isAuthenticated: boolean; error?: string }>(
    { isAuthenticated: false }
  );

  constructor(
    private readonly oidcSecurityService: OidcSecurityService,
    private router: Router,
    private errorHandler: ErrorHandlerService
  ) {
    // Monitor authentication state changes with error handling
    this.oidcSecurityService.isAuthenticated$.subscribe({
      next: (result) => {
        console.log('AuthService - OIDC result updated:', result);
        this.authState$.next({ 
          isAuthenticated: result.isAuthenticated
        });

        // If user logs out, clear workspace
        if (!result.isAuthenticated) {
          this.clearWorkspaceOnLogout();
        }
      },
      error: (error) => {
        console.error('AuthService - Authentication state error:', error);
        
        // Use error handler to determine if this is an auth state corruption
        if (this.errorHandler.isAuthStateCorrupted(error)) {
          this.authState$.next({ 
            isAuthenticated: false, 
            error: 'Authentication state corrupted' 
          });
        } else {
          this.authState$.next({ 
            isAuthenticated: false, 
            error: error.message || 'Authentication error' 
          });
        }
      }
    });
  }

  private clearWorkspaceOnLogout(): void {
    // Dynamic import to avoid circular dependency
    import('./workspace.service').then(({ WorkspaceService }) => {
      // Get service instance from injector if needed
      // For now, just clear localStorage directly
      localStorage.removeItem('selectedWorkspace');
    });
  }

  get isAuthenticated$(): Observable<boolean> {
    return this.oidcSecurityService.isAuthenticated$.pipe(
      map((result) => {
        console.log('AuthService - OIDC result:', result);
        console.log('AuthService - isAuthenticated:', result.isAuthenticated);
        
        return result.isAuthenticated;
      }),
      catchError((error) => {
        console.error('AuthService - isAuthenticated$ error:', error);
        
        // Use error handler for authentication-specific errors
        if (this.errorHandler.isAuthStateCorrupted(error)) {
          this.errorHandler.handleAuthError(error).subscribe({
            error: () => {} // Error handler manages the flow
          });
        }
        
        return of(false);
      })
    );
  }

  get userData$() {
    return this.oidcSecurityService.userData$;
  }

  get authenticationState$() {
    return this.authState$.asObservable();
  }

  login(): void {
    console.log('AuthService - Initiating login');
    this.oidcSecurityService.authorize();
  }

  logout(): void {
    console.log('AuthService - Initiating logout');
    
    // Perform comprehensive cleanup before logout
    this.errorHandler.cleanupForHeaderSizeError();
    
    // Navigate to logout route which will handle the cleanup and redirection
    this.router.navigate(['/logout']);
  }

  /**
   * Emergency cleanup for HTTP 431 errors
   */
  emergencyCleanup(): void {
    console.log('AuthService - Performing emergency cleanup for header size issues');
    this.errorHandler.cleanupForHeaderSizeError();
    
    // Force logout and redirect
    setTimeout(() => {
      this.router.navigate(['/login']);
    }, 100);
  }

  getAccessToken(): Observable<string> {
    return this.oidcSecurityService.getAccessToken().pipe(
      catchError((error) => {
        console.error('AuthService - getAccessToken error:', error);
        return of('');
      })
    );
  }

  /**
   * Force refresh the session/tokens
   */
  refreshSession(): Observable<any> {
    console.log('AuthService - Forcing session refresh');
    return this.oidcSecurityService.forceRefreshSession().pipe(
      tap((result) => {
        if (result && result.isAuthenticated) {
          console.log('AuthService - Session refresh successful');
        }
      }),
      catchError((error) => {
        console.error('AuthService - refreshSession error:', error);
        
        // Use error handler for authentication-specific errors
        if (this.errorHandler.isAuthStateCorrupted(error)) {
          return this.errorHandler.handleAuthError(error);
        }
        
        // For other errors, still redirect to login
        this.router.navigate(['/login']);
        return of({ isAuthenticated: false, error: error.message });
      })
    );
  }

  /**
   * Check current authentication status
   */
  checkAuth(): Observable<any> {
    return this.oidcSecurityService.checkAuth().pipe(
      tap((result) => {
        console.log('AuthService - checkAuth result:', {
          isAuthenticated: result.isAuthenticated,
          hasUserData: !!result.userData,
          hasAccessToken: !!result.accessToken
        });
      }),
      catchError((error) => {
        console.error('AuthService - checkAuth error:', error);
        
        // Use error handler for authentication-specific errors
        if (this.errorHandler.isAuthStateCorrupted(error)) {
          this.errorHandler.handleAuthError(error).subscribe({
            error: () => {} // Error handler manages the flow
          });
        }
        
        return of({ isAuthenticated: false, error: error.message });
      })
    );
  }
}
