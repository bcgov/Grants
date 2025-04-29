import { Component, OnInit, OnDestroy, inject, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ApiService } from './api.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Container Health Checks';
  
  // Status properties accessed directly by the template
  frontendHealthzColor: string = 'gray';
  frontendHealthzStatus: string = 'Loading...';
  frontendHealthzPayload: string = '';
  
  frontendReadyColor: string = 'gray';
  frontendReadyStatus: string = 'Loading...';
  frontendReadyPayload: string = '';
  
  backendHealthzColor: string = 'gray';
  backendHealthzStatus: string = 'Loading...';
  backendHealthzPayload: string = '';
  
  backendReadyColor: string = 'gray';
  backendReadyStatus: string = 'Loading...';
  backendReadyPayload: string = '';

  // Timer reference for auto-refresh
  private refreshTimer: any;
  private readonly refreshInterval = 5000; // 5 seconds in milliseconds
  private isBrowser: boolean;

  constructor(
    private readonly apiService: ApiService,
    @Inject(PLATFORM_ID) platformId: Object
  ) {
    // Check if we're running in the browser or on the server
    this.isBrowser = isPlatformBrowser(platformId);
  }

  ngOnInit(): void {
    // Only fetch health status and set up timer if running in browser
    if (this.isBrowser) {
      // Initial fetch of health statuses
      this.fetchAllHealthChecks();
      
      // Set up timer for periodic refresh
      this.refreshTimer = setInterval(() => {
        this.fetchAllHealthChecks();
      }, this.refreshInterval);
    } else {
      // If running on server during prerendering, set default values
      this.setDefaultStatusValues();
    }
  }

  ngOnDestroy(): void {
    // Clean up the timer when component is destroyed
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
    }
  }

  // Set default values when running on the server
  private setDefaultStatusValues(): void {
    this.frontendHealthzStatus = 'Prerendering...';
    this.frontendHealthzColor = 'gray';
    this.frontendHealthzPayload = '';
    
    this.frontendReadyStatus = 'Prerendering...';
    this.frontendReadyColor = 'gray';
    this.frontendReadyPayload = '';
    
    this.backendHealthzStatus = 'Prerendering...';
    this.backendHealthzColor = 'gray';
    this.backendHealthzPayload = '';
    
    this.backendReadyStatus = 'Prerendering...';
    this.backendReadyColor = 'gray';
    this.backendReadyPayload = '';
  }

  // Helper method to fetch all health checks at once
  private fetchAllHealthChecks(): void {
    this.fetchFrontendHealthz();
    this.fetchFrontendReady();
    this.fetchBackendHealthz();
    this.fetchBackendReady();
  }

  private fetchFrontendHealthz(): void {
    console.log('Fetching frontend healthz');
    fetch('/healthz', { method: 'GET' })
      .then(response => {
        this.frontendHealthzStatus = response.status === 200 ? '200 OK' : `${response.status} ${response.statusText}`;
        this.frontendHealthzColor = response.status === 200 ? '#00CC00' : '#FF0000';
        return response.text();
      })
      .then(text => {
        this.frontendHealthzPayload = text;
      })
      .catch(error => {
        console.log('Frontend healthz error:', error);
        this.frontendHealthzStatus = 'Error';
        this.frontendHealthzColor = '#FF0000';
        this.frontendHealthzPayload = error.message;
      });
  }

  private fetchFrontendReady(): void {
    console.log('Fetching frontend ready');
    fetch('/healthz/ready', { method: 'GET' })
      .then(response => {
        this.frontendReadyStatus = response.status === 200 ? '200 OK' : `${response.status} ${response.statusText}`;
        this.frontendReadyColor = response.status === 200 ? '#00CC00' : '#FF0000';
        return response.text();
      })
      .then(text => {
        this.frontendReadyPayload = text;
      })
      .catch(error => {
        console.log('Frontend ready error:', error);
        this.frontendReadyStatus = 'Error';
        this.frontendReadyColor = '#FF0000';
        this.frontendReadyPayload = error.message;
      });
  }

  private fetchBackendHealthz(): void {
    console.log('Fetching backend healthz');
    this.apiService.getHealthz().subscribe({
      next: (response) => {
        console.log('Backend healthz success:', response);
        this.backendHealthzStatus = '200 OK';
        this.backendHealthzColor = '#00CC00';
        this.backendHealthzPayload = response.body ?? '';
      },
      error: (error) => {
        console.log('Backend healthz error:', error);
        this.backendHealthzStatus = `${error.status} ${error.statusText}`;
        this.backendHealthzColor = '#FF0000';
        this.backendHealthzPayload = error.error ?? '';
      }
    });
  }

  private fetchBackendReady(): void {
    console.log('Fetching backend ready');
    this.apiService.getHealthzReady().subscribe({
      next: (response) => {
        console.log('Backend ready success:', response);
        this.backendReadyStatus = '200 OK';
        this.backendReadyColor = '#00CC00';
        this.backendReadyPayload = response.body ?? '';
      },
      error: (error) => {
        console.log('Backend ready error:', error);
        this.backendReadyStatus = `${error.status} ${error.statusText}`;
        this.backendReadyColor = '#FF0000';
        this.backendReadyPayload = error.error ?? '';
      }
    });
  }
}
