import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-org-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './org-header.component.html',
  styleUrls: ['./org-header.component.scss'],
})
export class OrgHeaderComponent {
  @Input() orgNumber: string = '';
  @Input() orgName: string = '';
  @Input() hasMultipleOrgs: boolean = false;
}
