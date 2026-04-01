export const environment = {
  production: false,
  apiUrl: '/api', // Use proxy for Docker development
  orgbookApiUrl: 'https://orgbook.gov.bc.ca/api',
  matomo: {
    enabled: true,
    url: '//dev-analytics-matomo.apps.silver.devops.gov.bc.ca/',
    siteId: '2'
  },
  keycloak: {
    authority: 'https://dev.loginproxy.gov.bc.ca/auth',
    realm: 'standard',
    clientId: 'grants-portal-5361'    
  }
};