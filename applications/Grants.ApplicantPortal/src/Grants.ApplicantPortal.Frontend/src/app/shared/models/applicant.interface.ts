export interface ApplicantInfo {
  id: string;
  organization: string;
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
