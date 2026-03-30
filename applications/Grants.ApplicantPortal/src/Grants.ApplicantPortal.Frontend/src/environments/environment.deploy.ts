export const environment = {
  production: true,
  apiUrl: '/api', // This will be proxied by the frontend server or handled by OpenShift routing
  orgbookApiUrl: 'https://orgbook.gov.bc.ca/api',
  matomo: {
    enabled: true,
    url: '${MATOMO__URL}',
    siteId: '${MATOMO__SITEID}'
  },
  keycloak: {
    authority: '${KEYCLOAK__AUTHSERVERURL}',
    realm: '${KEYCLOAK__REALM}',
    clientId: '${KEYCLOAK__RESOURCE}',
    clientSecret: '${KEYCLOAK__CREDENTIALS__SECRET}'
  }
};
