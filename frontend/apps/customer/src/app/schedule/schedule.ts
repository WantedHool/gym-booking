import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import {
  BookingApiService,
  ClassTypeApiService,
  InstructorApiService,
  ScheduleApiService,
} from '@frontend/data-access';
import { ClassType, Instructor, ScheduleSession } from '@frontend/models';

@Component({
  selector: 'app-schedule',
  standalone: true,
  imports: [DatePipe, FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatSelectModule],
  templateUrl: './schedule.html',
})
export class Schedule {
  private readonly scheduleApi = inject(ScheduleApiService);
  private readonly bookingApi = inject(BookingApiService);
  private readonly classTypeApi = inject(ClassTypeApiService);
  private readonly instructorApi = inject(InstructorApiService);

  protected readonly weekStart = signal(this.mondayOf(new Date()));
  protected readonly sessions = signal<ScheduleSession[]>([]);
  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly instructors = signal<Instructor[]>([]);
  protected readonly classTypeId = signal<string>('');
  protected readonly instructorId = signal<string>('');
  protected readonly message = signal<string | null>(null);

  protected readonly weekEnd = computed(() => {
    const d = new Date(this.weekStart());
    d.setDate(d.getDate() + 6);
    return d;
  });

  constructor() {
    this.classTypeApi.getAll().subscribe((x) => this.classTypes.set(x));
    this.instructorApi.getAll().subscribe((x) => this.instructors.set(x));
    this.load();
  }

  private mondayOf(date: Date): Date {
    const d = new Date(date);
    d.setHours(0, 0, 0, 0);
    const dayFromMonday = (d.getDay() + 6) % 7; // Κυρ=6 ... Δευ=0
    d.setDate(d.getDate() - dayFromMonday);
    return d;
  }

  protected load(): void {
    const from = this.weekStart();
    const to = new Date(from);
    to.setDate(to.getDate() + 7);
    this.scheduleApi
      .getSchedule({
        from: from.toISOString(),
        to: to.toISOString(),
        classTypeId: this.classTypeId() || undefined,
        instructorId: this.instructorId() || undefined,
      })
      .subscribe((x) => this.sessions.set(x));
  }

  changeWeek(deltaDays: number): void {
    const d = new Date(this.weekStart());
    d.setDate(d.getDate() + deltaDays);
    this.weekStart.set(d);
    this.load();
  }

  book(session: ScheduleSession): void {
    this.message.set(null);
    this.bookingApi.book({ classSessionId: session.id }).subscribe({
      next: () => this.load(),
      error: (err) => this.message.set(err?.error ?? 'Η κράτηση απέτυχε.'),
    });
  }

  cancel(session: ScheduleSession): void {
    if (!session.myBookingId) {
      return;
    }
    this.bookingApi.cancel(session.myBookingId).subscribe({ next: () => this.load() });
  }
}
