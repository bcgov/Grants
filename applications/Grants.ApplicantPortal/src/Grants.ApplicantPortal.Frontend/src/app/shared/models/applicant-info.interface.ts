export interface BackendResponse {
  pluginId: string;
  provider: string;
  key: string;
  jsonData: string;
  populatedAt: string;
}

export interface Submission {
  id: string;
  linkId: string;
  receivedTime: string;
  submissionTime: string;
  referenceNo: string;
  projectName: string;
  status: string;
}

export interface Address {
  id: string;
  addressType: string;
  street: string;
  street2: string;
  unit: string;
  city: string;
  province: string;
  postalCode: string;
  country: string;
  isPrimary: boolean;
  isEditable: boolean;
  referenceNo: string;
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

export interface OrganizationData {
  orgName: string;
  orgNumber: string;
  orgStatus: string;
  organizationType: string;
  nonRegOrgName?: string;
  orgSize: string;
  fiscalMonth: string;
  fiscalDay: number;
  fiscalYearEndMonth?: number;
  fiscalYearEndDay?: number;
  organizationId: string;
  legalName: string;
  doingBusinessAs: string;
  ein: string;
  founded: number;
  address: Address;
  contactInfo: ContactInfo;
  submissions?: SubmissionsData[];
  mission: string;
  servicesAreas: string[];
  certifications: Certification[];
  lastUpdated?: string;
  allowEdit?: boolean;
}

export interface SubmissionsData {
  id: string;
  linkId: string;
  receivedTime: string;
  submissionTime: string;
  referenceNo: string;
  projectName: string;
  status: string;
}

// Single response interface for parsed data
export interface OrganizationResponse {
  metadata: {
    pluginId: string;
    provider: string;
    key: string;
    populatedAt: string;
  };
  organizationData: OrganizationData;
}

export interface SubmissionsResponse {
  metadata: {
    pluginId: string;
    provider: string;
    key: string;
    populatedAt: string;
  };
  submissionsData: SubmissionsData[];
  linkSource?: string;
}

export interface OrgSearchResult {
  id: string;
  orgName: string;
  orgNumber: string;
  orgStatus: string;
  organizationType: string;
}

// Plugin Events
export type EventSeverity = 'Error' | 'Warning' | 'Info';

export interface PluginEventDto {
  eventId: string;
  severity: EventSeverity;
  userMessage: string;
  createdAt: string;
  acknowledgedAt?: string | null;
}

export interface PluginEventsResponse {
  events: PluginEventDto[];
}
