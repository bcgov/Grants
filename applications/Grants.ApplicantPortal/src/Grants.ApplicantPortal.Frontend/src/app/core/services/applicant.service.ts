import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import {
  ApplicantInfo,
  OrganizationInfo,
  ContactInfo,
  AddressInfo,
  Submission,
} from '../../shared/models/applicant.model';

@Injectable({
  providedIn: 'root',
})
export class ApplicantService {
  private readonly apiUrl = '/api'; // Replace with your actual API URL

  constructor(private readonly http: HttpClient) {}

  // Mock data - replace with actual API calls
  getApplicantInfo(): Observable<ApplicantInfo> {
    return of({
      id: '368GB783',
      organization: 'Your Amazing Organization Inc.',
    });
  }

  getOrganizationInfo(): Observable<OrganizationInfo> {
    return of({
      orgRegisteredNumber: '234523523423423',
      orgStatus: 'Active',
      orgType: 'Sole Proprietorship',
      orgName: 'Your Amazing Organization',
      orgSize: '50',
      nonRegOrgName: 'N/A',
      fiscalYearEndMonth: 'December',
      fiscalYearEndDay: '31',
    });
  }

  getContactInfo(): Observable<ContactInfo[]> {
    return of([
      {
        firstName: 'John',
        lastName: 'Doe',
        name: 'John Doe',
        email: 'johndoe@test.com',
        phone: '2289982839',
        title: 'CTO',
        type: 'Primary',
      },
      {
        firstName: 'Jane',
        lastName: 'Smith',
        name: 'Jane Smith',
        email: 'janesmith@test.com',
        phone: '3949302938',
        title: 'CFO',
        type: 'Primary',
      },
    ]);
  }

  getAddressInfo(): Observable<AddressInfo[]> {
    return of([
      {
        type: 'Primary',
        address: '87 1938 Lougheed Highway',
        city: 'Ucluelet',
        province: 'BC',
        postalCode: 'V3Y 2M5',
      },
      {
        type: 'Mailing',
        address: '1 234 Bellamy Link',
        city: 'Victoria',
        province: 'BC',
        postalCode: 'V9B 0Z3',
      },
    ]);
  }

  getSubmissions(): Observable<Submission[]> {
    return of([
      {
        date: '28/02/2024',
        id: '368GB783',
        projectName: 'Your project name here',
        title: 'Economic Development Grant Application',
        submissionDate: new Date('2024-02-28'),
        status: 'Submitted',
      },
      {
        date: '21/01/2024',
        id: '237456DDD',
        projectName: 'Your project name here',
        title: 'Community Enhancement Project',
        submissionDate: new Date('2024-01-21'),
        status: 'Approved',
      },
      {
        date: '22/12/2023',
        id: '161HND333',
        projectName: 'Your project name here',
        title: 'Infrastructure Development Initiative',
        submissionDate: new Date('2023-12-22'),
        status: 'Under Review',
      },
    ]);
  }

  saveOrganizationInfo(orgInfo: OrganizationInfo): Observable<any> {
    // Replace with actual API call
    return of({ success: true });
  }
}
