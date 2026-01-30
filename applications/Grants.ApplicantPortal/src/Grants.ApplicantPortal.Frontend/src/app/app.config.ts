import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
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
import { StorageCleanupService } from './core/services/storage-cleanup.service';

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
    // Initialize storage cleanup service
    {
      provide: APP_INITIALIZER,
      useFactory: (storageCleanupService: StorageCleanupService) => () => {
        storageCleanupService.initialize();
      },
      deps: [StorageCleanupService],
      multi: true,
    },
  ],
};
