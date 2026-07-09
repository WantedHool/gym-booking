import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ClassType, CreateClassTypeRequest, UpdateClassTypeRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class ClassTypeApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<ClassType[]> {
    return this.http.get<ClassType[]>('/class-types');
  }

  create(request: CreateClassTypeRequest): Observable<ClassType> {
    return this.http.post<ClassType>('/class-types', request);
  }

  update(id: string, request: UpdateClassTypeRequest): Observable<void> {
    return this.http.put<void>(`/class-types/${id}`, request);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`/class-types/${id}`);
  }
}
