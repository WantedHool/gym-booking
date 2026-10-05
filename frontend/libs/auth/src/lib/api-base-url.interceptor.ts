import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE_URL } from './api-base-url.token';

export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const baseUrl = inject(API_BASE_URL);
  if (!baseUrl || !req.url.startsWith('/')) {
    return next(req);
  }
  return next(req.clone({ url: `${baseUrl}${req.url}` }));
};
