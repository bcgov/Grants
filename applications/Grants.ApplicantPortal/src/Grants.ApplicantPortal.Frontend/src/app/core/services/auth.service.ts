import { Injectable } from '@angular/core';
import {
  OidcClientNotification,
  OidcSecurityService,
  OpenIdConfiguration,
} from 'angular-auth-oidc-client';
import { Observable, map } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private readonly oidcSecurityService: OidcSecurityService) {}

  get isAuthenticated$(): Observable<boolean> {
    return this.oidcSecurityService.isAuthenticated$.pipe(
      map((result) => {
        console.log('AuthService - OIDC result:', result);
        console.log('AuthService - isAuthenticated:', result.isAuthenticated);
        return result.isAuthenticated;
      })
    );
  }

  get userData$() {
    return this.oidcSecurityService.userData$;
  }

  login(): void {
    this.oidcSecurityService.authorize();
  }

  logout(): void {
    this.oidcSecurityService.logoff();
  }

  getAccessToken(): Observable<string> {
    return this.oidcSecurityService.getAccessToken();
  }
}
