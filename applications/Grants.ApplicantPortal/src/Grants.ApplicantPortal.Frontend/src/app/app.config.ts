import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import {
  provideHttpClient,
  withInterceptorsFromDi,
  withFetch,
} from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';

import { routes } from './app.routes';
import {
  provideClientHydration,
  withEventReplay,
} from '@angular/platform-browser';
import { authConfig } from './core/auth/auth.config';
import { provideAuth } from 'angular-auth-oidc-client';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAuth(authConfig),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    // Hydration removed - not needed for SPA-only mode
    provideHttpClient(withInterceptorsFromDi(), withFetch()),
    provideAnimations(),
  ],
};
