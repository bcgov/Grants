export const environment = {
  production: false,
  apiUrl: '/api', // Use proxy for Docker development
  keycloak: {
    authority: 'https://dev.loginproxy.gov.bc.ca/auth',
    realm: 'standard',
    clientId: 'grants-portal-5361',
    clientSecret: 'placeholder-client-secret'
  }
};