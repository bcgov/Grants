import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/components/layout.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Public routes
  {
    path: 'auth/callback',
    loadComponent: () =>
      import('./features/auth/callback/callback.component').then(
        (m) => m.CallbackComponent
      ),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(
        (m) => m.LoginComponent
      ),
  },

  // Protected routes
  {
    path: '',
    component: LayoutComponent,
    //canActivate: [authGuard], // Auth guard enabled for all protected routes
    children: [
      {
        path: '',
        redirectTo: 'applicant-info',
        pathMatch: 'full',
      },
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
      {
        path: 'test',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/test/test.component').then((m) => m.TestComponent),
      },
    ],
  },
  { path: '**', redirectTo: '/login' },
];
