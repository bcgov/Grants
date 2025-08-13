import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent {
  constructor(private router: Router) {}

  loginWithBCeID(): void {
    // Implement BCeID authentication redirect
    // This would typically redirect to the BCeID authentication service
    console.log('Redirecting to BCeID authentication...');

    // For development, simulate successful login
    // Replace with actual BCeID integration
    this.simulateLogin();
  }

  private simulateLogin(): void {
    // Simulate successful authentication
    localStorage.setItem('authToken', 'mock-bceid-token');
    localStorage.setItem(
      'userProfile',
      JSON.stringify({
        id: 'BCEID123',
        organization: 'Sample Organization',
      })
    );

    this.router.navigate(['/applicant-info']);
  }
}
