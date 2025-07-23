import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header.component';
import { SidebarComponent } from './sidebar.component';
import { ApplicantService } from '../../core/services/applicant.service';
import { ApplicantInfo } from '../../shared/models/applicant.model';
import { UserDropdownComponent } from '../../shared/components/user-dropdown/user-dropdown.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderComponent,
    SidebarComponent,
    UserDropdownComponent,
  ],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss'],
})
export class LayoutComponent implements OnInit {
  applicantInfo: ApplicantInfo | null = null;
  sidebarOpen = false;

  constructor(private readonly applicantService: ApplicantService) {}

  ngOnInit(): void {
    this.applicantService
      .getApplicantInfo()
      .subscribe((data: ApplicantInfo) => {
        this.applicantInfo = data;
      });
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  closeSidebar(): void {
    this.sidebarOpen = false;
  }

  onMobileLogout(event: Event): void {
    console.log('Mobile logout clicked');
    // Implement logout logic here
  }

  // Close sidebar when clicking on main content on mobile
  onMainContentClick(): void {
    if (window.innerWidth < 768) {
      this.closeSidebar();
    }
  }
}
