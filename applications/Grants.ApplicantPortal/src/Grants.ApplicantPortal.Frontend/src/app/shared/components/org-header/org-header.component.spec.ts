import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OrgHeaderComponent } from './org-header.component';

describe('OrgHeaderComponent', () => {
  let component: OrgHeaderComponent;
  let fixture: ComponentFixture<OrgHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrgHeaderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(OrgHeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('defaults displayMode to "applicant"', () => {
    expect(component.displayMode).toBe('applicant');
  });

  it('defaults hasMultipleOrgs to false', () => {
    expect(component.hasMultipleOrgs).toBeFalse();
  });

  describe('displayId', () => {
    it('returns applicantRefId when displayMode is "applicant"', () => {
      component.displayMode = 'applicant';
      component.applicantRefId = 'REF-123';
      component.orgNumber = 'ORG-456';
      expect(component.displayId).toBe('REF-123');
    });

    it('returns orgNumber when displayMode is "org"', () => {
      component.displayMode = 'org';
      component.applicantRefId = 'REF-123';
      component.orgNumber = 'ORG-456';
      expect(component.displayId).toBe('ORG-456');
    });
  });

  describe('displayName', () => {
    it('returns applicantName when displayMode is "applicant"', () => {
      component.displayMode = 'applicant';
      component.applicantName = 'John Doe';
      component.orgName = 'Acme Corp';
      expect(component.displayName).toBe('John Doe');
    });

    it('returns orgName when displayMode is "org"', () => {
      component.displayMode = 'org';
      component.applicantName = 'John Doe';
      component.orgName = 'Acme Corp';
      expect(component.displayName).toBe('Acme Corp');
    });
  });

  describe('@Input bindings', () => {
    it('accepts orgNumber input', () => {
      component.orgNumber = 'BC1234';
      expect(component.orgNumber).toBe('BC1234');
    });

    it('accepts tenantEmail input', () => {
      component.tenantEmail = 'admin@example.com';
      expect(component.tenantEmail).toBe('admin@example.com');
    });
  });
});
