import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';

import { ApplicantService } from './applicant.service';
import { ApplicantInfo, ContactInfo, AddressInfo } from '../../shared/models/applicant.interface';

describe('ApplicantService', () => {
  let service: ApplicantService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApplicantService],
    });

    service = TestBed.inject(ApplicantService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getApplicantInfo', () => {
    it('emits applicant info with id and organization', (done) => {
      service.getApplicantInfo().subscribe((info: ApplicantInfo) => {
        expect(info.id).toBe('368GB783');
        expect(info.organization).toBe('Your Amazing Organization Inc.');
        done();
      });
    });
  });

  describe('getContactInfo', () => {
    it('emits a list of contacts', (done) => {
      service.getContactInfo().subscribe((contacts: ContactInfo[]) => {
        expect(contacts.length).toBeGreaterThan(0);
        done();
      });
    });

    it('emits contacts with required fields', (done) => {
      service.getContactInfo().subscribe((contacts: ContactInfo[]) => {
        contacts.forEach((c) => {
          expect(c.firstName).toBeDefined();
          expect(c.lastName).toBeDefined();
          expect(c.email).toBeDefined();
        });
        done();
      });
    });
  });

  describe('getAddressInfo', () => {
    it('emits a list of addresses', (done) => {
      service.getAddressInfo().subscribe((addresses: AddressInfo[]) => {
        expect(addresses.length).toBeGreaterThan(0);
        done();
      });
    });

    it('emits addresses with required fields', (done) => {
      service.getAddressInfo().subscribe((addresses: AddressInfo[]) => {
        addresses.forEach((a) => {
          expect(a.type).toBeDefined();
          expect(a.address).toBeDefined();
          expect(a.city).toBeDefined();
          expect(a.province).toBeDefined();
          expect(a.postalCode).toBeDefined();
        });
        done();
      });
    });
  });
});
