import { provideServerRendering, withRoutes } from '@angular/ssr';
import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';

const serverConfig: ApplicationConfig = {
  providers: [provideServerRendering(withRoutes(serverRoutes)), provideHttpClient(withInterceptorsFromDi())]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
