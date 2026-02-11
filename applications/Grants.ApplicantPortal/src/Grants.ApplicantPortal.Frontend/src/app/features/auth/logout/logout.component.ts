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
  styles: [`
    .logout-page {
      display: flex;
      min-height: 100vh;
      width: 100%;
    }

    .logout-background {
      flex: 1;
      background: var(--bc-bg-info);
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      padding: 2rem;
      position: relative;
    }

    .bc-logo-header {
      display: none;
    }

    .logout-card-container {
      display: flex;
      justify-content: center;
      align-items: center;
      width: 100%;
      max-width: 500px;
    }

    .logout-card {
      background: var(--bc-white);
      border-radius: 8px;
      padding: 3rem 2.5rem;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
      text-align: center;
      width: 100%;
    }

    .logout-header {
      margin-bottom: 1.5rem;

      h1 {
        color: var(--bc-bg-info);
        font-size: 1.75rem;
        font-weight: 700;
        margin: 0 0 0.25rem 0;
        line-height: 1.2;
      }

      h2 {
        color: var(--bc-primary);
        font-size: 1.1rem;
        font-weight: 400;
        margin: 0;
        line-height: 1.3;
      }
    }

    .logout-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 120px;
    }

    .spinner-container {
      margin-bottom: 1.5rem;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 3px solid var(--bc-gray-20, #d1d1d1);
      border-top: 3px solid var(--bc-blue, #1a5a96);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .status-text {
      color: var(--bc-primary);
      font-size: var(--bc-font-size-14);
      line-height: 1.5;
      margin: 0;
    }

    .preview-section {
      flex: 1;
      background: #f8f9fa;
      display: flex;
      flex-direction: column;
      position: relative;
      padding: 2rem;
    }

    .bc-logo-corner {
      display: flex;
      justify-content: center;
      align-items: center;

      .bc-corner-logo {
        height: 120px;
        width: auto;
      }
    }

    .preview-image {
      flex: 1;
      display: flex;
      justify-content: center;
      align-items: center;

      .dashboard-preview {
        max-width: 100%;
        height: auto;
      }
    }

    .mobile-preview-image {
      display: none;
    }

    .footer-link {
      display: flex;
      justify-content: center;
      align-items: center;

      a {
        color: var(--bc-bg-info);
        text-decoration: none;
        font-size: 0.9rem;
        font-weight: 500;

        &:hover {
          text-decoration: underline;
        }
      }
    }

    @media (max-width: 768px) {
      .preview-section {
        display: none;
      }

      .logout-page {
        flex-direction: column;
        background: var(--bc-white);
      }

      .logout-background {
        background: var(--bc-white);
        padding: 2rem 1rem;
        min-height: 100vh;
        justify-content: flex-start;
      }

      .bc-logo-header {
        display: block !important;
        position: static;
        margin-bottom: 1rem;
        justify-content: center;

        .bc-header-logo {
          height: 80px;
          width: auto;
        }
      }

      .logout-card-container {
        max-width: 100%;
        width: 100%;
      }

      .logout-card {
        background: var(--bc-white);
        box-shadow: none;
        border-radius: 0;
        padding: 0;
      }

      .logout-header {
        text-align: center;
        margin-bottom: 3rem;

        h1 {
          color: var(--bc-bg-info);
          font-size: 2rem;
          font-weight: 700;
          margin-bottom: 0.5rem;
          line-height: 1.2;
        }

        h2 {
          color: var(--bc-primary);
          font-size: var(--bc-font-size-20);
          font-weight: 400;
          margin: 0;
          line-height: 1.3;
        }
      }

      .status-text {
        font-size: var(--bc-font-size-16);
        line-height: 1.6;
        padding: 0 1rem;
      }

      .mobile-preview-image {
        display: block;
        margin: 2rem 0;

        .dashboard-preview {
          max-width: 100%;
          height: auto;
        }
      }
    }
  `]
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