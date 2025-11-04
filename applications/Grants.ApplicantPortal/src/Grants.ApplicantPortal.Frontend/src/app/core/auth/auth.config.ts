import { PassedInitialConfig, LogLevel } from 'angular-auth-oidc-client';
import { environment } from '../../../environments/environment';

export const authConfig: PassedInitialConfig = {
  config: {
    authority: environment.keycloakAuthority, // using environment variable
    redirectUrl: window.location.origin + '/auth/callback', // using dev for now using port 3000.It is available in CSS for now , Need to add 4000/ 4200 redirect
    postLogoutRedirectUri: window.location.origin + '/login',
    clientId: 'grants-portal-5361', //Resource
    scope: 'openid profile email', // Adjust scopes as needed
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    logLevel: LogLevel.Debug,
    secureRoutes: [environment.apiUrl + '/api'],
  },
};
