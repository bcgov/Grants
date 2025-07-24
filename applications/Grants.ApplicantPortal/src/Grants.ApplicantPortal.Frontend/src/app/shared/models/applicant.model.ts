export interface ApplicantInfo {
  id: string;
  organization: string;
}

export interface OrganizationInfo {
  orgRegisteredNumber: string;
  orgStatus: string;
  orgType: string;
  orgName: string;
  orgSize: string;
  nonRegOrgName: string;
  fiscalYearEndMonth?: string;
  fiscalYearEndDay?: string;
}

export interface ContactInfo {
  firstName: string;
  lastName: string;
  name: string;
  email: string;
  phone: string;
  title: string;
  type: string;
}

export interface AddressInfo {
  type: string;
  address: string;
  city: string;
  province: string;
  postalCode: string;
}

export interface Submission {
  date: string;
  id: string;
  projectName: string;
  title: string;
  submissionDate: Date;
  status:
    | 'In Progress'
    | 'Approved'
    | 'Declined'
    | 'Submitted'
    | 'Under Review';
}
