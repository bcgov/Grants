import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { OidcSecurityService } from 'angular-auth-oidc-client';

import { AuthInterceptor } from './auth.interceptor';

describe('AuthInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let oidcServiceSpy: jasmine.SpyObj<OidcSecurityService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    oidcServiceSpy = jasmine.createSpyObj<OidcSecurityService>('OidcSecurityService', [
      'getAccessToken',
      'forceRefreshSession',
    ]);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: OidcSecurityService, useValue: oidcServiceSpy },
        { provide: Router, useValue: routerSpy },
        {
          provide: HTTP_INTERCEPTORS,
          useClass: AuthInterceptor,
          multi: true,
        },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('attaches Authorization: Bearer header when a token is present', () => {
    oidcServiceSpy.getAccessToken.and.returnValue(of('test-access-token'));

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-access-token');
    req.flush({});
  });

  it('passes request without Authorization header when token is empty', () => {
    oidcServiceSpy.getAccessToken.and.returnValue(of(''));

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('skips token attachment for /auth/ URLs', () => {
    oidcServiceSpy.getAccessToken.and.returnValue(of('should-not-attach'));

    httpClient.get('/auth/token').subscribe();

    const req = httpMock.expectOne('/auth/token');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('skips token attachment for /healthz URLs', () => {
    oidcServiceSpy.getAccessToken.and.returnValue(of('should-not-attach'));

    httpClient.get('/healthz').subscribe();

    const req = httpMock.expectOne('/healthz');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('skips token attachment for /.well-known/ URLs', () => {
    oidcServiceSpy.getAccessToken.and.returnValue(of('should-not-attach'));

    httpClient.get('/.well-known/openid-configuration').subscribe();

    const req = httpMock.expectOne('/.well-known/openid-configuration');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });
});
