import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/components/layout.component';
import { ApplicantInfoComponent } from './features/applicant-info/components/applicant-info.component';
import { SubmissionsComponent } from './features/submissions/components/submissions.component';
import { PaymentsComponent } from './features/payments/components/payments.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: '', redirectTo: '/applicant-info', pathMatch: 'full' },
      { path: 'applicant-info', component: ApplicantInfoComponent },
      { path: 'submissions', component: SubmissionsComponent },
      { path: 'payments', component: PaymentsComponent },
    ],
  },
  { path: '**', redirectTo: '/applicant-info' },
];
