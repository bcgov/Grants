import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, switchMap, take, catchError, throwError, BehaviorSubject, filter } from 'rxjs';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

  /**
   * Tracks the last time we redirected to /login due to a 401.
   * Prevents rapid-fire redirect loops when the API keeps returning 401
   * but the OIDC session is still valid.
   */
  private lastAuthFailureRedirect = 0;
  private static readonly AUTH_FAILURE_COOLDOWN_MS = 10_000; // 10 seconds

  constructor(
    private oidcSecurityService: OidcSecurityService,
    private router: Router
  ) {}

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    // Skip token attachment for external URLs or auth-related endpoints
    if (this.shouldSkipToken(request.url)) {
      return next.handle(request);
    }

    // Get the access token and add it to the request headers
    return this.oidcSecurityService.getAccessToken().pipe(
      take(1),
      switchMap((token: string) => {
        const authRequest = this.addTokenHeader(request, token);
        return next.handle(authRequest).pipe(
          catchError((error: HttpErrorResponse) => {
            if (error.status === 431) {
              console.error('HTTP 431 - Request headers too large. This should not happen with proper Node.js configuration.');
              // Simple cleanup and redirect - the configuration should prevent this
              localStorage.clear();
              sessionStorage.clear();
              this.router.navigate(['/login']);
              return throwError(() => new Error('Request headers too large - please try logging in again'));
            }
            if (error.status === 401 && token) {
              return this.handle401Error(request, next);
            }
            return throwError(() => error);
          })
        );
      })
    );
  }

  private addTokenHeader(request: HttpRequest<any>, token: string): HttpRequest<any> {
    if (token) {
      return request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      });
    }
    return request;
  }

  /**
   * Safely redirects to /login with a cooldown to prevent rapid-fire redirect loops.
   * Sets a sessionStorage flag so the login page knows this was a 401 redirect
   * and can avoid auto-redirecting back into the app.
   */
  private safeRedirectToLogin(): void {
    const now = Date.now();
    if (now - this.lastAuthFailureRedirect < AuthInterceptor.AUTH_FAILURE_COOLDOWN_MS) {
      console.warn('Auth interceptor: skipping /login redirect — cooldown active (prevents redirect loop)');
      return;
    }
    this.lastAuthFailureRedirect = now;
    sessionStorage.setItem('auth_redirect_reason', '401');
    this.router.navigate(['/login']);
  }

  private handle401Error(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      console.log('401 error detected, attempting token refresh');
      
      return this.oidcSecurityService.forceRefreshSession().pipe(
        switchMap((result) => {
          this.isRefreshing = false;
          console.log('Token refresh result:', result);
          
          if (result.isAuthenticated && result.accessToken) {
            this.refreshTokenSubject.next(result.accessToken);
            // Retry the original request with new token
            const authRequest = this.addTokenHeader(request, result.accessToken);
            return next.handle(authRequest).pipe(
              catchError((retryError: HttpErrorResponse) => {
                // If the retried request ALSO returns 401, do NOT attempt another
                // refresh cycle — the problem is server-side, not token-related.
                if (retryError.status === 401) {
                  console.error('Retried request still returned 401 after token refresh — API authorization issue, not a token issue.');
                  this.safeRedirectToLogin();
                }
                return throwError(() => retryError);
              })
            );
          } else {
            // Refresh failed, redirect to login
            console.log('Token refresh failed, redirecting to login');
            this.safeRedirectToLogin();
            return throwError(() => new Error('Token refresh failed'));
          }
        }),
        catchError((error) => {
          this.isRefreshing = false;
          console.error('Force refresh session error:', error);
          this.safeRedirectToLogin();
          return throwError(() => error);
        })
      );
    } else {
      // If already refreshing, wait for a non-null token from the refresh subject
      return this.refreshTokenSubject.pipe(
        // Skip the initial null value — wait for the refresh to complete
        filter((token) => token !== null),
        take(1),
        switchMap((token) => {
          const authRequest = this.addTokenHeader(request, token);
          return next.handle(authRequest);
        })
      );
    }
  }

  private shouldSkipToken(url: string): boolean {
    // Skip token for external URLs, auth endpoints, or health checks
    const skipUrls = [
      'https://www.example.com',
      '/auth/',
      '/.well-known/',
      '/connect/',
      '/healthz'
    ];
    
    return skipUrls.some(skipUrl => url.includes(skipUrl));
  }
}