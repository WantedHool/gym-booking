import { InjectionToken } from '@angular/core';

// Κενό string = relative paths (dev, μέσω proxy.conf.js). Production apps παρέχουν την
// πραγματική τιμή από το δικό τους environment.ts (βλ. app.config.ts).
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '',
});
