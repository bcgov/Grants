export const environment = {
  production: false,
  apiUrl: 'https://localhost:7000', // Direct connection to backend for local development
  orgbookApiUrl: 'https://orgbook.gov.bc.ca/api',
  matomo: {
    enabled: false,
    url: '//dev-analytics-matomo.apps.silver.devops.gov.bc.ca/',
    siteId: '2'
  },
  keycloak: {
    authority: 'https://dev.loginproxy.gov.bc.ca/auth',
    realm: 'standard',
    clientId: 'grants-portal-5361'    
  }
};
