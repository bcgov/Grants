import { Component, OnInit } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Router, RouterOutlet } from '@angular/router';
import { ErrorHandlerService } from './core/services/error-handler.service';
import { ToastComponent } from './shared/components/toast/toast.component';
import { environment } from '../environments/environment';

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
  ) {
    this.initMatomo();
  }

  ngOnInit(): void {
    console.log('App component initializing...');
    console.log('Current URL:', window.location.href);
    
    // Prevent multiple simultaneous auth checks
    if (this.authCheckInProgress) {
      console.log('Auth check already in progress, skipping');
      return;
    }
    
    // Don't check auth on callback, login, or logout pages
    const currentPath = window.location.pathname;
    const authPaths = ['/auth/callback', '/login', '/logout'];
    
    if (authPaths.some(path => currentPath.includes(path))) {
      console.log('Skipping auth check for auth-related page:', currentPath);
      return;
    }

    // Set flag to prevent multiple auth checks
    this.authCheckInProgress = true;

    // Initialize OIDC authentication check on app startup
    this.oidcSecurityService.checkAuth().subscribe({
      next: (result) => {
        this.authCheckInProgress = false;
        
        console.log('App startup auth check:', {
          isAuthenticated: result.isAuthenticated,
          userData: result.userData ? 'Present' : 'Missing',
          accessToken: result.accessToken ? 'Present' : 'Missing',
          currentPath
        });

        if (!result.isAuthenticated) {
          console.log('User not authenticated on startup, redirecting to login');
          this.router.navigate(['/login']);
        } else {
          console.log('User authenticated on startup');
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
          console.log('Authentication state appears corrupted, using error handler');
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
            console.log('Cleared potentially corrupted auth state');
          } catch (clearError) {
            console.warn('Error clearing auth state:', clearError);
          }
          
          this.router.navigate(['/login']);
        }
      }
    });
  }

  private initMatomo(): void {
    const { enabled, url, siteId } = environment.matomo;
    if (!enabled || !url || !siteId) return;

    const _paq: any[][] = ((window as any)._paq = (window as any)._paq || []);
    _paq.push(['trackPageView']);
    _paq.push(['enableLinkTracking']);
    _paq.push(['setTrackerUrl', url + 'matomo.php']);
    _paq.push(['setSiteId', siteId]);

    const g = document.createElement('script');
    g.async = true;
    g.src = url + 'matomo.js';
    document.head.appendChild(g);
  }
}
