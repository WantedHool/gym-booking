import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatSelectModule } from '@angular/material/select';
import { ClassSessionApiService, ClassTypeApiService } from '@frontend/data-access';
import { ClassSession, ClassType } from '@frontend/models';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatListModule,
    DatePipe,
  ],
  templateUrl: './sessions.html',
})
export class Sessions {
  private readonly fb = inject(FormBuilder);
  private readonly sessionApi = inject(ClassSessionApiService);
  private readonly classTypeApi = inject(ClassTypeApiService);

  protected readonly sessions = signal<ClassSession[]>([]);
  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    classTypeId: ['', Validators.required],
    startsAt: ['', Validators.required], // datetime-local (τοπική) → μετατροπή σε ISO UTC στο submit
    durationMinutes: [60, [Validators.required, Validators.min(1)]],
    capacity: [10, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    this.classTypeApi.getAll().subscribe({
      next: (items) => this.classTypes.set(items),
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης ειδών μαθημάτων.'),
    });
    this.load();
  }

  private load(): void {
    this.sessionApi.getAll().subscribe({
      next: (items) => this.sessions.set(items),
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης sessions.'),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    this.errorMessage.set(null);
    this.submitting.set(true);
    this.sessionApi
      .create({
        classTypeId: raw.classTypeId,
        // instructorId παραλείπεται → το backend βάζει τον τρέχοντα instructor.
        startsAt: new Date(raw.startsAt).toISOString(),
        durationMinutes: raw.durationMinutes,
        capacity: raw.capacity,
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.load();
        },
        error: () => {
          this.submitting.set(false);
          this.errorMessage.set('Αποτυχία δημιουργίας session.');
        },
      });
  }
}
