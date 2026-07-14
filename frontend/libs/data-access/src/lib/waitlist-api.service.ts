import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { WaitlistEntry } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class WaitlistApiService {
  private readonly http = inject(HttpClient);

  getMine(): Observable<WaitlistEntry[]> {
    return this.http.get<WaitlistEntry[]>('/waitlist/me');
  }

  join(sessionId: string): Observable<void> {
    return this.http.post<void>(`/sessions/${sessionId}/waitlist`, {});
  }

  leave(sessionId: string): Observable<void> {
    return this.http.delete<void>(`/sessions/${sessionId}/waitlist`);
  }
}
