import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Observable, of, Subject } from 'rxjs';
import { OidcSecurityService, AuthenticatedResult, LoginResponse } from 'angular-auth-oidc-client';

import { AuthService } from './auth.service';
import { ErrorHandlerService } from './error-handler.service';

function makeAuthenticatedResult(isAuthenticated: boolean): AuthenticatedResult {
  return { isAuthenticated, allConfigsAuthenticated: [] };
}

function makeLoginResponse(isAuthenticated: boolean, accessToken = ''): LoginResponse {
  return { isAuthenticated, accessToken, userData: null, idToken: '' };
}

describe('AuthService', () => {
  let service: AuthService;
  let oidcServiceSpy: jasmine.SpyObj<OidcSecurityService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let errorHandlerSpy: jasmine.SpyObj<ErrorHandlerService>;
  let isAuthenticatedSubject: Subject<AuthenticatedResult>;

  beforeEach(() => {
    isAuthenticatedSubject = new Subject<AuthenticatedResult>();

    oidcServiceSpy = jasmine.createSpyObj<OidcSecurityService>(
      'OidcSecurityService',
      ['authorize', 'forceRefreshSession', 'getAccessToken', 'checkAuth'],
      {
        isAuthenticated$: isAuthenticatedSubject.asObservable(),
        userData$: of(null) as any,
      }
    );
    oidcServiceSpy.getAccessToken.and.returnValue(of('test-token'));
    oidcServiceSpy.forceRefreshSession.and.returnValue(of(makeLoginResponse(true, 'new-token')));
    oidcServiceSpy.checkAuth.and.returnValue(of(makeLoginResponse(true)) as any);

    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    errorHandlerSpy = jasmine.createSpyObj<ErrorHandlerService>('ErrorHandlerService', [
      'isAuthStateCorrupted',
      'handleAuthError',
    ]);
    errorHandlerSpy.isAuthStateCorrupted.and.returnValue(false);

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: OidcSecurityService, useValue: oidcServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ErrorHandlerService, useValue: errorHandlerSpy },
      ],
    });

    service = TestBed.inject(AuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('isAuthenticated$', () => {
    it('emits true when OIDC service reports authenticated', (done) => {
      oidcServiceSpy.getAccessToken.and.returnValue(of('tok'));
      Object.defineProperty(oidcServiceSpy, 'isAuthenticated$', {
        get: () => of(makeAuthenticatedResult(true)),
      });

      // Re-create service after stub update
      const freshService = TestBed.runInInjectionContext(() =>
        new AuthService(oidcServiceSpy, routerSpy, errorHandlerSpy)
      );

      freshService.isAuthenticated$.subscribe((val) => {
        expect(val).toBeTrue();
        done();
      });
    });

    it('emits false when OIDC service reports not authenticated', (done) => {
      Object.defineProperty(oidcServiceSpy, 'isAuthenticated$', {
        get: () => of(makeAuthenticatedResult(false)),
      });

      const freshService = TestBed.runInInjectionContext(() =>
        new AuthService(oidcServiceSpy, routerSpy, errorHandlerSpy)
      );

      freshService.isAuthenticated$.subscribe((val) => {
        expect(val).toBeFalse();
        done();
      });
    });
  });

  describe('login', () => {
    it('calls oidcSecurityService.authorize()', () => {
      service.login();
      expect(oidcServiceSpy.authorize).toHaveBeenCalled();
    });
  });

  describe('logout', () => {
    it('navigates to /logout', () => {
      service.logout();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/logout']);
    });
  });

  describe('getAccessToken', () => {
    it('returns the token from OIDC service', (done) => {
      oidcServiceSpy.getAccessToken.and.returnValue(of('my-token'));

      service.getAccessToken().subscribe((token) => {
        expect(token).toBe('my-token');
        done();
      });
    });

    it('returns empty string when OIDC service throws', (done) => {
      oidcServiceSpy.getAccessToken.and.returnValue(
        new Observable((subscriber) => {
          subscriber.error(new Error('OIDC failure'));
        })
      );

      service.getAccessToken().subscribe((token) => {
        expect(token).toBe('');
        done();
      });
    });
  });

  describe('userData$', () => {
    it('delegates to oidcSecurityService.userData$', () => {
      expect(service.userData$).toBe(oidcServiceSpy.userData$);
    });
  });

  describe('authenticationState$', () => {
    it('emits the internal auth state', (done) => {
      service.authenticationState$.subscribe((state) => {
        expect(state).toBeDefined();
        done();
      });
    });
  });

  describe('refreshSession', () => {
    it('calls forceRefreshSession and returns result', (done) => {
      const mockResult = { isAuthenticated: true, accessToken: 'refreshed-token' };
      oidcServiceSpy.forceRefreshSession.and.returnValue(of(mockResult) as any);

      service.refreshSession().subscribe((result) => {
        expect(result).toEqual(mockResult);
        done();
      });
    });
  });

  describe('checkAuth', () => {
    it('calls oidcSecurityService.checkAuth and emits result', (done) => {
      const mockResult = { isAuthenticated: true };
      oidcServiceSpy.checkAuth.and.returnValue(of(mockResult) as any);

      service.checkAuth().subscribe((result) => {
        expect(result).toEqual(mockResult);
        done();
      });
    });
  });
});
