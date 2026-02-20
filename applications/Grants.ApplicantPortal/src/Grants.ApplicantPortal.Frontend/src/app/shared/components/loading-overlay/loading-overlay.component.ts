import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-overlay',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (isVisible) {
      <div class="loading-overlay-card" [class.transparent]="transparent">
        <div class="loading-content">
          <div class="spinner-border" [class]="spinnerClass" role="status">
            <span class="sr-only">{{ message }}</span>
          </div>
          @if (showMessage) {
            <p class="loading-text mt-2">{{ message }}</p>
          }
        </div>
      </div>
    }
  `,
  styleUrls: ['./loading-overlay.component.scss']
})
export class LoadingOverlayComponent {
  @Input() isVisible = false;
  @Input() message = 'Loading...';
  @Input() showMessage = true;
  @Input() transparent = false;
  @Input() spinnerClass = '';
}