export interface BackendResponse {
  profileId: string;
  pluginId: string;
  provider: string;
  key: string;
  jsonData: string;
  populatedAt: string;
}

export interface Submission {
  applicationId: string;
  submissionId: string;
  submissionDate: Date;
  projectName: string;
  status: string;
  updatedOn?: string;
  paidAmount?: number;
  submissionLink?: string;
}

export interface Address {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
}

export interface Contact {
  name: string;
  title: string;
  email: string;
  phone: string;
}

export interface ContactInfo {
  primaryContact: Contact;
  grantsContact: Contact;
}

export interface Certification {
  type: string;
  validUntil: string;
}

export interface Program1Specific {
  eligibilityStatus: string;
  lastAuditDate: string;
  complianceScore: number;
  specialDesignations: string[];
}

export interface OrganizationData {
  orgName: string;
  orgNumber: string;
  orgStatus: string;
  organizationType: string;
  nonRegOrgName: string;
  orgSize: string;
  fiscalMonth: string;
  fiscalDay: number;
  organizationId: string;
  legalName: string;
  doingBusinessAs: string;
  ein: string;
  founded: number;
  address: Address;
  contactInfo: ContactInfo;
  mission: string;
  servicesAreas: string[];
  certifications: Certification[];
  program1Specific: Program1Specific;
}

export interface SubmissionsData {
  submissionId: string;
  applicationId: string;
  projectName: string;
  programName: string;
  requestedAmount: number;
  paidAmount: number;
  status: string;
  submissionDate: Date;
  lastModified: Date;
}

// Single response interface for parsed data
export interface OrganizationResponse {
  metadata: {
    profileId: string;
    pluginId: string;
    provider: string;
    key: string;
    populatedAt: string;
  };
  organizationData: OrganizationData;
}

export interface SubmissionsResponse {
  metadata: {
    profileId: string;
    pluginId: string;
    provider: string;
    key: string;
    populatedAt: string;
  };
  submissionsData: SubmissionsData;
}

export interface OrgSearchResult {
  id: string;
  orgName: string;
  orgNumber: string;
  orgStatus: string;
  organizationType: string;
}
