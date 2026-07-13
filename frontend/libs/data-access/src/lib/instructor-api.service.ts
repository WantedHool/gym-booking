import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Instructor } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class InstructorApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<Instructor[]> {
    return this.http.get<Instructor[]>('/instructors');
  }
}
