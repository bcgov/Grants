import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { BehaviorSubject, of } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';

import { LayoutComponent } from './layout.component';
import { ApplicantService } from '../../core/services/applicant.service';
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

describe('LayoutComponent', () => {
  let workspaceState: BehaviorSubject<WorkspaceState>;
  let component: LayoutComponent;
  let fixture: ComponentFixture<LayoutComponent>;
  let applicantServiceSpy: jasmine.SpyObj<ApplicantService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let workspaceServiceSpy: jasmine.SpyObj<WorkspaceService>;

  beforeEach(async () => {
    workspaceState = new BehaviorSubject<WorkspaceState>({ ...defaultWorkspaceState });
    applicantServiceSpy = jasmine.createSpyObj<ApplicantService>('ApplicantService', [
      'getApplicantInfo',
    ]);
    applicantServiceSpy.getApplicantInfo.and.returnValue(
      of({ id: '123', organization: 'Test Org' })
    );

    authServiceSpy = jasmine.createSpyObj<AuthService>('AuthService', ['logout']);

    workspaceServiceSpy = jasmine.createSpyObj<WorkspaceService>(
      'WorkspaceService',
      ['getProviders', 'setTenantEmail'],
      {
        currentWorkspaceState$: workspaceState.asObservable(),
        isChangingWorkspace$: of(false),
      }
    );
    workspaceServiceSpy.getProviders.and.returnValue(of({ pluginId: '', providers: [] }));

    await TestBed.configureTestingModule({
      imports: [LayoutComponent, RouterTestingModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ApplicantService, useValue: applicantServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(LayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('loads applicant info on init', () => {
    expect(applicantServiceSpy.getApplicantInfo).toHaveBeenCalled();
    expect(component.applicantInfo).toEqual({ id: '123', organization: 'Test Org' });
  });

  it('toggleSidebar flips sidebarOpen', () => {
    expect(component.sidebarOpen).toBeFalse();
    component.toggleSidebar();
    expect(component.sidebarOpen).toBeTrue();
    component.toggleSidebar();
    expect(component.sidebarOpen).toBeFalse();
  });

  it('closeSidebar sets sidebarOpen to false', () => {
    component.sidebarOpen = true;
    component.closeSidebar();
    expect(component.sidebarOpen).toBeFalse();
  });

  it('onMobileLogout calls authService.logout()', () => {
    const event = new MouseEvent('click');
    spyOn(event, 'preventDefault');
    component.onMobileLogout(event);
    expect(event.preventDefault).toHaveBeenCalled();
    expect(authServiceSpy.logout).toHaveBeenCalled();
  });

  it('subscribes to workspace state and updates hasMultipleOrgs', () => {
    expect(component.hasMultipleOrgs).toBeFalse();
  });

  it('cleans up on destroy', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });

  it('renders the configured rich text only for multiple applicants', () => {
    workspaceState.next({ ...defaultWorkspaceState, multipleIdentitiesMessageHtml: '<p><strong>Contact us</strong></p>' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.global-warning-banner')).toBeNull();
    workspaceState.next({ ...workspaceState.value, hasMultipleOrgs: true });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.portal-message-content strong').textContent).toBe('Contact us');
  });

  it('sanitizes API HTML without bypassing Angular security', () => {
    workspaceState.next({ ...defaultWorkspaceState, hasMultipleOrgs: true,
      multipleIdentitiesMessageHtml: '<p onclick="alert(1)">Contact us</p><script>alert(1)</script><a href="javascript:alert(1)">Help</a>' });
    fixture.detectChanges();
    const content = fixture.nativeElement.querySelector('.portal-message-content');
    expect(content.querySelector('script')).toBeNull();
    expect(content.querySelector('p').hasAttribute('onclick')).toBeFalse();
    expect(content.querySelector('a').getAttribute('href')).not.toBe('javascript:alert(1)');
  });

  it('falls back to the original message for an older API response', () => {
    workspaceState.next({ ...defaultWorkspaceState, hasMultipleOrgs: true, tenantEmail: 'program@example.test' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.global-warning-banner').textContent).toContain('administrator at program@example.test.');
    workspaceState.next({ ...defaultWorkspaceState, hasMultipleOrgs: true });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.global-warning-banner').textContent).toContain('contact the grant program administrator.');
  });

  it('replaces the displayed message when the selected tenant state changes', () => {
    workspaceState.next({ ...defaultWorkspaceState, hasMultipleOrgs: true, multipleIdentitiesMessageHtml: '<p>Tenant A</p>' });
    fixture.detectChanges();
    workspaceState.next({ ...defaultWorkspaceState, hasMultipleOrgs: true, multipleIdentitiesMessageHtml: '<p>Tenant B</p>' });
    fixture.detectChanges();
    const content = fixture.nativeElement.querySelector('.portal-message-content').textContent;
    expect(content).toContain('Tenant B');
    expect(content).not.toContain('Tenant A');
  });
});
