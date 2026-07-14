import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AppUser, ChangeRoleRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class UserApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>('/users');
  }

  changeRole(id: string, request: ChangeRoleRequest): Observable<void> {
    return this.http.put<void>(`/users/${id}/role`, request);
  }

  deactivate(id: string): Observable<void> {
    return this.http.put<void>(`/users/${id}/deactivate`, {});
  }

  activate(id: string): Observable<void> {
    return this.http.put<void>(`/users/${id}/activate`, {});
  }
}
