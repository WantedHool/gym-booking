import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { API_BASE_URL, apiBaseUrlInterceptor, jwtInterceptor } from '@frontend/auth';
import { environment } from '../environments/environment';
import { appRoutes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(appRoutes),
    provideHttpClient(withInterceptors([apiBaseUrlInterceptor, jwtInterceptor])),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
  ]
};
