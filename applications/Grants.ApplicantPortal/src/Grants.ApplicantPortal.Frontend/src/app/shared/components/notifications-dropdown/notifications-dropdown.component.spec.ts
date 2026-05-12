import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';

import { NotificationsDropdownComponent } from './notifications-dropdown.component';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { WorkspaceState } from '../../models/workspace.interface';
import { PluginEventDto } from '../../models/applicant-info.interface';

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

describe('NotificationsDropdownComponent', () => {
  let component: NotificationsDropdownComponent;
  let fixture: ComponentFixture<NotificationsDropdownComponent>;
  let applicantInfoServiceSpy: jasmine.SpyObj<ApplicantInfoService>;
  let workspaceServiceSpy: jasmine.SpyObj<WorkspaceService>;
  let workspaceStateSubject: Subject<WorkspaceState>;

  beforeEach(async () => {
    workspaceStateSubject = new Subject<WorkspaceState>();

    applicantInfoServiceSpy = jasmine.createSpyObj<ApplicantInfoService>(
      'ApplicantInfoService',
      ['pollEvents', 'acknowledgeEvent', 'acknowledgeAllEvents']
    );
    applicantInfoServiceSpy.pollEvents.and.returnValue(of([]));
    applicantInfoServiceSpy.acknowledgeEvent.and.returnValue(of({}));
    applicantInfoServiceSpy.acknowledgeAllEvents.and.returnValue(of({}));

    workspaceServiceSpy = jasmine.createSpyObj<WorkspaceService>('WorkspaceService', [], {
      currentWorkspaceState$: workspaceStateSubject.asObservable(),
    });

    await TestBed.configureTestingModule({
      imports: [NotificationsDropdownComponent],
      providers: [
        { provide: ApplicantInfoService, useValue: applicantInfoServiceSpy },
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationsDropdownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('starts with isOpen false', () => {
    expect(component.isOpen).toBeFalse();
  });

  it('starts with empty events list', () => {
    expect(component.events).toEqual([]);
  });

  describe('toggleDropdown', () => {
    it('opens dropdown when closed', () => {
      component.isOpen = false;
      component.toggleDropdown();
      expect(component.isOpen).toBeTrue();
    });

    it('closes dropdown when open', () => {
      component.isOpen = true;
      component.toggleDropdown();
      expect(component.isOpen).toBeFalse();
    });
  });

  describe('unreadCount', () => {
    it('returns the length of the events array', () => {
      const mockEvents: PluginEventDto[] = [
        { eventId: '1', severity: 'Info', userMessage: 'Test', createdAt: '2024-01-01' },
        { eventId: '2', severity: 'Warning', userMessage: 'Warn', createdAt: '2024-01-02' },
      ];
      component.events = mockEvents;
      expect(component.unreadCount).toBe(2);
    });
  });

  describe('dismissEvent', () => {
    it('removes the event from the list after acknowledgement', () => {
      const mockEvents: PluginEventDto[] = [
        { eventId: 'e1', severity: 'Info', userMessage: 'Message', createdAt: '2024-01-01' },
      ];
      component.events = [...mockEvents];
      applicantInfoServiceSpy.acknowledgeEvent.and.returnValue(of({}));

      component.dismissEvent('e1');

      expect(component.events.find((e) => e.eventId === 'e1')).toBeUndefined();
    });
  });

  describe('getSeverityIcon', () => {
    it('returns error icon for Error severity', () => {
      expect(component.getSeverityIcon('Error')).toBe('fas fa-exclamation-circle');
    });

    it('returns warning icon for Warning severity', () => {
      expect(component.getSeverityIcon('Warning')).toBe('fas fa-exclamation-triangle');
    });

    it('returns info icon for Info severity', () => {
      expect(component.getSeverityIcon('Info')).toBe('fas fa-info-circle');
    });

    it('returns info icon for unknown severity', () => {
      expect(component.getSeverityIcon('Unknown')).toBe('fas fa-info-circle');
    });
  });

  describe('pollEvents', () => {
    it('starts polling when workspace and provider are emitted', () => {
      const events: PluginEventDto[] = [
        { eventId: 'evt-1', severity: 'Info', userMessage: 'Hello', createdAt: '2024-01-01' },
      ];
      applicantInfoServiceSpy.pollEvents.and.returnValue(of(events));

      workspaceStateSubject.next({
        ...defaultWorkspaceState,
        selectedWorkspace: { pluginId: 'p1', description: 'WS', features: [], providers: [] },
        selectedProvider: 'prov-1',
        isWorkspaceSelected: true,
        isProviderSelected: true,
      });

      expect(applicantInfoServiceSpy.pollEvents).toHaveBeenCalledWith('p1', 'prov-1');
      expect(component.events.length).toBe(1);
    });

    it('clears events when workspace becomes unset', () => {
      component.events = [
        { eventId: 'e1', severity: 'Info', userMessage: 'Hi', createdAt: '2024-01-01' },
      ];

      workspaceStateSubject.next(defaultWorkspaceState);

      expect(component.events).toEqual([]);
    });
  });

  it('cleans up on destroy', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
