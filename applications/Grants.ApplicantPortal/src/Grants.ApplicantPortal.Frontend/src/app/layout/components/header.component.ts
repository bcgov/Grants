import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApplicantInfo } from '../../shared/models/applicant.model';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
})
export class HeaderComponent {
  @Input() applicantInfo: ApplicantInfo | null = null;

  onLogout() {
    // Implement logout logic
    console.log('Logout clicked');
  }
}
