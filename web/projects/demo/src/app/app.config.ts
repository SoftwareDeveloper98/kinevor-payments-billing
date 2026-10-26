import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { provideBilling } from '@softwaredeveloper98/kinevor-payments-billing-web';
import { appRoutes } from './app.routes';
import { demoApiInterceptor } from './demo-api.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([demoApiInterceptor])),
    provideBilling({ apiBaseUrl: 'http://localhost:5087/api/v1' }),
  ],
};
