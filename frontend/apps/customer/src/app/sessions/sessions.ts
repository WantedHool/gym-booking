import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { BookingApiService, ClassSessionApiService } from '@frontend/data-access';
import { Booking, ClassSession } from '@frontend/models';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [DatePipe, MatButtonModule],
  templateUrl: './sessions.html',
})
export class Sessions {
  private readonly sessionApi = inject(ClassSessionApiService);
  private readonly bookingApi = inject(BookingApiService);

  protected readonly sessions = signal<ClassSession[]>([]);
  protected readonly myBookings = signal<Booking[]>([]);
  protected readonly message = signal<string | null>(null);

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.sessionApi.getAll().subscribe((items) => this.sessions.set(items));
    this.bookingApi.getMine().subscribe((items) => this.myBookings.set(items));
  }

  book(session: ClassSession): void {
    this.message.set(null);
    this.bookingApi.book({ classSessionId: session.id }).subscribe({
      next: () => this.reload(),
      error: (err) => this.message.set(err?.error ?? 'Η κράτηση απέτυχε.'),
    });
  }

  cancel(booking: Booking): void {
    this.bookingApi.cancel(booking.id).subscribe({ next: () => this.reload() });
  }
}
