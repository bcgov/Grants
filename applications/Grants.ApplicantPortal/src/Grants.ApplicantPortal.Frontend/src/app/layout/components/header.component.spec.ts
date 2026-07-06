import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Subject, of } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';

import { HeaderComponent } from './header.component';
import { AuthService } from '../../core/services/auth.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { Provider, WorkspaceState } from '../../shared/models/workspace.interface';

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

  describe('isSingleWorkspace', () => {
    it('returns true when availableWorkspaces has exactly 1 item', () => {
      component.availableWorkspaces = [{ pluginId: 'p1', description: 'WS One', features: [], providers: [] }];
      expect(component.isSingleWorkspace).toBeTrue();
    });

    it('returns false when availableWorkspaces has 2 or more items', () => {
      component.availableWorkspaces = [
        { pluginId: 'p1', description: 'WS One', features: [], providers: [] },
        { pluginId: 'p2', description: 'WS Two', features: [], providers: [] },
      ];
      expect(component.isSingleWorkspace).toBeFalse();
    });

    it('returns false when availableWorkspaces is empty', () => {
      component.availableWorkspaces = [];
      expect(component.isSingleWorkspace).toBeFalse();
    });
  });

  describe('displayText', () => {
    it('returns "No Workspace" when no workspace is selected', () => {
      component.selectedWorkspace = null;
      expect(component.displayText).toBe('No Workspace');
    });

    it('returns workspace description when no provider name (multi-workspace)', () => {
      component.availableWorkspaces = [];
      component.selectedWorkspace = {
        pluginId: 'p1',
        description: 'Test WS',
        features: [],
        providers: [],
      };
      component.selectedProviderName = null;
      expect(component.displayText).toBe('Test WS');
    });

    it('returns "workspace > provider" when provider name is present (multi-workspace)', () => {
      component.availableWorkspaces = [];
      component.selectedWorkspace = {
        pluginId: 'p1',
        description: 'Test WS',
        features: [],
        providers: [],
      };
      component.selectedProviderName = 'Provider One';
      expect(component.displayText).toBe('Test WS > Provider One');
    });

    it('returns just the provider name in single-workspace mode', () => {
      component.availableWorkspaces = [{ pluginId: 'p1', description: 'Test WS', features: [], providers: [] }];
      component.selectedWorkspace = { pluginId: 'p1', description: 'Test WS', features: [], providers: [] };
      component.selectedProviderName = 'Provider One';
      expect(component.displayText).toBe('Provider One');
    });

    it('returns "Select Program" in single-workspace mode when no provider is selected', () => {
      component.availableWorkspaces = [{ pluginId: 'p1', description: 'Test WS', features: [], providers: [] }];
      component.selectedWorkspace = { pluginId: 'p1', description: 'Test WS', features: [], providers: [] };
      component.selectedProviderName = null;
      expect(component.displayText).toBe('Select Program');
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

  describe('provider display label (dropdown)', () => {
    beforeEach(() => {
      component.selectedWorkspace = { pluginId: 'p1', description: 'WS', features: [], providers: [] };
    });

    it('shows displayName when present', () => {
      component.currentProviders = [
        { id: 'a', name: 'internal-a', displayName: 'Program A' },
        { id: 'b', name: 'internal-b', displayName: 'Program B' },
      ];
      fixture.detectChanges();
      const spans: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.provider-item span');
      expect(spans[0].textContent?.trim()).toBe('Program A');
      expect(spans[1].textContent?.trim()).toBe('Program B');
    });

    it('falls back to name when displayName is absent', () => {
      component.currentProviders = [
        { id: 'a', name: 'internal-a' },
        { id: 'b', name: 'internal-b' },
      ];
      fixture.detectChanges();
      const spans: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.provider-item span');
      expect(spans[0].textContent?.trim()).toBe('internal-a');
      expect(spans[1].textContent?.trim()).toBe('internal-b');
    });
  });

  describe('selectProviderById', () => {
    beforeEach(() => {
      component.selectedWorkspace = { pluginId: 'p1', description: 'WS', features: [], providers: [] };
      component.selectedProvider = 'old-id';
    });

    it('calls setTenantEmail with the provider defaultFromAddress', () => {
      const provider: Provider = { id: 'new-id', name: 'internal', defaultFromAddress: 'from@example.com' };
      component.selectProviderById(provider);
      expect(workspaceServiceSpy.setTenantEmail).toHaveBeenCalledWith('from@example.com');
    });

    it('calls setTenantEmail with null when defaultFromAddress is absent', () => {
      const provider: Provider = { id: 'new-id', name: 'internal' };
      component.selectProviderById(provider);
      expect(workspaceServiceSpy.setTenantEmail).toHaveBeenCalledWith(null);
    });
  });

  describe('updateTenantEmail (private)', () => {
    it('sets tenant email from defaultFromAddress of the currently selected provider', () => {
      component.currentProviders = [
        { id: 'a', name: 'internal-a', defaultFromAddress: 'a@example.com' },
        { id: 'b', name: 'internal-b', defaultFromAddress: 'b@example.com' },
      ];
      component.selectedProvider = 'b';
      (component as any).updateTenantEmail();
      expect(workspaceServiceSpy.setTenantEmail).toHaveBeenCalledWith('b@example.com');
    });

    it('sets tenant email to null when the selected provider has no defaultFromAddress', () => {
      component.currentProviders = [{ id: 'a', name: 'internal-a' }];
      component.selectedProvider = 'a';
      (component as any).updateTenantEmail();
      expect(workspaceServiceSpy.setTenantEmail).toHaveBeenCalledWith(null);
    });
  });
});
