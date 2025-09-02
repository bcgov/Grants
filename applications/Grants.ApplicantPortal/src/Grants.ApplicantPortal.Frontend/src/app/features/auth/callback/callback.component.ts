import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

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

    this.oidcSecurityService
      .checkAuth()
      .subscribe(({ isAuthenticated, userData, accessToken, errorMessage }) => {
        console.log('Auth check result:', {
          isAuthenticated,
          userData,
          accessToken,
          errorMessage,
        });

        if (isAuthenticated) {
          console.log(
            'Authentication successful, redirecting to applicant-info'
          );
          this.router.navigate(['/applicant-info']);
        } else {
          console.log('Authentication failed, redirecting to login');
          this.router.navigate(['/login']);
        }
      });
  }
}
