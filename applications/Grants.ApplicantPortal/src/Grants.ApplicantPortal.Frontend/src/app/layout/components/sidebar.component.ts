import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
})
export class SidebarComponent {
  @Output() menuItemClicked = new EventEmitter<void>();

  onMenuItemClick(): void {
    console.log('Menu item clicked');
    if (window.innerWidth < 768) {
      this.menuItemClicked.emit();
    }
  }
}
