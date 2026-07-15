import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateInvitationRequest, InvitationResponse } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class InvitationApiService {
  private readonly http = inject(HttpClient);

  send(request: CreateInvitationRequest): Observable<InvitationResponse> {
    return this.http.post<InvitationResponse>('/invitations', request);
  }
}
