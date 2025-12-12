import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, switchMap, retry, catchError } from 'rxjs/operators';
import {
  BackendResponse,  
  OrganizationData,
  OrganizationResponse,
  SubmissionsResponse,
} from '../../shared/models/applicant-info.interface';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ApplicantInfoService {
  private readonly baseUrl = environment.apiUrl;
  constructor(private readonly http: HttpClient) {}

  /**
   * Gets organization information using the new endpoint structure
   */
  private getOrganizationData(
    profileId: string,
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Organizations/${profileId}/${pluginId}/${provider}`;
    return this.http.get<BackendResponse>(url, { params: parameters });
  }

  /**
   * Gets submissions information using the new endpoint structure
   */
  private getSubmissionsData(
    profileId: string,
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Submissions/${profileId}/${pluginId}/${provider}`;
    return this.http.get<BackendResponse>(url, { params: parameters });
  }

  /**
   * Gets contacts information using the new endpoint structure
   */
  private getContactsData(
    profileId: string,
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Contacts/${profileId}/${pluginId}/${provider}`;
    return this.http.get<BackendResponse>(url, { params: parameters });
  }

  /**
   * Gets addresses information using the new endpoint structure
   */
  private getAddressesData(
    profileId: string,
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Addresses/${profileId}/${pluginId}/${provider}`;
    return this.http.get<BackendResponse>(url, { params: parameters });
  }

  /**
   * Parses backend response and extracts organization data
   */
  private parseOrganizationResponse(
    response: BackendResponse
  ): OrganizationResponse {
    try {
      const parsedData = JSON.parse(response.jsonData);

      return {
        metadata: {
          profileId: response.profileId,
          pluginId: response.pluginId,
          provider: response.provider,
          key: response.key,
          populatedAt: response.populatedAt,
        },
        organizationData: parsedData.data.organizationInfo,
      };
    } catch (error) {
      console.error('Error parsing organization data:', error);
      throw new Error('Failed to parse organization data');
    }
  }

  private parseSubmissionsResponse(
    response: BackendResponse
  ): SubmissionsResponse {
    try {
      const parsedData = JSON.parse(response.jsonData);

      return {
        metadata: {
          profileId: response.profileId,
          pluginId: response.pluginId,
          provider: response.provider,
          key: response.key,
          populatedAt: response.populatedAt,
        },
        submissionsData: parsedData.data.submissions,
      };
    } catch (error) {
      console.error('Error parsing submissions data:', error);
      throw new Error('Failed to parse submissions data');
    }
  }

  /**
   * Main method: Gets formatted organization information
   */
  getOrganizationInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<OrganizationResponse> {
    return this.getOrganizationData(profileId, pluginId, provider, parameters).pipe(
      map((response) => this.parseOrganizationResponse(response)),
      retry({ count: 2, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to load organization info after retries:', error);
        throw error;
      })
    );
  }

  saveOrganizationInfo(orgInfo: OrganizationData): Observable<any> {
    // Replace with actual API call
    return of({ success: true });
  }

  //Submissions information
  getSubmissionsInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<SubmissionsResponse> {
    return this.getSubmissionsData(profileId, pluginId, provider, parameters).pipe(      
      map((response) => this.parseSubmissionsResponse(response)),
      retry({ count: 2, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to load submissions info after retries:', error);
        throw error;
      })
    );
  }

  /**
   * Gets contacts information
   */
  getContactsInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<any> {
    return this.getContactsData(profileId, pluginId, provider, parameters).pipe(      
      retry({ count: 2, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to load contacts info after retries:', error);
        throw error;
      })
    );
  }

  /**
   * Gets addresses information
   */
  getAddressesInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<any> {
    return this.getAddressesData(profileId, pluginId, provider, parameters).pipe(      
      retry({ count: 2, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to load addresses info after retries:', error);
        throw error;
      })
    );
  }

  /**
   * Creates a new contact
   */
  createContact(
    profileId: string,
    pluginId: string,
    provider: string,
    contactData: {
      name: string;
      email: string;
      title?: string;
      type: string;
      phoneNumber?: string;
      isPrimary: boolean;
    }
  ): Observable<any> {
    const url = `${this.baseUrl}/Contacts/${profileId}/${pluginId}/${provider}`;
    return this.http.post<any>(url, contactData).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to create contact:', error);
        throw error;
      })
    );
  }

  /**
   * Sets a contact as primary
   */
  setContactAsPrimary(
    contactId: string,
    profileId: string,
    pluginId: string,
    provider: string
  ): Observable<any> {
    const url = `${this.baseUrl}/Contacts/${contactId}/${profileId}/${pluginId}/${provider}/set-primary`;
    return this.http.patch<any>(url, {}).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to set contact as primary:', error);
        throw error;
      })
    );
  }

  /**
   * Sets an address as primary
   */
  setAddressAsPrimary(
    addressId: string,
    profileId: string,
    pluginId: string,
    provider: string
  ): Observable<any> {
    const url = `${this.baseUrl}/Addresses/${addressId}/${profileId}/${pluginId}/${provider}/set-primary`;
    return this.http.patch<any>(url, {}).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to set address as primary:', error);
        throw error;
      })
    );
  }

  /**
   * Updates an existing contact
   */
  updateContact(
    contactId: string,
    profileId: string,
    pluginId: string,
    provider: string,
    contactData: {
      name: string;
      email: string;
      title?: string;
      type: string;
      phoneNumber?: string;
      isPrimary: boolean;
    }
  ): Observable<any> {
    const url = `${this.baseUrl}/Contacts/${contactId}/${profileId}/${pluginId}/${provider}`;
    return this.http.put<any>(url, contactData).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to update contact:', error);
        throw error;
      })
    );
  }
}
