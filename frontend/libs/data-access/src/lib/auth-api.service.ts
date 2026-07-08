import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { LoginRequest, LoginResponse, RegisterRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/auth/login', credentials);
  }

  register(token: string, request: RegisterRequest): Observable<void> {
    return this.http.post<void>(
      `/auth/register?token=${encodeURIComponent(token)}`,
      request,
    );
  }
}
