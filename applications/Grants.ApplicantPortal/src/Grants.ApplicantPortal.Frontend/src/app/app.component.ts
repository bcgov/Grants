import { Component, OnInit } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Router, RouterOutlet } from '@angular/router';
import { ErrorHandlerService } from './core/services/error-handler.service';
import { ToastComponent } from './shared/components/toast/toast.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  title = 'Grants Applicant Portal';
  private authCheckInProgress = false;

  constructor(
    private readonly oidcSecurityService: OidcSecurityService,
    private readonly router: Router,
    private readonly errorHandler: ErrorHandlerService
  ) {}

  ngOnInit(): void {
    // Prevent multiple simultaneous auth checks
    if (this.authCheckInProgress) {
      return;
    }
    
    // Don't check auth on callback, login, or logout pages
    const currentPath = globalThis.location.pathname;
    const authPaths = ['/auth/callback', '/login', '/logout'];
    
    if (authPaths.some(path => currentPath.includes(path))) {
      return;
    }

    // Set flag to prevent multiple auth checks
    this.authCheckInProgress = true;

    // Initialize OIDC authentication check on app startup
    this.oidcSecurityService.checkAuth().subscribe({
      next: (result) => {
        this.authCheckInProgress = false;
        
        if (!result.isAuthenticated) {
          this.router.navigate(['/login']);
        } else {
          // If user is on root path, redirect to app
          if (currentPath === '/' || currentPath === '') {
            this.router.navigate(['/app']);
          }
        }
      },
      error: (error) => {
        this.authCheckInProgress = false;
        
        console.error('App startup auth check failed:', error);
        
        // Use enhanced error handler for authentication errors
        if (this.errorHandler.isAuthStateCorrupted(error)) {
          this.errorHandler.handleAuthError(error).subscribe({
            error: () => {
              // Error handler manages the flow, just ensure we end up at login
              if (!this.router.url.includes('/login')) {
                this.router.navigate(['/login']);
              }
            }
          });
        } else {
          // For non-auth specific errors, clear state manually and redirect
          try {
            const oidcKeys = [
              'angular-auth-oidc-client',
              'oidc.user',
              'oidc.state',
              'oidc.nonce'
            ];
            oidcKeys.forEach(key => {
              localStorage.removeItem(key);
              sessionStorage.removeItem(key);
            });
          } catch (clearError) {
            console.warn('Error clearing auth state:', clearError);
          }
          
          this.router.navigate(['/login']);
        }
      }
    });
  }
}
