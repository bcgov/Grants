import { Component, OnInit, OnDestroy, HostListener, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header.component';
import { SidebarComponent } from './sidebar.component';
import { ApplicantService } from '../../core/services/applicant.service';
import { AuthService } from '../../core/services/auth.service';
import { ApplicantInfo } from '../../shared/models/applicant.interface';
import { UserDropdownComponent } from '../../shared/components/user-dropdown/user-dropdown.component';
import { NotificationsDropdownComponent } from '../../shared/components/notifications-dropdown/notifications-dropdown.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderComponent,
    SidebarComponent,
    UserDropdownComponent,
    NotificationsDropdownComponent,
  ],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss'],
})
export class LayoutComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('mobileHeaderRef') mobileHeaderRef!: ElementRef<HTMLElement>;
  applicantInfo: ApplicantInfo | null = null;
  sidebarOpen = false;
  sidebarCollapsed = false;

  private readonly lgBreakpoint = 992;
  private resizeObserver: ResizeObserver | null = null;

  constructor(
    private readonly applicantService: ApplicantService,
    private readonly authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.applicantService
      .getApplicantInfo()
      .subscribe((data: ApplicantInfo) => {
        this.applicantInfo = data;
      });
  }

  ngAfterViewInit(): void {
    if (this.mobileHeaderRef?.nativeElement) {
      this.updateMobileHeaderHeight();
      this.resizeObserver = new ResizeObserver(() => this.updateMobileHeaderHeight());
      this.resizeObserver.observe(this.mobileHeaderRef.nativeElement);
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  private updateMobileHeaderHeight(): void {
    const height = this.mobileHeaderRef?.nativeElement?.offsetHeight ?? 70;
    document.documentElement.style.setProperty('--mobile-header-height', `${height}px`);
  }

  @HostListener('window:resize')
  onResize(): void {
    if (window.innerWidth < this.lgBreakpoint && this.sidebarCollapsed) {
      this.sidebarCollapsed = false;
    }
    if (window.innerWidth >= this.lgBreakpoint && this.sidebarOpen) {
      this.sidebarOpen = false;
    }
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  closeSidebar(): void {
    this.sidebarOpen = false;
  }

  onMobileLogout(event: Event): void {
    event.preventDefault();
    this.authService.logout();
  }

  // Close sidebar when clicking on main content on mobile
  onMainContentClick(): void {
    if (window.innerWidth < 768) {
      this.closeSidebar();
    }
  }

  private clearSession(): void {
    try {
      // Clear sessionStorage
      sessionStorage.clear();
      // Redirect to login page
      this.router.navigate(['/login']);
    } catch (error) {
      console.error('Error clearing session:', error);
      this.router.navigate(['/login']);
    }
  }
}
