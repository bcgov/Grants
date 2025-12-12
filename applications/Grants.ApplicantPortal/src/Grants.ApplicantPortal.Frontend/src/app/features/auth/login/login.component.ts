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
    console.log('LoginComponent initialized');
    
    // Check if the user is already authenticated
    this.oidcSecurityService.checkAuth().pipe(take(1)).subscribe({
      next: (result) => {
        this.isChecking = false;
        console.log('Login page auth check:', { 
          isAuthenticated: result.isAuthenticated 
        });
        
        if (result.isAuthenticated) {
          console.log('User already authenticated, redirecting to app');
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

  loginWithBCeID(): void {
    console.log('Initiating BCeID authentication...');

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
      
      console.log('Cleared existing auth storage before login');
    } catch (error) {
      console.warn('Error clearing auth storage:', error);
    }
  }
}
