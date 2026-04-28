import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError, timer } from 'rxjs';
import { map, retry, catchError, switchMap, shareReplay, finalize } from 'rxjs/operators';
import {
  BackendResponse,  
  OrganizationData,
  SubmissionsResponse,
  PaymentsResponse,
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
   * Gets payments information using the new endpoint structure
   */
  private getPaymentsData(
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Payments/${pluginId}/${provider}`;
    return this.http.get<BackendResponse>(url, { params: parameters });
  }

  /**
   * Parses addresses backend response and extracts addresses data
   */
  private parseAddressesResponse(
    response: any
  ): { addressesData: any[] } {
    try {
      const jsonData = response.data ?? response.jsonData;

      const parsedData = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;

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
      const jsonData = response.data ?? response.jsonData;

      const parsedData = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;

      return {
        contactsData: parsedData.contacts ?? []
      };
    } catch (error) {
      console.error('Error parsing contacts data:', error, response);
      throw new Error('Failed to parse contacts data');
    }
  }

  /**
   * Parses organization backend response and extracts organizations array + saved organizationInfo
   */
  private parseOrganizationResponse(
    response: any
  ): { organizationsData: any[]; organizationData: any } {
    try {
      const jsonData = response.data ?? response.jsonData;

      if (!jsonData) {
        return { organizationsData: [], organizationData: null };
      }

      let parsedData = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;

      // Check if the parsed data is still a string (double-encoded)
      if (typeof parsedData === 'string') {
        parsedData = JSON.parse(parsedData);
      }

      // Extract organizations array
      let organizationsData: any[] = [];
      if (Array.isArray(parsedData)) {
        organizationsData = parsedData;
      } else if (parsedData && Array.isArray(parsedData.organizations)) {
        organizationsData = parsedData.organizations;
      } else if (parsedData?.organizationInfo) {
        organizationsData = Array.isArray(parsedData.organizationInfo)
          ? parsedData.organizationInfo
          : [parsedData.organizationInfo];
      }

      // Extract saved organization data
      const organizationData = parsedData?.organizationInfo ?? null;

      return { organizationsData, organizationData };
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
      const parsedData = typeof dataString === 'string' ? JSON.parse(dataString) : dataString;

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

  private parsePaymentsResponse(
    response: BackendResponse
  ): PaymentsResponse {
    try {
      const dataString = (response as any).data ?? response.jsonData;
      const parsedData = typeof dataString === 'string' ? JSON.parse(dataString) : dataString;

      return {
        paymentsData: parsedData.payments ?? [],
      };
    } catch (error) {
      console.error('Error parsing payments data:', error, (response as any).data ?? response.jsonData);
      throw new Error('Failed to parse payments data');
    }
  }

  /**
   * Gets organization information
   */
  getOrganizationInfo(
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<any> {
    return this.getOrganizationData(pluginId, provider, parameters).pipe(
      retry({ count: 2, delay: 1000 }),
      map(response => this.parseOrganizationResponse(response)),
      catchError((error) => {
        console.error('Failed to load organization info after retries:', error);
        return throwError(() => error);
      })
    );
  }

  saveOrganizationInfo(
    orgInfo: OrganizationData, 
    pluginId: string, 
    provider: string
  ): Observable<any> {
    // Get the required parameters for the API endpoint
    const addressId = orgInfo.organizationId ?? orgInfo.orgNumber;
    
    // Map to the API payload structure with required properties
    const apiPayload = {
      Name: orgInfo.orgName,
      OrganizationType: orgInfo.organizationType,
      OrganizationNumber: orgInfo.orgNumber,
      Status: orgInfo.orgStatus,
      NonRegOrgName: orgInfo.nonRegOrgName,
      OrganizationSize: orgInfo.orgSize,
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
        return throwError(() => error);
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
        return throwError(() => error);
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
        return throwError(() => error);
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
        return throwError(() => error);
      })
    );
  }

  /**
   * Gets payments information
   */
  getPaymentsInfo(
    pluginId: string,
    provider: string,
    parameters?: any
  ): Observable<PaymentsResponse> {
    return this.getPaymentsData(pluginId, provider, parameters).pipe(
      retry({ count: 2, delay: 1000 }),
      map(response => this.parsePaymentsResponse(response)),
      catchError((error) => {
        console.error('Failed to load payments info after retries:', error);
        return throwError(() => error);
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
        return throwError(() => error);
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
      applicantId?: string;
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
        return throwError(() => error);
      })
    );
  }

  /**
   * Sets a contact as primary
   */
  setContactAsPrimary(
    contactId: string,
    pluginId: string,
    provider: string,
    applicantId?: string
  ): Observable<any> {
    const url = `${this.baseUrl}/Contacts/${contactId}/${pluginId}/${provider}/set-primary`;
    const body = applicantId ? { applicantId } : {};
    return this.http.patch<any>(url, body).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to set contact as primary:', error);
        return throwError(() => error);
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
        return throwError(() => error);
      })
    );
  }

  /**
   * Fetches available address types for a workspace
   */
  getAddressTypes(pluginId: string): Observable<any> {
    const url = `${this.baseUrl}/Addresses/${pluginId}/types`;
    return this.http.get<any>(url).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to fetch address types:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Creates a new address
   */
  createAddress(
    pluginId: string,
    provider: string,
    addressData: {
      applicantId?: string;
      addressType: string;
      street: string;
      city: string;
      province: string;
      postalCode: string;
      isPrimary: boolean;
      street2?: string;
      unit?: string;
      country?: string;
    }
  ): Observable<any> {
    const url = `${this.baseUrl}/Addresses/${pluginId}/${provider}`;
    return this.http.post<any>(url, addressData).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to create address:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Deletes an address
   */
  deleteAddress(
    addressId: string,
    pluginId: string,
    provider: string,
    applicantId?: string
  ): Observable<any> {
    const url = `${this.baseUrl}/Addresses/${addressId}/${pluginId}/${provider}`;
    const options = applicantId ? { body: { applicantId } } : {};
    return this.http.delete<any>(url, options).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to delete address:', error);
        return throwError(() => error);
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
      addressType: string;
      street: string;
      city: string;
      province: string;
      postalCode: string;
      isPrimary: boolean;
      street2?: string;
      unit?: string;
      country?: string;
    }
  ): Observable<any> {
    const url = `${this.baseUrl}/Addresses/${addressId}/${pluginId}/${provider}`;
    return this.http.put<any>(url, addressData).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to update address:', error);
        return throwError(() => error);
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
      applicantId?: string;
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
        return throwError(() => error);
      })
    );
  }

  /**
   * Deletes a contact
   */
  deleteContact(
    contactId: string,
    pluginId: string,
    provider: string,
    applicantId?: string
  ): Observable<any> {
    const url = `${this.baseUrl}/Contacts/${contactId}/${pluginId}/${provider}`;
    const options = applicantId ? { body: { applicantId } } : {};
    return this.http.delete<any>(url, options).pipe(
      retry({ count: 1, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to delete contact:', error);
        return throwError(() => error);
      })
    );
  }

  // ── Plugin Events ──────────────────────────────────────────────

  /**
   * Cache of shared polling streams keyed by `${pluginId}|${provider}`.
   * Ensures multiple subscribers (e.g. duplicated notifications dropdowns in
   * mobile + desktop headers) share a single HTTP request per interval
   * instead of each running their own timer.
   */
  private readonly eventsPollers = new Map<string, Observable<PluginEventDto[]>>();
  private static readonly EVENTS_POLL_INTERVAL_MS = 30000;

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
   * Returns a shared polling stream of events for the given workspace/provider.
   * Emits immediately, then every 30s. Multiplexed across all subscribers via
   * shareReplay so we only hit the API once per interval regardless of how
   * many components subscribe. The cache entry is evicted automatically once
   * the last subscriber unsubscribes, so switching workspaces/providers does
   * not leak stale streams.
   */
  pollEvents(pluginId: string, provider: string): Observable<PluginEventDto[]> {
    const key = `${pluginId}|${provider}`;
    const existing = this.eventsPollers.get(key);
    if (existing) {
      return existing;
    }
    let stream!: Observable<PluginEventDto[]>;
    stream = timer(0, ApplicantInfoService.EVENTS_POLL_INTERVAL_MS).pipe(
      switchMap(() => this.getEvents(pluginId, provider)),
      finalize(() => {
        // Only evict if this exact stream is still cached; a concurrent
        // re-subscription may have replaced it.
        if (this.eventsPollers.get(key) === stream) {
          this.eventsPollers.delete(key);
        }
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );
    this.eventsPollers.set(key, stream);
    return stream;
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
