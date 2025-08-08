import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import {
  ProfileId,
  PluginId,
  Provider,
  Key,
} from '../../shared/models/organization.enums';
import {
  BackendResponse,
  HydrateRequest,
  OrganizationResponse,
} from '../../shared/models/applicant-info.interface';

@Injectable({
  providedIn: 'root',
})
export class OrganizationService {
  private readonly baseUrl = 'https://localhost:7000';

  // Default parameters as constants
  private readonly defaults = {
    profileId: ProfileId.DEFAULT,
    pluginId: PluginId.DEMO,
    provider: Provider.DEMO,
    key: Key.ORGINFO,
  };

  constructor(private readonly http: HttpClient) {}

  /**
   * Hydrates organization information
   */
  private hydrateOrganizationInfo(data?: any): Observable<BackendResponse> {
    const { profileId, pluginId, provider, key } = this.defaults;
    const url = `${this.baseUrl}/Profiles/${profileId}/${pluginId}/${provider}/${key}/hydrate`;
    const body: HydrateRequest = { data };
    return this.http.post<BackendResponse>(url, body);
  }

  /**
   * Gets organization information from backend
   */
  private getOrganizationInfo(): Observable<BackendResponse> {
    const { profileId, pluginId, provider, key } = this.defaults;
    const url = `${this.baseUrl}/Profiles/${profileId}/${pluginId}/${provider}/${key}`;
    return this.http.get<BackendResponse>(url);
  }

  /**
   * Parses backend response and extracts organization data
   */
  private parseResponse(response: BackendResponse): OrganizationResponse {
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

  /**
   * Main method: Hydrates and returns formatted organization information
   * This is the only public method you need to call
   */
  hydrateAndGetOrganizationInfo(data?: any): Observable<OrganizationResponse> {
    return this.hydrateOrganizationInfo(data).pipe(
      switchMap(() => this.getOrganizationInfo()),
      map((response) => this.parseResponse(response))
    );
  }

  /**
   * Gets already hydrated organization information (without hydrating first)
   */
  getFormattedOrganizationInfo(): Observable<OrganizationResponse> {
    return this.getOrganizationInfo().pipe(
      map((response) => this.parseResponse(response))
    );
  }
}
