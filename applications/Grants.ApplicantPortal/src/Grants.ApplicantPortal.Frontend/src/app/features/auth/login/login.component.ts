import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit {
  isChecking = true;

  constructor(
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly oidcSecurityService: OidcSecurityService
  ) {}

  ngOnInit(): void {
    // Check if we arrived here due to a 401 API error redirect.
    // If so, the user's OIDC session is still valid but the API is rejecting
    // requests, so auto-redirecting back to /app would recreate the loop.
    const redirectReason = sessionStorage.getItem('auth_redirect_reason');
    if (redirectReason === '401') {
      sessionStorage.removeItem('auth_redirect_reason');
      console.warn('LoginComponent: arrived via 401 redirect \u2014 NOT auto-redirecting to avoid loop');
      this.isChecking = false;
      return;
    }
    
    // Check if the user is already authenticated
    this.oidcSecurityService.checkAuth().pipe(take(1)).subscribe({
      next: (result) => {
        this.isChecking = false;
        
        if (result.isAuthenticated) {
          this.router.navigate(['/app']);
        }
      },
      error: (error) => {
        this.isChecking = false;
        console.error('Auth check error on login page:', error);
        // Clear any existing auth state
        localStorage.clear();
        sessionStorage.clear();
      }
    });
  }

  loginRequest(): void { 
    // Clear any existing authentication state that might cause conflicts
    this.clearAuthStorage();

    // Use OidcSecurityService directly instead of AuthService
    this.oidcSecurityService.authorize();
  }

  private clearAuthStorage(): void {
    try {
      // Clear specific OIDC keys that might cause state conflicts
      const oidcKeys = [
        'angular-auth-oidc-client',
        'oidc.user',
        'oidc.id_token',
        'oidc.access_token', 
        'oidc.refresh_token',
        'oidc.state',
        'oidc.nonce'
      ];
      
      oidcKeys.forEach(key => {
        localStorage.removeItem(key);
        sessionStorage.removeItem(key);
      });
    } catch (error) {
      console.warn('Error clearing auth storage:', error);
    }
  }
}
