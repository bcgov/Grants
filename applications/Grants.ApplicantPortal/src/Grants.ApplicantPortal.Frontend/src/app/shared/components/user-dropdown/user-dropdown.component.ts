import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
@Component({
  selector: 'app-user-dropdown',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-dropdown.component.html',
  styleUrls: ['./user-dropdown.component.scss'],
})
export class UserDropdownComponent {
  @Input() userInfo: any;
  @Input() dropdownId: string = 'userDropdown';
  @Input() buttonClass: string = 'btn btn-link dropdown-toggle';
  @Input() dropdownClass: string = '';
  @Input() menuClass: string = 'dropdown-menu-end';
  @Input() iconClass: string = 'fa-regular fa-circle-user fa-2xl';
  @Input() showLogout: boolean = true;

  @Output() logout = new EventEmitter<Event>();

  onLogout(event: Event): void {
    event.preventDefault();
    this.logout.emit(event);
  }
}
