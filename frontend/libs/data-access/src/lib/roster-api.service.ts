import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Roster, StaffBookRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class RosterApiService {
  private readonly http = inject(HttpClient);

  get(sessionId: string): Observable<Roster> {
    return this.http.get<Roster>(`/sessions/${sessionId}/roster`);
  }

  walkIn(sessionId: string, request: StaffBookRequest): Observable<void> {
    return this.http.post<void>(`/sessions/${sessionId}/bookings`, request);
  }

  cancel(bookingId: string): Observable<void> {
    return this.http.delete<void>(`/bookings/${bookingId}/staff`);
  }
}
