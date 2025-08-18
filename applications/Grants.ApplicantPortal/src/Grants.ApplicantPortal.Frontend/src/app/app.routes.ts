import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/components/layout.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(
        (m) => m.LoginComponent
      ),
  },
  {
    path: '',
    redirectTo: '/applicant-info',
    pathMatch: 'full'
  },
  {
    path: '',
    component: LayoutComponent,
    children: [
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
  { path: '**', redirectTo: '/login' },
];
