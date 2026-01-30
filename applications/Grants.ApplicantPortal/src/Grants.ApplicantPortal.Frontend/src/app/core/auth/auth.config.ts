import { PassedInitialConfig, LogLevel } from 'angular-auth-oidc-client';
import { environment } from '../../../environments/environment';

export const authConfig: PassedInitialConfig = {
  config: {
    authority: `${environment.keycloak.authority}/realms/${environment.keycloak.realm}`,
    redirectUrl: window.location.origin + '/auth/callback',
    postLogoutRedirectUri: window.location.origin + '/login',
    clientId: environment.keycloak.clientId,
    scope: 'openid profile email offline_access',
    responseType: 'code',
    silentRenew: true,
    silentRenewUrl: window.location.origin + '/auth/callback',
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 60,
    // Reduce log level to prevent localStorage bloat
    logLevel: LogLevel.Warn,
    secureRoutes: [environment.apiUrl],
    autoUserInfo: false,
    triggerAuthorizationResultEvent: true,
    startCheckSession: false,
    // Improve stability and reduce nonce issues
    historyCleanupOff: false,
    silentRenewTimeoutInSeconds: 60,
    // Enable automatic cleanup to prevent storage accumulation
    autoCleanStateAfterAuthentication: true,
    // Improve token handling
    ignoreNonceAfterRefresh: true,
    // Prevent multiple simultaneous auth operations
    disablePkce: false,
    // Better error handling
    forbiddenRoute: '/login',
    unauthorizedRoute: '/login',
    // Additional cleanup options to prevent header accumulation
    maxIdTokenIatOffsetAllowedInSeconds: 120,
    renewUserInfoAfterTokenRenew: false
  },
};
