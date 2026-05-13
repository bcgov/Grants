import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { ApiService } from './api.service';
import { environment } from '../environments/environment';

describe('ApiService', () => {
  let service: ApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApiService],
    });

    service = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getHealthz', () => {
    it('sends GET request to /healthz', () => {
      service.getHealthz().subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/healthz`);
      expect(req.request.method).toBe('GET');
      req.flush({});
    });

    it('emits the response', () => {
      const mockBody = { status: 'ok' };
      let emittedResponse: any;

      service.getHealthz().subscribe((res) => {
        emittedResponse = res;
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/healthz`);
      req.flush(mockBody);

      expect(emittedResponse.body).toEqual(mockBody);
    });
  });

  describe('getHealthzReady', () => {
    it('sends GET request to /healthz/ready', () => {
      service.getHealthzReady().subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/healthz/ready`);
      expect(req.request.method).toBe('GET');
      req.flush({});
    });

    it('emits the response', () => {
      const mockBody = { ready: true };
      let emittedResponse: any;

      service.getHealthzReady().subscribe((res) => {
        emittedResponse = res;
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/healthz/ready`);
      req.flush(mockBody);

      expect(emittedResponse.body).toEqual(mockBody);
    });
  });

  describe('getExampleComStatus', () => {
    it('sends GET request to https://www.example.com', () => {
      service.getExampleComStatus().subscribe();

      const req = httpMock.expectOne('https://www.example.com');
      expect(req.request.method).toBe('GET');
      req.flush({});
    });
  });
});
