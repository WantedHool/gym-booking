import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreatePlanRequest, MembershipPlan } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class MembershipPlanApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<MembershipPlan[]> {
    return this.http.get<MembershipPlan[]>('/membership-plans');
  }

  create(request: CreatePlanRequest): Observable<MembershipPlan> {
    return this.http.post<MembershipPlan>('/membership-plans', request);
  }
}
