import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { ApplicantInfoService } from './applicant-info.service';
import { environment } from '../../../environments/environment';
import { BackendResponse, PluginEventsResponse } from '../../shared/models/applicant-info.interface';

const BASE = environment.apiUrl;

function makeBackendResponse(jsonData: string | object): BackendResponse {
  return {
    pluginId: 'plugin-1',
    provider: 'prov-1',
    key: 'key',
    jsonData: typeof jsonData === 'string' ? jsonData : JSON.stringify(jsonData),
    populatedAt: new Date().toISOString(),
  };
}

describe('ApplicantInfoService', () => {
  let service: ApplicantInfoService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApplicantInfoService],
    });

    service = TestBed.inject(ApplicantInfoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getOrganizationInfo', () => {
    it('sends GET request to /Organizations/:pluginId/:provider', () => {
      service.getOrganizationInfo('plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Organizations/plugin-1/prov-1`);
      expect(req.request.method).toBe('GET');
      req.flush(makeBackendResponse({ organizations: [] }));
    });

    it('parses organizations array from response', (done) => {
      const org = { id: 'org-1', orgName: 'Org One', orgNumber: '1234' };
      service.getOrganizationInfo('plugin-1', 'prov-1').subscribe((result) => {
        expect(result.organizationsData.length).toBe(1);
        expect(result.organizationsData[0].id).toBe('org-1');
        done();
      });

      const req = httpMock.expectOne(`${BASE}/Organizations/plugin-1/prov-1`);
      req.flush(makeBackendResponse({ organizations: [org] }));
    });
  });

  describe('getSubmissionsInfo', () => {
    it('sends GET request to /Submissions/:pluginId/:provider', () => {
      service.getSubmissionsInfo('plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1`);
      expect(req.request.method).toBe('GET');
      req.flush(makeBackendResponse({ submissions: [] }));
    });

    it('returns empty submissionsData when response has no submissions', (done) => {
      service.getSubmissionsInfo('plugin-1', 'prov-1').subscribe((result) => {
        expect(result.submissionsData).toEqual([]);
        done();
      });

      const req = httpMock.expectOne(`${BASE}/Submissions/plugin-1/prov-1`);
      req.flush(makeBackendResponse({ submissions: [] }));
    });
  });

  describe('getContactsInfo', () => {
    it('sends GET request to /Contacts/:pluginId/:provider', () => {
      service.getContactsInfo('plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Contacts/plugin-1/prov-1`);
      expect(req.request.method).toBe('GET');
      req.flush(makeBackendResponse({ contacts: [] }));
    });

    it('parses contacts from response', (done) => {
      const contact = { name: 'John', email: 'john@example.com' };
      service.getContactsInfo('plugin-1', 'prov-1').subscribe((result) => {
        expect(result.contactsData.length).toBe(1);
        done();
      });

      const req = httpMock.expectOne(`${BASE}/Contacts/plugin-1/prov-1`);
      req.flush(makeBackendResponse({ contacts: [contact] }));
    });
  });

  describe('getAddressesInfo', () => {
    it('sends GET request to /Addresses/:pluginId/:provider', () => {
      service.getAddressesInfo('plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Addresses/plugin-1/prov-1`);
      expect(req.request.method).toBe('GET');
      req.flush(makeBackendResponse({ addresses: [] }));
    });

    it('parses addresses from response', (done) => {
      const address = { street: '123 Main St', city: 'Victoria' };
      service.getAddressesInfo('plugin-1', 'prov-1').subscribe((result) => {
        expect(result.addressesData.length).toBe(1);
        done();
      });

      const req = httpMock.expectOne(`${BASE}/Addresses/plugin-1/prov-1`);
      req.flush(makeBackendResponse({ addresses: [address] }));
    });
  });

  describe('getPaymentsInfo', () => {
    it('sends GET request to /Payments/:pluginId/:provider', () => {
      service.getPaymentsInfo('plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Payments/plugin-1/prov-1`);
      expect(req.request.method).toBe('GET');
      req.flush(makeBackendResponse({ payments: [] }));
    });

    it('returns paymentsData array', (done) => {
      const payment = { id: 'pay-1', amount: 100 };
      service.getPaymentsInfo('plugin-1', 'prov-1').subscribe((result) => {
        expect(result.paymentsData.length).toBe(1);
        done();
      });

      const req = httpMock.expectOne(`${BASE}/Payments/plugin-1/prov-1`);
      req.flush(makeBackendResponse({ payments: [payment] }));
    });
  });

  describe('saveOrganizationInfo', () => {
    it('sends PUT request to /Organizations/:addressId/:pluginId/:provider', () => {
      const orgData: any = {
        organizationId: 'org-id-1',
        orgName: 'Test Org',
        organizationType: 'Non-Profit',
        orgNumber: 'BC1234',
        orgStatus: 'Active',
        orgSize: 10,
        fiscalMonth: 'January',
        fiscalDay: 1,
      };

      service.saveOrganizationInfo(orgData, 'plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Organizations/org-id-1/plugin-1/prov-1`);
      expect(req.request.method).toBe('PUT');
      req.flush({ success: true });
    });
  });

  describe('createContact', () => {
    it('sends POST request to /Contacts/:pluginId/:provider', () => {
      const contactData = {
        name: 'Jane',
        email: 'jane@example.com',
        role: 'Primary',
        isPrimary: true,
      };

      service.createContact('plugin-1', 'prov-1', contactData).subscribe();

      const req = httpMock.expectOne(`${BASE}/Contacts/plugin-1/prov-1`);
      expect(req.request.method).toBe('POST');
      req.flush({});
    });
  });

  describe('createAddress', () => {
    it('sends POST request to /Addresses/:pluginId/:provider', () => {
      const addressData = {
        addressType: 'Mailing',
        street: '1 Main St',
        city: 'Victoria',
        province: 'BC',
        postalCode: 'V1A 1A1',
        isPrimary: false,
      };

      service.createAddress('plugin-1', 'prov-1', addressData).subscribe();

      const req = httpMock.expectOne(`${BASE}/Addresses/plugin-1/prov-1`);
      expect(req.request.method).toBe('POST');
      req.flush({});
    });
  });

  describe('deleteAddress', () => {
    it('sends DELETE request to /Addresses/:addressId/:pluginId/:provider', () => {
      service.deleteAddress('addr-1', 'plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Addresses/addr-1/plugin-1/prov-1`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });
  });

  describe('deleteContact', () => {
    it('sends DELETE request to /Contacts/:contactId/:pluginId/:provider', () => {
      service.deleteContact('con-1', 'plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Contacts/con-1/plugin-1/prov-1`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });
  });

  describe('getEvents', () => {
    it('sends GET request to /Events/:pluginId/:provider', () => {
      service.getEvents('plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Events/plugin-1/prov-1`);
      expect(req.request.method).toBe('GET');
      const eventsResponse: PluginEventsResponse = { events: [] };
      req.flush(eventsResponse);
    });

    it('returns empty array on HTTP error (fails silently)', (done) => {
      service.getEvents('plugin-1', 'prov-1').subscribe((events) => {
        expect(events).toEqual([]);
        done();
      });

      const req = httpMock.expectOne(`${BASE}/Events/plugin-1/prov-1`);
      req.flush('server error', { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('acknowledgeEvent', () => {
    it('sends PATCH request to /Events/:eventId/acknowledge', () => {
      service.acknowledgeEvent('event-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Events/event-1/acknowledge`);
      expect(req.request.method).toBe('PATCH');
      req.flush({});
    });
  });

  describe('acknowledgeAllEvents', () => {
    it('sends PATCH request to /Events/:pluginId/:provider/acknowledge-all', () => {
      service.acknowledgeAllEvents('plugin-1', 'prov-1').subscribe();

      const req = httpMock.expectOne(`${BASE}/Events/plugin-1/prov-1/acknowledge-all`);
      expect(req.request.method).toBe('PATCH');
      req.flush({});
    });
  });
});
