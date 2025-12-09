import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import { Observable, switchMap, take } from 'rxjs';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private oidcSecurityService: OidcSecurityService) {}

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
        if (token) {
          // Clone the request and add the authorization header
          const authRequest = request.clone({
            setHeaders: {
              Authorization: `Bearer ${token}`,
            },
          });
          return next.handle(authRequest);
        }
        
        // If no token, proceed with original request
        return next.handle(request);
      })
    );
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