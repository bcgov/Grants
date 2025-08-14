import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, switchMap, retry, catchError } from 'rxjs/operators';
import {
  BackendResponse,
  HydrateRequest,
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
   * Hydrates profile information
   */
  private hydrateInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    key: string,
    data?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Profiles/${profileId}/${pluginId}/${provider}/${key}/hydrate`;
    const body: HydrateRequest = { data };
    return this.http.post<BackendResponse>(url, body);
  }

  /**
   * Gets profile information
   */
  private getInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    key: string
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Profiles/${profileId}/${pluginId}/${provider}/${key}`;
    return this.http.get<BackendResponse>(url);
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
   * Main method: Hydrates and returns formatted organization information
   * This is the only public method you need to call
   */
  hydrateAndGetOrganizationInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    key: string,
    data?: any
  ): Observable<OrganizationResponse> {
    return this.hydrateInfo(profileId, pluginId, provider, key, data).pipe(
      switchMap(() => this.getInfo(profileId, pluginId, provider, key)),
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
  hydrateAndGetSubmissionsInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    key: string,
    data?: any
  ): Observable<SubmissionsResponse> {
    return this.hydrateInfo(profileId, pluginId, provider, key, data).pipe(
      switchMap(() => this.getInfo(profileId, pluginId, provider, key)),
      map((response) => this.parseSubmissionsResponse(response)),
      retry({ count: 2, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to load submissions info after retries:', error);
        throw error;
      })
    );
  }
}
