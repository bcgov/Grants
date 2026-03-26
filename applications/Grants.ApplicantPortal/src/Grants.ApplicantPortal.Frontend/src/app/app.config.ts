import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import {
  provideHttpClient,
  withInterceptorsFromDi,
  withFetch,
  HTTP_INTERCEPTORS,
} from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';

import { routes } from './app.routes';
import {
  provideClientHydration,
  withEventReplay,
} from '@angular/platform-browser';
import { authConfig } from './core/auth/auth.config';
import { provideAuth } from 'angular-auth-oidc-client';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { provideMatomo } from 'ngx-matomo-client/core';
import { withRouter } from 'ngx-matomo-client/router';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAuth(authConfig),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    // Hydration removed - not needed for SPA-only mode
    provideHttpClient(withInterceptorsFromDi(), withFetch()),
    provideAnimations(),
    // Register the auth interceptor
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true,
    },
    // Matomo analytics with automatic router tracking
    provideMatomo(
      {
        siteId: environment.matomo.siteId,
        trackerUrl: environment.matomo.url,
        disabled: !environment.matomo.enabled,
      },
      withRouter()
    ),
  ],
};
