import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { UserDropdownComponent } from '../../shared/components/user-dropdown/user-dropdown.component';
import { ApplicantInfo } from '../../shared/models/applicant.model';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, UserDropdownComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
})
export class HeaderComponent implements OnInit, OnDestroy {
  @Input() applicantInfo: ApplicantInfo | null = null;
  pageTitle = 'Applicant Info';
  private routerSubscription?: Subscription;

  constructor(private router: Router) {}

  ngOnInit(): void {
    // Set initial title
    this.updateTitle();

    // Listen to route changes
    this.routerSubscription = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updateTitle();
      });
  }

  ngOnDestroy(): void {
    if (this.routerSubscription) {
      this.routerSubscription.unsubscribe();
    }
  }

  private updateTitle(): void {
    const url = this.router.url;

    if (url.includes('applicant-info')) {
      this.pageTitle = 'Applicant Info';
    } else if (url.includes('submissions')) {
      this.pageTitle = 'Submissions';
    } else if (url.includes('payments')) {
      this.pageTitle = 'Payments';
    } else {
      this.pageTitle = 'Applicant Info'; // Default
    }
  }

  onLogout(event: Event): void {
    console.log('Desktop logout clicked');
    // Implement logout logic here
  }
}
