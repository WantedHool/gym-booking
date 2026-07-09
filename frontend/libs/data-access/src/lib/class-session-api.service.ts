import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ClassSession, CreateClassSessionRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class ClassSessionApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<ClassSession[]> {
    return this.http.get<ClassSession[]>('/class-sessions');
  }

  create(request: CreateClassSessionRequest): Observable<ClassSession> {
    return this.http.post<ClassSession>('/class-sessions', request);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`/class-sessions/${id}/cancel`, {});
  }
}
