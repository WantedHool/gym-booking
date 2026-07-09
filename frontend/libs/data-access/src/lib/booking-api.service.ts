import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Booking, CreateBookingRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class BookingApiService {
  private readonly http = inject(HttpClient);

  getMine(): Observable<Booking[]> {
    return this.http.get<Booking[]>('/bookings/me');
  }

  book(request: CreateBookingRequest): Observable<void> {
    return this.http.post<void>('/bookings', request);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`/bookings/${id}/cancel`, {});
  }
}
