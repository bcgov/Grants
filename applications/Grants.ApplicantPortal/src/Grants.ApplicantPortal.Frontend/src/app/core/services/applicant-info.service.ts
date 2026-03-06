import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { map, retry, catchError } from 'rxjs/operators';
import {
  BackendResponse,  
  OrganizationData,
  OrganizationResponse,
  SubmissionsResponse,
  PluginEventDto,
  PluginEventsResponse,
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
   * Parses addresses backend response and extracts addresses data
   */
  private parseAddressesResponse(
    response: any
  ): { addressesData: any[] } {
    try {
      console.log('Parsing addresses response:', response);
      const jsonData = response.data ?? response.jsonData;
      console.log('Raw addresses JSON data:', jsonData);

      const parsedData = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;
      console.log('Parsed addresses data:', parsedData);

      return {
        addressesData: parsedData.addresses ?? []
      };
    } catch (error) {
      console.error('Error parsing addresses data:', error, response);
      throw new Error('Failed to parse addresses data');
    }
  }

  /**
   * Parses contacts backend response and extracts contacts data
   */
  private parseContactsResponse(
    response: any
  ): { contactsData: any[] } {
    try {
      console.log('Parsing contacts response:', response);
      const jsonData = response.data ?? response.jsonData;
      console.log('Raw contacts JSON data:', jsonData);

      const parsedData = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;
      console.log('Parsed contacts data:', parsedData);

      return {
        contactsData: parsedData.contacts ?? []
      };
    } catch (error) {
      console.error('Error parsing contacts data:', error, response);
      throw new Error('Failed to parse contacts data');
    }
  }

  /**
   * Parses backend response and extracts organization data
   */
  private parseOrganizationResponse(
    response: any
  ): OrganizationResponse {
    try {
      // Handle both new format (data) and old format (jsonData)
      const jsonData = response.data ?? response.jsonData;
      console.log('Parsing organization response:', jsonData);

      let parsedData = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;

      // Check if the parsed data is still a string (double-encoded)
      if (typeof parsedData === 'string') {
        console.log('Data was double-encoded, parsing again...');
        parsedData = JSON.parse(parsedData);
      }
      
      console.log('Final parsed organization data:', parsedData);
      console.log('Organization info from parsed data:', parsedData.organizationInfo);

      const result = {
        metadata: {
          pluginId: response.pluginId,
          provider: response.provider,
          key: response.key,
          populatedAt: response.populatedAt,
        },
        organizationData: parsedData.organizationInfo,
      };
      
      console.log('Returning organization response:', result);
      return result;
    } catch (error) {
      console.error('Error parsing organization data:', error, response);
      throw new Error('Failed to parse organization data');
    }
  }

  private parseSubmissionsResponse(
    response: BackendResponse
  ): SubmissionsResponse {
    try {
      // Handle both old 'jsonData' format and new 'data' format
      const dataString = (response as any).data ?? response.jsonData;
      console.log('Parsing submissions response:', dataString);
      const parsedData = typeof dataString === 'string' ? JSON.parse(dataString) : dataString;
      console.log('Parsed submissions data:', parsedData);

      return {
        metadata: {
          pluginId: response.pluginId,
          provider: response.provider,
          key: response.key,
          populatedAt: response.populatedAt,
        },
        submissionsData: parsedData.submissions ?? [],
        linkSource: parsedData.linkSource,
      };
    } catch (error) {
      console.error('Error parsing submissions data:', error, (response as any).data ?? response.jsonData);
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
      retry({ count: 2, delay: 1000 }),
      map((response) => this.parseOrganizationResponse(response)),
      catchError((error) => {
        console.error('Failed to load organization info after retries:', error);
        throw error;
      })
    );
  }

  saveOrganizationInfo(
    orgInfo: OrganizationData, 
    pluginId: string, 
    provider: string
  ): Observable<any> {
    console.log('ApplicantInfoService - Saving organization info:', orgInfo);
    
    // Get the required parameters for the API endpoint
    const addressId = orgInfo.organizationId ?? orgInfo.orgNumber;
    
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
          message: error.error?.message ?? 'Please try again later',
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
      retry({ count: 2, delay: 1000 }),
      map((response) => this.parseSubmissionsResponse(response)),
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
      map(response => this.parseContactsResponse(response)),
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
      map(response => this.parseAddressesResponse(response)),
      catchError((error) => {
        console.error('Failed to load addresses info after retries:', error);
        throw error;
      })
    );
  }

  /**
   * Fetches available contact roles for a workspace
   */
  getContactRoles(pluginId: string): Observable<any> {
    const url = `${this.baseUrl}/Contacts/${pluginId}/roles`;
    return this.http.get<any>(url).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to fetch contact roles:', error);
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
      role: string;
      workPhoneNumber?: string;
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
   * Updates an existing address
   */
  updateAddress(
    addressId: string,
    pluginId: string,
    provider: string,
    addressData: {
      addressLine1?: string;
      addressLine2?: string;
      city: string;
      province: string;
      postalCode: string;
      country?: string;
      type: string;
      isPrimary: boolean;
    }
  ): Observable<any> {
    const url = `${this.baseUrl}/Addresses/${addressId}/${pluginId}/${provider}`;
    return this.http.put<any>(url, addressData).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to update address:', error);
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
      role: string;
      workPhoneNumber?: string;
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

  // ── Plugin Events ──────────────────────────────────────────────

  /**
   * Gets unacknowledged events for the current workspace/provider.
   * Fails silently — events are non-blocking.
   */
  getEvents(pluginId: string, provider: string): Observable<PluginEventDto[]> {
    const url = `${this.baseUrl}/Events/${pluginId}/${provider}`;
    return this.http.get<PluginEventsResponse>(url).pipe(
      map(response => response.events),
      catchError(error => {
        console.error('Failed to load plugin events:', error);
        return of([]);
      })
    );
  }

  /**
   * Acknowledges (dismisses) a single event.
   */
  acknowledgeEvent(eventId: string): Observable<any> {
    const url = `${this.baseUrl}/Events/${eventId}/acknowledge`;
    return this.http.patch(url, {});
  }

  /**
   * Acknowledges (dismisses) all events for a workspace/provider.
   */
  acknowledgeAllEvents(pluginId: string, provider: string): Observable<any> {
    const url = `${this.baseUrl}/Events/${pluginId}/${provider}/acknowledge-all`;
    return this.http.patch(url, {});
  }
}
