import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { AuthService } from '@frontend/auth';
import { BookingApiService } from '@frontend/data-access';
import { Booking } from '@frontend/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, MatIconModule, RouterLink],
  templateUrl: './dashboard.html',
})
export class Dashboard {
  protected readonly authService = inject(AuthService);
  private readonly bookingApi = inject(BookingApiService);

  private readonly bookings = signal<Booking[]>([]);

  protected readonly upcoming = computed(() =>
    this.bookings().filter((b) => b.status === 'Confirmed' && new Date(b.startsAt).getTime() >= Date.now()),
  );
  protected readonly history = computed(() =>
    this.bookings().filter((b) => b.status !== 'Confirmed' || new Date(b.startsAt).getTime() < Date.now()),
  );

  constructor() {
    this.bookingApi.getMine().subscribe((x) => this.bookings.set(x));
  }
}
