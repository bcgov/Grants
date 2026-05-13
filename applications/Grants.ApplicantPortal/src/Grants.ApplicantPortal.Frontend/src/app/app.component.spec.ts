import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { AppComponent } from './app.component';
import { ErrorHandlerService } from './core/services/error-handler.service';

describe('AppComponent', () => {
  let mockOidcSecurityService: jasmine.SpyObj<OidcSecurityService>;
  let mockErrorHandlerService: jasmine.SpyObj<ErrorHandlerService>;

  beforeEach(async () => {
    mockOidcSecurityService = jasmine.createSpyObj('OidcSecurityService', ['checkAuth']);
    mockOidcSecurityService.checkAuth.and.returnValue(of({ isAuthenticated: false } as any));

    mockErrorHandlerService = jasmine.createSpyObj('ErrorHandlerService', [
      'isAuthStateCorrupted',
      'handleAuthError',
    ]);

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        { provide: OidcSecurityService, useValue: mockOidcSecurityService },
        { provide: ErrorHandlerService, useValue: mockErrorHandlerService },
        provideRouter([]),
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it(`should have the 'Grants Applicant Portal' title`, () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app.title).toEqual('Grants Applicant Portal');
  });

  it('should render the router outlet', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });
});
