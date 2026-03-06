import { Component, OnInit, OnDestroy, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { PluginEventDto } from '../../models/applicant-info.interface';

@Component({
  selector: 'app-notifications-dropdown',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications-dropdown.component.html',
  styleUrls: ['./notifications-dropdown.component.scss']
})
export class NotificationsDropdownComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();

  events: PluginEventDto[] = [];
  isOpen = false;
  isLoading = false;

  private pluginId: string | null = null;
  private provider: string | null = null;

  constructor(
    private readonly applicantInfoService: ApplicantInfoService,
    private readonly workspaceService: WorkspaceService,
    private readonly elementRef: ElementRef
  ) {}

  ngOnInit(): void {
    // React to workspace changes and load events
    this.workspaceService.currentWorkspaceState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(state => {
        this.pluginId = state.selectedWorkspace?.pluginId ?? null;
        this.provider = state.selectedProvider ?? null;

        if (this.pluginId && this.provider) {
          this.loadEvents();
        } else {
          this.events = [];
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Close dropdown when clicking outside */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.isOpen && !this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  get unreadCount(): number {
    return this.events.length;
  }

  toggleDropdown(): void {
    this.isOpen = !this.isOpen;
  }

  dismissEvent(eventId: string): void {
    this.applicantInfoService.acknowledgeEvent(eventId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.events = this.events.filter(e => e.eventId !== eventId);
        },
        error: (err) => console.error('Failed to acknowledge event:', err)
      });
  }

  dismissAllEvents(): void {
    if (!this.pluginId || !this.provider) return;

    this.applicantInfoService.acknowledgeAllEvents(this.pluginId, this.provider)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.events = [];
          this.isOpen = false;
        },
        error: (err) => console.error('Failed to acknowledge all events:', err)
      });
  }

  getSeverityIcon(severity: string): string {
    switch (severity) {
      case 'Error':   return 'fas fa-exclamation-circle';
      case 'Warning': return 'fas fa-exclamation-triangle';
      case 'Info':
      default:        return 'fas fa-info-circle';
    }
  }

  private loadEvents(): void {
    if (!this.pluginId || !this.provider) return;

    this.isLoading = true;
    this.applicantInfoService.getEvents(this.pluginId, this.provider)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (events) => {
          this.events = events;
          this.isLoading = false;
        },
        error: () => {
          this.events = [];
          this.isLoading = false;
        }
      });
  }
}
