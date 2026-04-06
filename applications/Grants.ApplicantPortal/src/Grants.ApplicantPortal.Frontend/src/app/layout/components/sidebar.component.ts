import { Component, Output, EventEmitter, Input, HostBinding } from '@angular/core';
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
  @Input() collapsed = false;
  @Output() menuItemClicked = new EventEmitter<void>();
  @Output() collapsedChange = new EventEmitter<boolean>();

  @HostBinding('class.collapsed') get isCollapsed() {
    return this.collapsed;
  }

  onMenuItemClick(): void {
    if (window.innerWidth < 768) {
      this.menuItemClicked.emit();
    }
  }

  toggleCollapse(): void {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
  }
}
