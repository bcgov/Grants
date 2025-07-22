import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header.component';
import { SidebarComponent } from './sidebar.component';
import { ApplicantService } from '../../core/services/applicant.service';
import { ApplicantInfo } from '../../shared/models/applicant.model';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, HeaderComponent, SidebarComponent],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss'],
})
export class LayoutComponent implements OnInit {
  applicantInfo: ApplicantInfo | null = null;

  constructor(private readonly applicantService: ApplicantService) {}

  ngOnInit(): void {
    this.applicantService
      .getApplicantInfo()
      .subscribe((data: ApplicantInfo) => {
        this.applicantInfo = data;
      });
  }
}
