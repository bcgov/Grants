export const environment = {
  production: true,
  apiUrl: '/api', // This will be proxied by the frontend server
  keycloakAuthority: '${KEYCLOAK__AUTHSERVERURL}', // Set by OpenShift
};
