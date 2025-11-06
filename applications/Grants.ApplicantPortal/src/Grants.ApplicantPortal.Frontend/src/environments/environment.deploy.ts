export const environment = {
  production: true,
  apiUrl: '/api', // This will be proxied by the frontend server or handled by OpenShift routing
  keycloak: {
    authority: '${KEYCLOAK__AUTHSERVERURL}',
    realm: '${KEYCLOAK__REALM}',
    clientId: '${KEYCLOAK__RESOURCE}',
    clientSecret: '${KEYCLOAK__CREDENTIALS__SECRET}'
  }
};
