import { InjectionToken } from '@angular/core';

/**
 * Base URL of the Product Catalog API. Defaults to a relative path, which the dev server
 * proxies to the backend (see proxy.conf.json) and a reverse proxy can serve in production.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
