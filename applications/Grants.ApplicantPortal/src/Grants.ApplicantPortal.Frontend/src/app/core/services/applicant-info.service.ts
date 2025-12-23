import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
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
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Organizations/${pluginId}/${provider}`;
    return this.http.get<BackendResponse>(url, { params: parameters });
  }

  /**
   * Gets submissions information using the new endpoint structure
   */
  private getSubmissionsData(
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Submissions/${pluginId}/${provider}`;
    return this.http.get<BackendResponse>(url, { params: parameters });
  }

  /**
   * Gets contacts information using the new endpoint structure
   */
  private getContactsData(
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Contacts/${pluginId}/${provider}`;
    return this.http.get<BackendResponse>(url, { params: parameters });
  }

  /**
   * Gets addresses information using the new endpoint structure
   */
  private getAddressesData(
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Addresses/${pluginId}/${provider}`;
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
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<OrganizationResponse> {
    return this.getOrganizationData(pluginId, provider, parameters).pipe(
      map((response) => this.parseOrganizationResponse(response)),
      retry({ count: 2, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to load organization info after retries:', error);
        throw error;
      })
    );
  }

  saveOrganizationInfo(orgInfo: OrganizationData): Observable<any> {
    console.log('ApplicantInfoService - Saving organization info:', orgInfo);
    
    // Get the required parameters for the API endpoint
    const addressId = orgInfo.organizationId || orgInfo.orgNumber || 'CD12E345-6789-0ABC-DEF1-234567890ABC';
    const pluginId = 'DEMO';   // You may need to get this from configuration
    const provider = 'PROGRAM1'; // You may need to get this from configuration
    
    // Map to the API payload structure with required properties
    const apiPayload = {
      Name: orgInfo.orgName,
      OrganizationType: orgInfo.organizationType,
      OrganizationNumber: orgInfo.orgNumber,
      Status: orgInfo.orgStatus,
      NonRegOrgName: orgInfo.nonRegOrgName,
      OrganizationSize: orgInfo.orgSize ? parseInt(orgInfo.orgSize) : null,
      FiscalMonth: orgInfo.fiscalMonth,
      FiscalDay: orgInfo.fiscalDay
    };
    
    const endpoint = `${this.baseUrl}/Organizations/${addressId}/${pluginId}/${provider}`;
    
    return this.http.put(endpoint, apiPayload, {
      headers: {
        'Content-Type': 'application/json'
      }
    }).pipe(
      map(response => ({
        success: true,
        message: 'Organization information saved successfully',
        data: response
      })),
      catchError(error => {
        console.error('Error saving organization:', error);
        return throwError(() => ({
          error: 'Failed to save organization information',
          message: error.error?.message || 'Please try again later',
          details: error
        }));
      })
    );
  }

  //Submissions information
  getSubmissionsInfo(
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<SubmissionsResponse> {
    return this.getSubmissionsData(pluginId, provider, parameters).pipe(      
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
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<any> {
    return this.getContactsData(pluginId, provider, parameters).pipe(      
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
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<any> {
    return this.getAddressesData(pluginId, provider, parameters).pipe(      
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
    const url = `${this.baseUrl}/Contacts/${pluginId}/${provider}`;
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
    pluginId: string,
    provider: string
  ): Observable<any> {
    const url = `${this.baseUrl}/Contacts/${contactId}/${pluginId}/${provider}/set-primary`;
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
    pluginId: string,
    provider: string
  ): Observable<any> {
    const url = `${this.baseUrl}/Addresses/${addressId}/${pluginId}/${provider}/set-primary`;
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
    const url = `${this.baseUrl}/Contacts/${contactId}/${pluginId}/${provider}`;
    return this.http.put<any>(url, contactData).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to update contact:', error);
        throw error;
      })
    );
  }

  /**
   * Deletes a contact
   */
  deleteContact(
    contactId: string,
    pluginId: string,
    provider: string
  ): Observable<any> {
    const url = `${this.baseUrl}/Contacts/${contactId}/${pluginId}/${provider}`;
    return this.http.delete<any>(url).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to delete contact:', error);
        throw error;
      })
    );
  }
}
