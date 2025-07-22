import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/components/layout.component';
import { DashboardComponent } from './features/dashboard/components/dashboard.component';
import { SubmissionsComponent } from './features/submissions/components/submissions.component';
import { PaymentsComponent } from './features/payments/components/payments.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'submissions', component: SubmissionsComponent },
      { path: 'payments', component: PaymentsComponent },
    ],
  },
  { path: '**', redirectTo: '/dashboard' },
];
