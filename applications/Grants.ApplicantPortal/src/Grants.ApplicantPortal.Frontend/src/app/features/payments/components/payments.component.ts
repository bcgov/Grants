import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="payments-content">
      <h2>Payments</h2>
      <p>This page will contain payment management functionality.</p>
    </div>
  `,
  styles: [
    `
      .payments-content {
        padding: 2rem;
      }
    `,
  ],
})
export class PaymentsComponent {}
