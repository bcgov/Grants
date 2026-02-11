import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-logout',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="logout-page">
      <div class="logout-background">
        <div class="bc-logo-header">
          <img
            src="images/logo/BCID_H_rgb_pos.png"
            alt="British Columbia Government"
            class="bc-header-logo"
          />
        </div>

        <div class="logout-card-container">
          <div class="logout-card">
            <div class="logout-header">
              <h1>Enterprise Grant</h1>
              <h2>Management System Portal</h2>
            </div>

            <div class="mobile-preview-image">
              <img
                src="images/dashboard-preview.png"
                alt="Dashboard Preview"
                class="dashboard-preview"
              />
            </div>

            <div class="logout-state">
              <div class="spinner-container">
                <div class="spinner"></div>
              </div>
              <p class="status-text">Please wait while we log you out securely.</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Right side preview -->
      <div class="preview-section">
        <div class="bc-logo-corner">
          <img
            src="images/logo/BCID_H_rgb_pos.png"
            alt="British Columbia"
            class="bc-corner-logo"
          />
        </div>

        <div class="preview-image">
          <img
            src="images/dashboard-preview.png"
            alt="Dashboard Preview"
            class="dashboard-preview"
          />
        </div>

        <div class="footer-link">
          <a href="https://grants.gov.bc.ca" target="_blank">grants.gov.bc.ca</a>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./logout.component.scss']
})
export class LogoutComponent implements OnInit {
  constructor(private router: Router) {}

  ngOnInit(): void {
    // Clear all storage and redirect after a short delay
    setTimeout(() => {
      try {
        localStorage.clear();
        sessionStorage.clear();
        console.log('Storage cleared during logout');
      } catch (error) {
        console.warn('Error clearing storage during logout:', error);
      }
      
      this.router.navigate(['/login']);
    }, 2000);
  }
}