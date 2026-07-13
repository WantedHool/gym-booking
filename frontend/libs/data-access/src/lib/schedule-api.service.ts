import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ScheduleQuery, ScheduleSession } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class ScheduleApiService {
  private readonly http = inject(HttpClient);

  getSchedule(query: ScheduleQuery): Observable<ScheduleSession[]> {
    let params = new HttpParams().set('from', query.from).set('to', query.to);
    if (query.classTypeId) {
      params = params.set('classTypeId', query.classTypeId);
    }
    if (query.instructorId) {
      params = params.set('instructorId', query.instructorId);
    }
    return this.http.get<ScheduleSession[]>('/schedule', { params });
  }
}
