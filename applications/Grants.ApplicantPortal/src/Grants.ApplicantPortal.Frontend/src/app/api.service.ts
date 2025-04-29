import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getHealthz(): Observable<any> {
    return this.http.get(`${this.apiUrl}/healthz`, { observe: 'response' });
  }

  getHealthzReady(): Observable<any> {
    return this.http.get(`${this.apiUrl}/healthz/ready`, { observe: 'response' });
  }

  getExampleComStatus(): Observable<any> {
    return this.http.get('https://www.example.com', { observe: 'response' });
  }
}