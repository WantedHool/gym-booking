import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateInvitationRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class InvitationApiService {
  private readonly http = inject(HttpClient);

  send(request: CreateInvitationRequest): Observable<void> {
    return this.http.post<void>('/invitations', request);
  }
}
