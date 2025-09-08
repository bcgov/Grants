import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-auth-callback',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './callback.component.html',
  styleUrls: ['./callback.component.scss'],
})
export class CallbackComponent implements OnInit {
  constructor(
    private readonly oidcSecurityService: OidcSecurityService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    console.log('CallbackComponent initialized');
    console.log('Current URL:', window.location.href);

    // Handle the callback from the identity provider
    this.oidcSecurityService
      .checkAuth()
      .pipe(take(1))
      .subscribe({
        next: ({ isAuthenticated, userData, accessToken, errorMessage }) => {
          console.log('Auth check result:', {
            isAuthenticated,
            userData,
            accessToken: accessToken ? 'Present' : 'Missing',
            errorMessage,
          });

          if (isAuthenticated) {
            console.log(
              'Authentication successful, redirecting to applicant-info'
            );
            this.router.navigate(['/applicant-info']);
          } else {
            console.log('Authentication failed:', errorMessage);
            this.router.navigate(['/login']);
          }
        },
        error: (error) => {
          console.error('Auth check error:', error);
          this.router.navigate(['/login']);
        },
      });
  }
}
