import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE_URL } from './api-base-url.token';

// Κάνει prepend το API base URL σε relative requests (π.χ. '/auth/login' -> 'https://api.../auth/login').
// Χρειάζεται σε production όπου το frontend και το API είναι σε ξεχωριστά origins (CORS).
export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const baseUrl = inject(API_BASE_URL);
  if (!baseUrl || !req.url.startsWith('/')) {
    return next(req);
  }
  return next(req.clone({ url: `${baseUrl}${req.url}` }));
};
