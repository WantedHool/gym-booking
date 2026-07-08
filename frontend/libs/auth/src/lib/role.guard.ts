import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export function roleGuard(...allowedRoles: string[]): CanActivateFn {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const allowed =
      authService.isAuthenticated() && allowedRoles.some((role) => authService.hasRole(role));
    return allowed || router.createUrlTree(['/login']);
  };
}
