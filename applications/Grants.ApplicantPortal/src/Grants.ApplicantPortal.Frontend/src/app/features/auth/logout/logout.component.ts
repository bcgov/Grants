import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-logout',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="logout-container">
      <div class="logout-content">
        <div class="spinner-container">
          <div class="spinner-border text-primary">
            <span class="visually-hidden">Logging out...</span>
          </div>
        </div>
        <h3>Logging out...</h3>
        <p>Please wait while we log you out securely.</p>
      </div>
    </div>
  `,
  styles: [`
    .logout-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background-color: var(--bc-bg-info);
    }

    .logout-content {
      text-align: center;
      background: var(--bc-white);
      padding: 3rem 2rem;
      border-radius: 8px;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
      max-width: 400px;
      width: 100%;
      margin: 0 1rem;
    }

    .spinner-container {
      margin-bottom: 1.5rem;
    }

    h3 {
      color: var(--bc-primary);
      margin-bottom: 1rem;
      font-size: 1.5rem;
    }

    p {
      color: var(--bc-primary);
      font-size: var(--bc-font-size-14);
      margin: 0;
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