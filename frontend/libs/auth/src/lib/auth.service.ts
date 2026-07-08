import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { AuthApiService } from '@frontend/data-access';
import { AuthUser, LoginRequest } from '@frontend/models';
import { decodeJwt, isTokenExpired, toAuthUser } from './jwt.util';

export const ACCESS_TOKEN_KEY = 'gym_access_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authApi = inject(AuthApiService);

  private readonly _user = signal<AuthUser | null>(this.readUserFromStorage());
  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);

  login(credentials: LoginRequest): Observable<AuthUser> {
    return this.authApi.login(credentials).pipe(
      tap(({ accessToken }) => localStorage.setItem(ACCESS_TOKEN_KEY, accessToken)),
      map(({ accessToken }) => {
        const user = toAuthUser(decodeJwt(accessToken) as NonNullable<ReturnType<typeof decodeJwt>>);
        this._user.set(user);
        return user;
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    this._user.set(null);
  }

  hasRole(role: string): boolean {
    return this._user()?.roles.includes(role) ?? false;
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  private readUserFromStorage(): AuthUser | null {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    if (!token) {
      return null;
    }
    const payload = decodeJwt(token);
    if (!payload || isTokenExpired(payload)) {
      localStorage.removeItem(ACCESS_TOKEN_KEY);
      return null;
    }
    return toAuthUser(payload);
  }
}
