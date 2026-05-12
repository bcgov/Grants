import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { of } from 'rxjs';

import { WorkspaceService } from './workspace.service';
import { ApplicantInfoService } from './applicant-info.service';
import { Plugin, Provider, WorkspaceState } from '../../shared/models/workspace.interface';
import { environment } from '../../../environments/environment';

const BASE = environment.apiUrl;

const mockPlugin: Plugin = {
  pluginId: 'plugin-1',
  description: 'Test Workspace',
  features: [],
  providers: [],
};

const mockProvider: Provider = {
  id: 'prov-1',
  name: 'Provider One',
};

describe('WorkspaceService', () => {
  let service: WorkspaceService;
  let httpMock: HttpTestingController;
  let applicantInfoServiceSpy: jasmine.SpyObj<ApplicantInfoService>;

  beforeEach(() => {
    applicantInfoServiceSpy = jasmine.createSpyObj<ApplicantInfoService>(
      'ApplicantInfoService',
      ['getOrganizationInfo']
    );
    // Return empty organizations by default so setupOrgDataLoading doesn't cause HTTP requests
    applicantInfoServiceSpy.getOrganizationInfo.and.returnValue(
      of({ organizationsData: [], organizationData: null })
    );

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        WorkspaceService,
        { provide: ApplicantInfoService, useValue: applicantInfoServiceSpy },
      ],
    });

    service = TestBed.inject(WorkspaceService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('initial state', () => {
    it('starts with no workspace selected', (done) => {
      service.currentWorkspaceState$.subscribe((state: WorkspaceState) => {
        expect(state.isWorkspaceSelected).toBeFalse();
        expect(state.selectedWorkspace).toBeNull();
        done();
      });
    });

    it('starts with no provider selected', (done) => {
      service.currentWorkspaceState$.subscribe((state: WorkspaceState) => {
        expect(state.isProviderSelected).toBeFalse();
        expect(state.selectedProvider).toBeNull();
        done();
      });
    });
  });

  describe('selectWorkspace', () => {
    it('updates state with the selected workspace', (done) => {
      service.selectWorkspace(mockPlugin);

      service.currentWorkspaceState$.subscribe((state: WorkspaceState) => {
        if (state.selectedWorkspace) {
          expect(state.selectedWorkspace.pluginId).toBe('plugin-1');
          expect(state.isWorkspaceSelected).toBeTrue();
          done();
        }
      });
    });

    it('stores selection in localStorage', () => {
      service.selectWorkspace(mockPlugin, 'prov-1', 'Provider One');

      const stored = localStorage.getItem('selectedWorkspace');
      expect(stored).not.toBeNull();
      const parsed = JSON.parse(stored!);
      expect(parsed.workspace.pluginId).toBe('plugin-1');
    });

    it('marks provider as selected when provider is supplied', (done) => {
      service.selectWorkspace(mockPlugin, 'prov-1');

      service.currentWorkspaceState$.subscribe((state: WorkspaceState) => {
        if (state.isWorkspaceSelected) {
          expect(state.isProviderSelected).toBeTrue();
          expect(state.selectedProvider).toBe('prov-1');
          done();
        }
      });
    });
  });

  describe('selectWorkspaceWithProviderDetails', () => {
    it('sets both workspace and provider with name', (done) => {
      service.selectWorkspaceWithProviderDetails(mockPlugin, mockProvider);

      service.currentWorkspaceState$.subscribe((state: WorkspaceState) => {
        if (state.isProviderSelected) {
          expect(state.selectedProvider).toBe('prov-1');
          expect(state.selectedProviderName).toBe('Provider One');
          done();
        }
      });
    });
  });

  describe('clearWorkspace', () => {
    it('resets state to defaults', (done) => {
      service.selectWorkspace(mockPlugin, 'prov-1');
      service.clearWorkspace();

      service.currentWorkspaceState$.subscribe((state: WorkspaceState) => {
        expect(state.isWorkspaceSelected).toBeFalse();
        expect(state.selectedWorkspace).toBeNull();
        expect(state.selectedProvider).toBeNull();
        done();
      });
    });

    it('removes selection from localStorage', () => {
      localStorage.setItem('selectedWorkspace', JSON.stringify({ workspace: mockPlugin }));
      service.clearWorkspace();
      expect(localStorage.getItem('selectedWorkspace')).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('clears workspace/provider but keeps availableWorkspaces', (done) => {
      service.selectWorkspace(mockPlugin, 'prov-1');

      // Prime availableWorkspaces via getAvailableWorkspaces
      service.getAvailableWorkspaces().subscribe();
      httpMock.expectOne(`${BASE}/System/plugins`).flush({ plugins: [mockPlugin] });

      service.clearSelection();

      service.currentWorkspaceState$.subscribe((state: WorkspaceState) => {
        expect(state.isWorkspaceSelected).toBeFalse();
        expect(state.selectedWorkspace).toBeNull();
        expect(state.availableWorkspaces.length).toBe(1);
        done();
      });
    });
  });

  describe('isWorkspaceSelectionRequired', () => {
    it('returns true when no workspace is selected', () => {
      expect(service.isWorkspaceSelectionRequired()).toBeTrue();
    });

    it('returns false when workspace and provider are both selected', () => {
      service.selectWorkspace(mockPlugin, 'prov-1');
      expect(service.isWorkspaceSelectionRequired()).toBeFalse();
    });
  });

  describe('isWorkspaceSelected', () => {
    it('returns false initially', () => {
      expect(service.isWorkspaceSelected()).toBeFalse();
    });

    it('returns true after selecting a workspace', () => {
      service.selectWorkspace(mockPlugin);
      expect(service.isWorkspaceSelected()).toBeTrue();
    });
  });

  describe('isProviderSelected', () => {
    it('returns false initially', () => {
      expect(service.isProviderSelected()).toBeFalse();
    });

    it('returns true after selecting workspace with provider', () => {
      service.selectWorkspace(mockPlugin, 'prov-1');
      expect(service.isProviderSelected()).toBeTrue();
    });
  });

  describe('getAvailableWorkspaces', () => {
    it('sends GET request to /System/plugins', () => {
      service.getAvailableWorkspaces().subscribe();

      const req = httpMock.expectOne(`${BASE}/System/plugins`);
      expect(req.request.method).toBe('GET');
      req.flush({ plugins: [] });
    });

    it('updates availableWorkspaces in state', (done) => {
      service.getAvailableWorkspaces().subscribe(() => {
        service.currentWorkspaceState$.subscribe((state) => {
          expect(state.availableWorkspaces.length).toBe(1);
          expect(state.availableWorkspaces[0].pluginId).toBe('plugin-1');
          done();
        });
      });

      httpMock.expectOne(`${BASE}/System/plugins`).flush({ plugins: [mockPlugin] });
    });
  });

  describe('getProviders', () => {
    it('sends GET request to /Plugins/:pluginId/providers', () => {
      service.getProviders('plugin-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Plugins/plugin-1/providers`);
      expect(req.request.method).toBe('GET');
      req.flush({ pluginId: 'plugin-1', providers: [] });
    });

    it('returns empty providers on error (catches silently)', (done) => {
      service.getProviders('plugin-1').subscribe((res) => {
        expect(res.providers).toEqual([]);
        done();
      });

      httpMock.expectOne(`${BASE}/Plugins/plugin-1/providers`).flush(
        'error',
        { status: 500, statusText: 'Server Error' }
      );
    });
  });

  describe('getWorkspaceById', () => {
    it('returns undefined when no workspaces are loaded', () => {
      expect(service.getWorkspaceById('plugin-1')).toBeUndefined();
    });

    it('returns the workspace when found', (done) => {
      service.getAvailableWorkspaces().subscribe(() => {
        const found = service.getWorkspaceById('plugin-1');
        expect(found?.pluginId).toBe('plugin-1');
        done();
      });

      httpMock.expectOne(`${BASE}/System/plugins`).flush({ plugins: [mockPlugin] });
    });
  });

  describe('restoreWorkspaceFromStorage', () => {
    it('restores workspace from localStorage', (done) => {
      localStorage.setItem(
        'selectedWorkspace',
        JSON.stringify({ workspace: mockPlugin, provider: 'prov-1', providerName: 'Provider One' })
      );

      service.restoreWorkspaceFromStorage();

      service.currentWorkspaceState$.subscribe((state) => {
        if (state.isWorkspaceSelected) {
          expect(state.selectedWorkspace?.pluginId).toBe('plugin-1');
          expect(state.selectedProvider).toBe('prov-1');
          done();
        }
      });
    });

    it('handles invalid JSON in localStorage gracefully', () => {
      localStorage.setItem('selectedWorkspace', 'not-valid-json');
      expect(() => service.restoreWorkspaceFromStorage()).not.toThrow();
      expect(localStorage.getItem('selectedWorkspace')).toBeNull();
    });
  });

  describe('currentPluginId', () => {
    it('returns null when no workspace is selected', () => {
      expect(service.currentPluginId).toBeNull();
    });

    it('returns pluginId when workspace is selected', () => {
      service.selectWorkspace(mockPlugin);
      expect(service.currentPluginId).toBe('plugin-1');
    });
  });

  describe('currentProvider', () => {
    it('returns null when no provider is selected', () => {
      expect(service.currentProvider).toBeNull();
    });

    it('returns provider id when selected', () => {
      service.selectWorkspace(mockPlugin, 'prov-1');
      expect(service.currentProvider).toBe('prov-1');
    });
  });

  describe('setTenantEmail', () => {
    it('updates tenantEmail in state', (done) => {
      service.setTenantEmail('admin@example.com');

      service.currentWorkspaceState$.subscribe((state) => {
        expect(state.tenantEmail).toBe('admin@example.com');
        done();
      });
    });
  });
});
