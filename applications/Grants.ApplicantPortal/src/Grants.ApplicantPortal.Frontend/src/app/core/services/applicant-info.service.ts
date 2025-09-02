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
   * Gets profile information
   */
  private getInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    key: string,
    parameters?: any
  ): Observable<BackendResponse> {
    const url = `${this.baseUrl}/Profiles/${profileId}/${pluginId}/${provider}/${key}`;
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
   * This is the only public method you need to call
   */
  getOrganizationInfo(
    profileId: string,
    pluginId: string,
    provider: string,
    key: string,
    parameters?: any
  ): Observable<OrganizationResponse> {
    return this.getInfo(profileId, pluginId, provider, key, parameters).pipe(
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
    key: string,
    parameters?: any
  ): Observable<SubmissionsResponse> {
    return this.getInfo(profileId, pluginId, provider, key, parameters).pipe(      
      map((response) => this.parseSubmissionsResponse(response)),
      retry({ count: 2, delay: 1000 }),
      catchError((error) => {
        console.error('Failed to load submissions info after retries:', error);
        throw error;
      })
    );
  }
}
