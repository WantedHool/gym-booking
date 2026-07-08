import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthApiService } from '@frontend/data-access';
import { ACCESS_TOKEN_KEY, AuthService } from './auth.service';

function makeToken(payload: Record<string, unknown>): string {
  const base64url = (value: unknown) =>
    btoa(JSON.stringify(value))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
  return `${base64url({ alg: 'HS256' })}.${base64url(payload)}.signature`;
}

describe('AuthService', () => {
  afterEach(() => localStorage.clear());

  function createService(authApi: Partial<AuthApiService> = {}): AuthService {
    return TestBed.configureTestingModule({
      providers: [{ provide: AuthApiService, useValue: authApi }],
    }).inject(AuthService);
  }

  it('starts unauthenticated when no token is stored', () => {
    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.user()).toBeNull();
  });

  it('restores the user from a valid stored token', () => {
    const token = makeToken({
      sub: 'u1',
      tenantId: 't1',
      role: 'Admin',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    localStorage.setItem(ACCESS_TOKEN_KEY, token);

    const service = createService();

    expect(service.isAuthenticated()).toBe(true);
    expect(service.user()).toEqual({ id: 'u1', tenantId: 't1', roles: ['Admin'] });
  });

  it('clears an expired stored token', () => {
    const token = makeToken({
      sub: 'u1',
      tenantId: 't1',
      role: 'Admin',
      exp: Math.floor(Date.now() / 1000) - 10,
    });
    localStorage.setItem(ACCESS_TOKEN_KEY, token);

    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(ACCESS_TOKEN_KEY)).toBeNull();
  });

  it('login stores the token and updates the user signal', () => {
    const token = makeToken({
      sub: 'u2',
      tenantId: 't1',
      role: ['User', 'Instructor'],
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    const service = createService({ login: () => of({ accessToken: token }) });

    service.login({ email: 'a@b.com', password: 'pw' }).subscribe();

    expect(localStorage.getItem(ACCESS_TOKEN_KEY)).toBe(token);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.hasRole('Instructor')).toBe(true);
    expect(service.hasRole('Admin')).toBe(false);
  });

  it('logout clears the token and the user signal', () => {
    const token = makeToken({
      sub: 'u1',
      tenantId: 't1',
      role: 'Admin',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    localStorage.setItem(ACCESS_TOKEN_KEY, token);
    const service = createService();

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(ACCESS_TOKEN_KEY)).toBeNull();
  });
});
