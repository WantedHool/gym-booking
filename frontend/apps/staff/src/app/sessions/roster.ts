import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { RosterApiService, UserApiService } from '@frontend/data-access';
import { AppUser, Roster } from '@frontend/models';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-roster',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatSelectModule,
    PageHeader,
  ],
  templateUrl: './roster.html',
})
export class RosterView {
  private readonly route = inject(ActivatedRoute);
  private readonly rosterApi = inject(RosterApiService);
  private readonly userApi = inject(UserApiService);
  private readonly fb = inject(FormBuilder);

  private readonly sessionId = this.route.snapshot.paramMap.get('id')!;

  protected readonly roster = signal<Roster | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly customers = signal<AppUser[]>([]);

  protected readonly walkInForm = this.fb.nonNullable.group({
    userId: ['', Validators.required],
  });

  constructor() {
    this.load();
    this.userApi.getAll().subscribe({
      next: (u) => this.customers.set(u.filter((x) => x.roles.includes('User') && x.isActive)),
      error: () => void 0,
    });
  }

  private load(): void {
    this.rosterApi.get(this.sessionId).subscribe({
      next: (r) => this.roster.set(r),
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης roster.'),
    });
  }

  walkIn(): void {
    if (this.walkInForm.invalid) {
      this.walkInForm.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.rosterApi.walkIn(this.sessionId, this.walkInForm.getRawValue()).subscribe({
      next: () => {
        this.walkInForm.reset({ userId: '' });
        this.load();
      },
      error: (err) => this.errorMessage.set(err?.error ?? 'Η κράτηση απέτυχε.'),
    });
  }

  cancel(bookingId: string): void {
    this.errorMessage.set(null);
    this.rosterApi.cancel(bookingId).subscribe({
      next: () => this.load(),
      error: (err) => this.errorMessage.set(err?.error ?? 'Η ακύρωση απέτυχε.'),
    });
  }
}
