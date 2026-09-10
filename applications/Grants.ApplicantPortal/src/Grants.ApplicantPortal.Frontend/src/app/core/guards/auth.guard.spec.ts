import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Observable, of } from 'rxjs';

import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { WorkspaceService } from '../services/workspace.service';
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

function runGuard(
  authServiceSpy: jasmine.SpyObj<AuthService>,
  workspaceServiceSpy: jasmine.SpyObj<WorkspaceService>,
  routerSpy: jasmine.SpyObj<Router>,
  url: string
): Promise<boolean> {
  const mockRoute = {} as ActivatedRouteSnapshot;
  const mockState = { url } as RouterStateSnapshot;

  return TestBed.runInInjectionContext(() => {
    const result = authGuard(mockRoute, mockState);
    if (typeof result === 'boolean') {
      return Promise.resolve(result);
    }
    return new Promise<boolean>((resolve, reject) => {
      (result as Observable<boolean>).subscribe({
        next: (v: boolean) => resolve(v),
        error: reject,
      });
    });
  });
}

describe('authGuard', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let workspaceServiceSpy: jasmine.SpyObj<WorkspaceService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj<AuthService>('AuthService', [], {
      isAuthenticated$: of(false),
    });
    workspaceServiceSpy = jasmine.createSpyObj<WorkspaceService>(
      'WorkspaceService',
      ['getAvailableWorkspaces', 'isWorkspaceSelectionRequired'],
      { currentWorkspaceState$: of(defaultWorkspaceState) }
    );
    workspaceServiceSpy.isWorkspaceSelectionRequired.and.returnValue(false);
    workspaceServiceSpy.getAvailableWorkspaces.and.returnValue(of({ plugins: [] }));

    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
        { provide: Router, useValue: routerSpy },
      ],
    });
  });

  it('redirects to /login and returns false when not authenticated', async () => {
    Object.defineProperty(authServiceSpy, 'isAuthenticated$', { get: () => of(false) });

    const result = await runGuard(authServiceSpy, workspaceServiceSpy, routerSpy, '/app/applicant-info');

    expect(result).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('allows activation when authenticated and accessing workspace-selector', async () => {
    Object.defineProperty(authServiceSpy, 'isAuthenticated$', { get: () => of(true) });

    const result = await runGuard(authServiceSpy, workspaceServiceSpy, routerSpy, '/workspace-selector');

    expect(result).toBeTrue();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  it('allows activation when authenticated and accessing submission-print, with no workspace selected', async () => {
    Object.defineProperty(authServiceSpy, 'isAuthenticated$', { get: () => of(true) });
    // A submission-print tab is a fresh page load — it always starts with empty workspace
    // state (the default from beforeEach) — and the guard must let it through anyway, since
    // that route is self-contained (reads pluginId/provider/submissionId from the URL) and
    // never touches WorkspaceService.

    const result = await runGuard(
      authServiceSpy,
      workspaceServiceSpy,
      routerSpy,
      '/submission-print/plugin-1/prov-1/sub-1'
    );

    expect(result).toBeTrue();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
    expect(workspaceServiceSpy.getAvailableWorkspaces).not.toHaveBeenCalled();
  });

  it('does NOT bypass workspace selection for an unrelated route that merely carries "/submission-print/" in a query param', async () => {
    Object.defineProperty(authServiceSpy, 'isAuthenticated$', { get: () => of(true) });
    // Regression guard: the submission-print exemption must match the URL's path only, not the
    // full state.url string (which includes the query string) — otherwise a route like
    // /app/applicant-info?returnUrl=/submission-print/x/y/z would wrongly skip the mandatory
    // workspace-selection check just because that substring appears in a query param.

    const noWorkspaceState: WorkspaceState = {
      ...defaultWorkspaceState,
      availableWorkspaces: [{ pluginId: 'p1', description: 'Workspace 1', features: [], providers: [] }],
      isWorkspaceSelected: false,
      isProviderSelected: false,
    };
    Object.defineProperty(workspaceServiceSpy, 'currentWorkspaceState$', {
      get: () => of(noWorkspaceState),
    });
    workspaceServiceSpy.isWorkspaceSelectionRequired.and.returnValue(true);

    const result = await runGuard(
      authServiceSpy,
      workspaceServiceSpy,
      routerSpy,
      '/app/applicant-info?returnUrl=/submission-print/plugin-1/prov-1/sub-1'
    );

    expect(result).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/workspace-selector'], jasmine.anything());
  });

  it('allows activation when authenticated and workspace + provider are selected', async () => {
    Object.defineProperty(authServiceSpy, 'isAuthenticated$', { get: () => of(true) });

    const selectedState: WorkspaceState = {
      ...defaultWorkspaceState,
      selectedWorkspace: { pluginId: 'p1', description: 'Workspace 1', features: [], providers: [] },
      selectedProvider: 'prov-1',
      availableWorkspaces: [{ pluginId: 'p1', description: 'Workspace 1', features: [], providers: [] }],
      isWorkspaceSelected: true,
      isProviderSelected: true,
    };
    Object.defineProperty(workspaceServiceSpy, 'currentWorkspaceState$', {
      get: () => of(selectedState),
    });
    workspaceServiceSpy.isWorkspaceSelectionRequired.and.returnValue(false);

    const result = await runGuard(authServiceSpy, workspaceServiceSpy, routerSpy, '/app/applicant-info');

    expect(result).toBeTrue();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  it('redirects to workspace-selector when authenticated but no workspace selected', async () => {
    Object.defineProperty(authServiceSpy, 'isAuthenticated$', { get: () => of(true) });

    const noWorkspaceState: WorkspaceState = {
      ...defaultWorkspaceState,
      availableWorkspaces: [{ pluginId: 'p1', description: 'Workspace 1', features: [], providers: [] }],
      isWorkspaceSelected: false,
      isProviderSelected: false,
    };
    Object.defineProperty(workspaceServiceSpy, 'currentWorkspaceState$', {
      get: () => of(noWorkspaceState),
    });
    workspaceServiceSpy.isWorkspaceSelectionRequired.and.returnValue(true);

    const result = await runGuard(authServiceSpy, workspaceServiceSpy, routerSpy, '/app/applicant-info');

    expect(result).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(
      ['/workspace-selector'],
      jasmine.objectContaining({ queryParams: jasmine.objectContaining({ returnUrl: '/app/applicant-info' }) })
    );
  });

  it('returns false and fetches workspaces when no workspaces are loaded yet', async () => {
    Object.defineProperty(authServiceSpy, 'isAuthenticated$', { get: () => of(true) });

    const emptyState: WorkspaceState = {
      ...defaultWorkspaceState,
      availableWorkspaces: [],
      isWorkspaceSelected: false,
    };
    Object.defineProperty(workspaceServiceSpy, 'currentWorkspaceState$', {
      get: () => of(emptyState),
    });

    const result = await runGuard(authServiceSpy, workspaceServiceSpy, routerSpy, '/app/applicant-info');

    expect(result).toBeFalse();
    expect(workspaceServiceSpy.getAvailableWorkspaces).toHaveBeenCalled();
  });
});
