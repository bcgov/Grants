import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Subject, of } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';

import { HeaderComponent } from './header.component';
import { AuthService } from '../../core/services/auth.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { WorkspaceState } from '../../shared/models/workspace.interface';

const defaultWorkspaceState: WorkspaceState = {
  selectedWorkspace: null,
  selectedProvider: null,
  selectedProviderName: null,
  availableWorkspaces: [],
  isWorkspaceSelected: false,
  isProviderSelected: false,
  hasMultipleOrgs: false,
  applicantId: null,
  applicantRefId: null,
  applicantName: '',
  orgNumber: '',
  orgName: '',
  tenantEmail: null,
};

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let workspaceServiceSpy: jasmine.SpyObj<WorkspaceService>;
  let routerSpy: { url: string; events: Subject<any>; navigate: jasmine.Spy };

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj<AuthService>('AuthService', ['logout']);

    workspaceServiceSpy = jasmine.createSpyObj<WorkspaceService>(
      'WorkspaceService',
      [
        'getProviders',
        'selectWorkspaceWithProviderDetails',
        'setTenantEmail',
        'clearSelection',
        'selectWorkspace',
      ],
      {
        currentWorkspaceState$: of(defaultWorkspaceState),
        isChangingWorkspace$: of(false),
      }
    );
    workspaceServiceSpy.getProviders.and.returnValue(of({ pluginId: 'p1', providers: [] }));

    routerSpy = {
      url: '/app/applicant-info',
      events: new Subject<any>(),
      navigate: jasmine.createSpy('navigate'),
    };

    await TestBed.configureTestingModule({
      imports: [HeaderComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceSpy },
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
        { provide: Router, useValue: routerSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('sets pageTitle to "Applicant Info" for applicant-info route', () => {
    routerSpy.url = '/app/applicant-info';
    (component as any).updateTitle();
    expect(component.pageTitle).toBe('Applicant Info');
  });

  it('sets pageTitle to "Payments" for payments route', () => {
    routerSpy.url = '/app/payments';
    (component as any).updateTitle();
    expect(component.pageTitle).toBe('Payments');
  });

  it('calls authService.logout() when onLogout is called', () => {
    const event = new MouseEvent('click');
    spyOn(event, 'preventDefault');
    component.onLogout(event);
    expect(event.preventDefault).toHaveBeenCalled();
    expect(authServiceSpy.logout).toHaveBeenCalled();
  });

  it('calls workspaceService.clearSelection and navigates on changeWorkspace', () => {
    component.changeWorkspace();
    expect(workspaceServiceSpy.clearSelection).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/workspace-selector']);
  });

  describe('displayText', () => {
    it('returns "No Workspace" when no workspace is selected', () => {
      component.selectedWorkspace = null;
      expect(component.displayText).toBe('No Workspace');
    });

    it('returns workspace description when no provider name', () => {
      component.selectedWorkspace = {
        pluginId: 'p1',
        description: 'Test WS',
        features: [],
        providers: [],
      };
      component.selectedProviderName = null;
      expect(component.displayText).toBe('Test WS');
    });

    it('returns "workspace > provider" when provider name is present', () => {
      component.selectedWorkspace = {
        pluginId: 'p1',
        description: 'Test WS',
        features: [],
        providers: [],
      };
      component.selectedProviderName = 'Provider One';
      expect(component.displayText).toBe('Test WS > Provider One');
    });
  });

  describe('showOrgInfo', () => {
    it('returns false when all org fields are empty', () => {
      component.applicantRefId = '';
      component.applicantName = '';
      component.orgNumber = '';
      component.orgName = '';
      expect(component.showOrgInfo).toBeFalse();
    });

    it('returns true when applicantRefId is set', () => {
      component.applicantRefId = 'REF-123';
      expect(component.showOrgInfo).toBeTrue();
    });
  });

  it('cleans up subscriptions on destroy', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
