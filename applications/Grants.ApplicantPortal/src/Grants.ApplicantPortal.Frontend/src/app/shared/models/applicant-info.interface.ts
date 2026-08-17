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
  type: string;
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
  orgSize: number | null;
  fiscalMonth: string | null;
  fiscalDay: number | null;
  fiscalYearEndMonth?: number | null;
  fiscalYearEndDay?: number | null;
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

export interface SubmissionLink {
  uri: string;
  title: string;
  description: string;
  order: number;
}

export interface SubmissionsData {
  id: string;
  linkId: string;
  receivedTime: string;
  submissionTime: string;
  referenceNo: string;
  type: string;
  status: string;
  /** Nullable — only present when eligible for renewal and a Published Renewal link exists. */
  renewalLink?: SubmissionLink | null;
  /** Always present, may be empty. Ordered by `order` asc, with order === -1 sorted last. */
  relatedLinks?: SubmissionLink[];
  /** Derived client-side: true when renewalLink, a non-empty relatedLinks, applicantMessage, or eligibleForRenewal is present/true. */
  hasRelatedLinks?: boolean;
  /** Nullable free-text feedback for this submission. */
  applicantMessage?: string | null;
  /** Renewal eligibility, independent of whether a Published Renewal link exists. */
  eligibleForRenewal?: boolean;
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

export interface PaymentData {
  id: string;
  paymentNumber: string;
  referenceNo: string;
  amount: number;
  paymentDate: string | null;
  paymentStatus: string;
}

export interface PaymentsResponse {
  paymentsData: PaymentData[];
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

// Orgbook API Response
export interface OrgbookOrganization {
  id: string;
  orgName: string | null;
  organizationType: string | null;
  orgNumber: string | null;
  orgStatus: string | null;
  nonRegOrgName: string | null;
  fiscalMonth: string | null;
  fiscalDay: number | null;
  organizationSize: string | number | null;
  sector: string | null;
  subSector: string | null;
}

export interface OrgbookResponse {
  profileId: string;
  pluginId: string;
  provider: string;
  data: {
    organizations: OrgbookOrganization[];
  };
  populatedAt: string;
  cacheStatus: string;
  cacheStore: string;
}
