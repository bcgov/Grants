import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/components/layout.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: '', redirectTo: '/applicant-info', pathMatch: 'full' },
      {
        path: 'applicant-info',
        loadComponent: () =>
          import(
            './features/applicant-info/components/applicant-info.component'
          ).then((m) => m.ApplicantInfoComponent),
      },
      {
        path: 'submissions',
        loadComponent: () =>
          import(
            './features/submissions/components/submissions.component'
          ).then((m) => m.SubmissionsComponent),
      },
      {
        path: 'payments',
        loadComponent: () =>
          import('./features/payments/components/payments.component').then(
            (m) => m.PaymentsComponent
          ),
      },
    ],
  },
  { path: '**', redirectTo: '/applicant-info' },
];
