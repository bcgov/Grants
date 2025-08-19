import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit {
  constructor(
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly oidcSecurityService: OidcSecurityService
  ) {}

  ngOnInit(): void {
    console.log('LoginComponent initialized');
    // Check if the user is already authenticated
    this.oidcSecurityService.checkAuth().subscribe(({ isAuthenticated }) => {
      console.log('Login page auth check:', isAuthenticated);
      if (isAuthenticated) {
        console.log(
          'User already authenticated, redirecting to applicant-info'
        );
        this.router.navigate(['/applicant-info']);
      }
    });
  }

  loginWithBCeID(): void {
    // Implement BCeID authentication redirect
    // This would typically redirect to the BCeID authentication service
    console.log('Redirecting to BCeID authentication...');

    // For development, simulate successful login
    // Replace with actual BCeID integration
    // this.simulateLogin();

    this.authService.login();
  }

  private simulateLogin(): void {
    // Simulate successful authentication
    localStorage.setItem('authToken', 'mock-bceid-token');
    localStorage.setItem(
      'userProfile',
      JSON.stringify({
        id: 'BCEID123',
        organization: 'Sample Organization',
      })
    );

    this.router.navigate(['/applicant-info']);
  }
}
