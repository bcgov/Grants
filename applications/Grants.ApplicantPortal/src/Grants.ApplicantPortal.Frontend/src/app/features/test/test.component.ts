import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-test',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="test-content">
      <h2>test page with auth</h2>
    </div>
  `,
  styles: [
    `
      .test-content {
        padding: 2rem;
      }
    `,
  ],
})
export class TestComponent {}
