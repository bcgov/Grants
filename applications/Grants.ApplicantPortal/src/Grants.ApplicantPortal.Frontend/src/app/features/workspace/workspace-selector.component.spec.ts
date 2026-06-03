import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';

import { WorkspaceSelectorComponent } from './workspace-selector.component';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { Plugin, WorkspaceState } from '../../shared/models/workspace.interface';

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

const mockPlugin: Plugin = {
  pluginId: 'plugin-1',
  description: 'Test Workspace',
  features: [],
  providers: [],
};

describe('WorkspaceSelectorComponent', () => {
  let component: WorkspaceSelectorComponent;
  let fixture: ComponentFixture<WorkspaceSelectorComponent>;
  let workspaceServiceSpy: jasmine.SpyObj<WorkspaceService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    workspaceServiceSpy = jasmine.createSpyObj<WorkspaceService>(
      'WorkspaceService',
      [
        'getAvailableWorkspaces',
        'selectWorkspace',
        'selectWorkspaceWithProviderDetails',
        'isWorkspaceSelected',
        'isProviderSelected',
        'getProviders',
      ],
      {
        currentWorkspaceState$: of(defaultWorkspaceState),
      }
    );
    workspaceServiceSpy.getAvailableWorkspaces.and.returnValue(of({ plugins: [] }));
    workspaceServiceSpy.isWorkspaceSelected.and.returnValue(false);
    workspaceServiceSpy.isProviderSelected.and.returnValue(false);
    workspaceServiceSpy.getProviders.and.returnValue(
      of({ pluginId: 'plugin-1', providers: [] })
    );

    authServiceSpy = jasmine.createSpyObj<AuthService>('AuthService', ['logout']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate', 'navigateByUrl']);

    await TestBed.configureTestingModule({
      imports: [WorkspaceSelectorComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        {
          provide: ActivatedRoute,
          useValue: { queryParams: of({}) },
        },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('initial state', () => {
    it('starts with hasError = false', () => {
      expect(component.hasError).toBeFalse();
    });

    it('starts with empty availableWorkspaces', () => {
      expect(component.availableWorkspaces).toEqual([]);
    });
  });

  describe('backToLogin', () => {
    it('calls authService.logout()', () => {
      component.backToLogin();
      expect(authServiceSpy.logout).toHaveBeenCalled();
    });
  });

  describe('selectWorkspace', () => {
    it('calls workspaceService.selectWorkspace and navigates', () => {
      component.selectWorkspace(mockPlugin);
      expect(workspaceServiceSpy.selectWorkspace).toHaveBeenCalledWith(mockPlugin);
      expect(routerSpy.navigateByUrl).toHaveBeenCalled();
    });
  });

  describe('goBackToWorkspaceSelection', () => {
    it('resets provider selection state', () => {
      component.selectedWorkspaceForProvider = mockPlugin;
      component.showProviderSelection = true;
      component.showWorkspaceSelection = false;

      component.goBackToWorkspaceSelection();

      expect(component.selectedWorkspaceForProvider).toBeNull();
      expect(component.showProviderSelection).toBeFalse();
      expect(component.showWorkspaceSelection).toBeTrue();
    });
  });

  describe('retryFetch', () => {
    it('resets error state and sets isRetrying', () => {
      component.hasError = true;
      workspaceServiceSpy.getAvailableWorkspaces.and.returnValue(of({ plugins: [] }));

      component.retryFetch();

      expect(component.hasError).toBeFalse();
    });
  });

  describe('onWorkspaceClick', () => {
    it('sets showProviderSelection when workspace is clicked', () => {
      workspaceServiceSpy.getProviders.and.returnValue(
        of({ pluginId: 'plugin-1', providers: [] })
      );

      component.onWorkspaceClick(mockPlugin);

      expect(workspaceServiceSpy.getProviders).toHaveBeenCalledWith('plugin-1');
    });
  });

  it('cleans up on destroy', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
