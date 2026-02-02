import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService {
  constructor(
    private router: Router,
    private oidcSecurityService: OidcSecurityService
  ) {}

  /**
   * Handles HTTP errors and determines appropriate action
   */
  handleError(error: HttpErrorResponse): Observable<never> {
    console.error('HTTP Error occurred:', error);

    switch (error.status) {
      case 401:
        console.log('Unauthorized - redirecting to login');
        this.router.navigate(['/login']);
        break;
      case 403:
        console.log('Forbidden - user lacks permissions');
        break;
      case 500:
        console.log('Server error - please try again later');
        break;
      case 0:
        console.log('Network error - check internet connection');
        break;
      default:
        console.log(`HTTP Error ${error.status}: ${error.message}`);
        break;
    }

    return throwError(() => error);
  }

  /**
   * Checks if error is authentication-related
   */
  isAuthError(error: HttpErrorResponse): boolean {
    return error.status === 401 || error.status === 403;
  }

  /**
   * Checks if error is a network error
   */
  isNetworkError(error: HttpErrorResponse): boolean {
    return error.status === 0 || !navigator.onLine;
  }

  /**
   * Handles OIDC authentication errors specifically
   */
  handleAuthError(error: any): Observable<never> {
    console.error('Authentication error occurred:', error);

    // Check if it's a nonce validation error
    if (this.isNonceValidationError(error)) {
      console.log('Nonce validation failed - clearing auth state and redirecting');
      this.clearAuthState();
      this.router.navigate(['/login']);
      return throwError(() => new Error('Authentication nonce validation failed'));
    }

    // Check if it's a token validation error
    if (this.isTokenValidationError(error)) {
      console.log('Token validation failed - clearing auth state and redirecting');
      this.clearAuthState();
      this.router.navigate(['/login']);
      return throwError(() => new Error('Authentication token validation failed'));
    }

    // Check if it's a silent renew error
    if (this.isSilentRenewError(error)) {
      console.log('Silent renew failed - attempting manual refresh');
      return this.handleSilentRenewFailure();
    }

    // Generic auth error
    console.log('Generic authentication error - redirecting to login');
    this.clearAuthState();
    this.router.navigate(['/login']);
    return throwError(() => error);
  }

  /**
   * Checks if error is related to nonce validation
   */
  private isNonceValidationError(error: any): boolean {
    const errorMessage = error?.message || error?.toString() || '';
    return errorMessage.includes('nonce') || 
           errorMessage.includes('Validate_id_token_nonce failed') ||
           errorMessage.includes('incorrect nonce');
  }

  /**
   * Checks if error is related to token validation
   */
  private isTokenValidationError(error: any): boolean {
    const errorMessage = error?.message || error?.toString() || '';
    return errorMessage.includes('token(s) validation failed') ||
           errorMessage.includes('authCallback token(s) invalid') ||
           errorMessage.includes('authorizedCallback, token(s) validation failed');
  }

  /**
   * Checks if error is related to silent renew
   */
  private isSilentRenewError(error: any): boolean {
    const errorMessage = error?.message || error?.toString() || '';
    return errorMessage.includes('silent renew failed') ||
           errorMessage.includes('Silent renew');
  }

  /**
   * Handles silent renew failure by attempting manual refresh or redirecting to login
   */
  private handleSilentRenewFailure(): Observable<never> {
    console.log('Attempting to handle silent renew failure');
    
    // Clear potentially corrupted auth state
    this.clearAuthState();
    
    // Force a fresh login
    setTimeout(() => {
      this.router.navigate(['/login']);
    }, 100);
    
    return throwError(() => new Error('Silent token renewal failed - please login again'));
  }

  /**
   * Clears all authentication-related storage
   */
  private clearAuthState(): void {
    try {
      // Clear OIDC-related storage keys with more comprehensive patterns
      const oidcKeys = [
        'angular-auth-oidc-client',
        'oidc.user',
        'oidc.state',
        'oidc.nonce',
        'oidc.code_verifier',
        'oidc.session_state',
        'oidc.access_token',
        'oidc.id_token',
        'oidc.refresh_token',
        'oidc.authorizationState',
        'oidc.authorizationResult',
        'oidc.silentRenew',
        'authzData',
        'authnResult'
      ];
      
      // Clear direct keys
      oidcKeys.forEach(key => {
        localStorage.removeItem(key);
        sessionStorage.removeItem(key);
      });
      
      // Clear keys with config ID prefixes (be more thorough)
      const configIds = [
        '0-grants-portal-5361_',
        'grants-portal-',
        'oidc_',
        'auth_'
      ];
      
      configIds.forEach(prefix => {
        oidcKeys.forEach(key => {
          localStorage.removeItem(`${prefix}${key}`);
          sessionStorage.removeItem(`${prefix}${key}`);
        });
      });
      
      console.log('Successfully cleared authentication state');
    } catch (clearError) {
      console.warn('Error clearing authentication state:', clearError);
    }
  }

  /**
   * Checks if the current error requires authentication state cleanup
   */
  isAuthStateCorrupted(error: any): boolean {
    return this.isNonceValidationError(error) || 
           this.isTokenValidationError(error) ||
           this.isSilentRenewError(error);
  }
}