export const environment = {
  production: false,
  apiUrl: 'https://localhost:7000', // Direct connection to backend for local development
  keycloak: {
    authority: 'https://dev.loginproxy.gov.bc.ca/auth',
    realm: 'standard',
    clientId: 'grants-portal-5361'    
  }
};
