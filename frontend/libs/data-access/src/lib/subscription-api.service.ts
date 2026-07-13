import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AssignSubscriptionRequest, Subscription } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class SubscriptionApiService {
  private readonly http = inject(HttpClient);

  getMine(): Observable<Subscription | null> {
    return this.http.get<Subscription | null>('/subscriptions/me');
  }

  assign(request: AssignSubscriptionRequest): Observable<void> {
    return this.http.post<void>('/subscriptions', request);
  }
}
