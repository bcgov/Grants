import { PassedInitialConfig, LogLevel } from 'angular-auth-oidc-client';
import { environment } from '../../../environments/environment';

export const authConfig: PassedInitialConfig = {
  config: {
    authority: `${environment.keycloak.authority}/realms/${environment.keycloak.realm}`,
    redirectUrl: window.location.origin + '/auth/callback',
    postLogoutRedirectUri: window.location.origin + '/login',
    clientId: environment.keycloak.clientId,
    scope: 'openid profile email',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    logLevel: LogLevel.Debug,
    secureRoutes: [environment.apiUrl],
  },
};
