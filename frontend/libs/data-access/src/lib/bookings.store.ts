import { computed, inject, Injectable, signal } from '@angular/core';
import { Booking } from '@frontend/models';
import { BookingApiService } from './booking-api.service';

@Injectable({ providedIn: 'root' })
export class BookingsStore {
  private readonly api = inject(BookingApiService);

  readonly bookings = signal<Booking[]>([]);
  readonly loading = signal(false);

  readonly upcoming = computed(() =>
    this.bookings().filter((b) => b.status === 'Confirmed' && new Date(b.startsAt).getTime() >= Date.now()),
  );

  readonly history = computed(() =>
    this.bookings().filter((b) => b.status !== 'Confirmed' || new Date(b.startsAt).getTime() < Date.now()),
  );

  load(): void {
    this.loading.set(true);
    this.api.getMine().subscribe({
      next: (bookings) => {
        this.bookings.set(bookings);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
