import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { Router } from '@angular/router';
import { ClassSessionApiService, ClassTypeApiService } from '@frontend/data-access';
import { ClassType } from '@frontend/models';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-session-new',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatTimepickerModule,
    MatIconModule,
    PageHeader,
  ],
  templateUrl: './session-new.html',
})
export class SessionNew {
  private readonly fb = inject(FormBuilder);
  private readonly sessionApi = inject(ClassSessionApiService);
  private readonly classTypeApi = inject(ClassTypeApiService);
  private readonly router = inject(Router);

  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    classTypeId: ['', Validators.required],
    startsDate: this.fb.control<Date | null>(null, Validators.required),
    startsTime: this.fb.control<Date | null>(null, Validators.required), // ώρα (τοπική) → συνδυάζεται με startsDate στο submit
    durationMinutes: [60, [Validators.required, Validators.min(1)]],
    capacity: [10, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    this.classTypeApi.getAll().subscribe({
      next: (items) => this.classTypes.set(items),
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης ειδών μαθημάτων.'),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    const startsAt = new Date(raw.startsDate!);
    startsAt.setHours(raw.startsTime!.getHours(), raw.startsTime!.getMinutes(), 0, 0);
    this.errorMessage.set(null);
    this.submitting.set(true);
    this.sessionApi
      .create({
        classTypeId: raw.classTypeId,
        // instructorId παραλείπεται → το backend βάζει τον τρέχοντα instructor.
        startsAt: startsAt.toISOString(),
        durationMinutes: raw.durationMinutes,
        capacity: raw.capacity,
      })
      .subscribe({
        next: () => {
          this.router.navigate(['/sessions']);
        },
        error: () => {
          this.submitting.set(false);
          this.errorMessage.set('Αποτυχία δημιουργίας session.');
        },
      });
  }
}
