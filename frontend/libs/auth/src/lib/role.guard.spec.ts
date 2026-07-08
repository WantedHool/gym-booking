import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { roleGuard } from './role.guard';
import { AuthService } from './auth.service';

describe('roleGuard', () => {
  function runGuard(allowedRoles: string[]) {
    return TestBed.runInInjectionContext(() => roleGuard(...allowedRoles)({} as never, {} as never));
  }

  it('allows navigation when the user has one of the allowed roles', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => true, hasRole: (role: string) => role === 'Admin' },
        },
      ],
    });

    expect(runGuard(['Admin', 'Instructor'])).toBe(true);
  });

  it('redirects to /login when the user lacks an allowed role', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => true, hasRole: () => false },
        },
      ],
    });

    const result = runGuard(['Admin']);
    const router = TestBed.inject(Router);

    expect(result).toEqual(router.createUrlTree(['/login']));
  });

  it('redirects to /login when the user is not authenticated', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => false, hasRole: () => true },
        },
      ],
    });

    const result = runGuard(['Admin']);
    const router = TestBed.inject(Router);

    expect(result).toEqual(router.createUrlTree(['/login']));
  });
});
