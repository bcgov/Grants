import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-submissions',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="submissions-content">
      <h2>Submissions</h2>
      <p>This page will contain submission management functionality.</p>
    </div>
  `,
  styles: [
    `
      .submissions-content {
        padding: 2rem;
      }
    `,
  ],
})
export class SubmissionsComponent {}
