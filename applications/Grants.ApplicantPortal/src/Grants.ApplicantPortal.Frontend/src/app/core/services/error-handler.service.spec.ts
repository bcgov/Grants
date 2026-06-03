import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { OidcSecurityService } from 'angular-auth-oidc-client';

import { ErrorHandlerService } from './error-handler.service';

describe('ErrorHandlerService', () => {
  let service: ErrorHandlerService;
  let routerSpy: jasmine.SpyObj<Router>;
  let oidcServiceSpy: jasmine.SpyObj<OidcSecurityService>;

  beforeEach(() => {
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    oidcServiceSpy = jasmine.createSpyObj<OidcSecurityService>('OidcSecurityService', [
      'getAccessToken',
    ]);

    TestBed.configureTestingModule({
      providers: [
        ErrorHandlerService,
        { provide: Router, useValue: routerSpy },
        { provide: OidcSecurityService, useValue: oidcServiceSpy },
      ],
    });

    service = TestBed.inject(ErrorHandlerService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('handleError', () => {
    it('navigates to /login on 401', (done) => {
      const error = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
      service.handleError(error).subscribe({
        error: () => {
          expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
          done();
        },
      });
    });

    it('returns throwError observable', (done) => {
      const error = new HttpErrorResponse({ status: 500, statusText: 'Server Error' });
      service.handleError(error).subscribe({
        error: (err: HttpErrorResponse) => {
          expect(err.status).toBe(500);
          done();
        },
      });
    });

    it('does not navigate for non-401 errors', (done) => {
      const error = new HttpErrorResponse({ status: 500 });
      service.handleError(error).subscribe({
        error: () => {
          expect(routerSpy.navigate).not.toHaveBeenCalled();
          done();
        },
      });
    });
  });

  describe('isAuthError', () => {
    it('returns true for 401 status', () => {
      const error = new HttpErrorResponse({ status: 401 });
      expect(service.isAuthError(error)).toBeTrue();
    });

    it('returns true for 403 status', () => {
      const error = new HttpErrorResponse({ status: 403 });
      expect(service.isAuthError(error)).toBeTrue();
    });

    it('returns false for 500 status', () => {
      const error = new HttpErrorResponse({ status: 500 });
      expect(service.isAuthError(error)).toBeFalse();
    });
  });

  describe('isNetworkError', () => {
    it('returns true for status 0', () => {
      const error = new HttpErrorResponse({ status: 0 });
      expect(service.isNetworkError(error)).toBeTrue();
    });

    it('returns false for status 500', () => {
      const error = new HttpErrorResponse({ status: 500 });
      // navigator.onLine is true in test environment
      expect(service.isNetworkError(error)).toBeFalse();
    });
  });

  describe('isAuthStateCorrupted', () => {
    it('returns true for nonce validation errors', () => {
      const error = new Error('Validate_id_token_nonce failed');
      expect(service.isAuthStateCorrupted(error)).toBeTrue();
    });

    it('returns true for token validation errors', () => {
      const error = new Error('authCallback token(s) invalid');
      expect(service.isAuthStateCorrupted(error)).toBeTrue();
    });

    it('returns true for silent renew errors', () => {
      const error = new Error('silent renew failed');
      expect(service.isAuthStateCorrupted(error)).toBeTrue();
    });

    it('returns false for generic errors', () => {
      const error = new Error('something went wrong');
      expect(service.isAuthStateCorrupted(error)).toBeFalse();
    });
  });

  describe('handleAuthError', () => {
    it('redirects to /login and throws for nonce validation error', (done) => {
      const error = new Error('Validate_id_token_nonce failed');
      service.handleAuthError(error).subscribe({
        error: (err: Error) => {
          expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
          expect(err.message).toContain('nonce validation failed');
          done();
        },
      });
    });

    it('redirects to /login and throws for token validation error', (done) => {
      const error = new Error('token(s) validation failed');
      service.handleAuthError(error).subscribe({
        error: (err: Error) => {
          expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
          expect(err.message).toContain('token validation failed');
          done();
        },
      });
    });

    it('throws for generic auth errors', (done) => {
      const error = new Error('some auth error');
      service.handleAuthError(error).subscribe({
        error: (err: Error) => {
          expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
          expect(err).toBe(error);
          done();
        },
      });
    });
  });
});
