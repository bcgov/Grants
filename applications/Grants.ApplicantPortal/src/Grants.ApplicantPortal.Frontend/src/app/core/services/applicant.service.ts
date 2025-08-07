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

  saveOrganizationInfo(orgInfo: OrganizationInfo): Observable<any> {
    // Replace with actual API call
    return of({ success: true });
  }
}
