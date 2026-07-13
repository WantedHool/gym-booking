import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { MembershipPlanApiService, SubscriptionApiService } from '@frontend/data-access';
import { MembershipPlan } from '@frontend/models';
import { createPaginationState, ListPaginator, PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-memberships',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    RouterLink,
    PageHeader,
    ListPaginator,
  ],
  templateUrl: './memberships.html',
})
export class Memberships {
  private readonly fb = inject(FormBuilder);
  private readonly planApi = inject(MembershipPlanApiService);
  private readonly subApi = inject(SubscriptionApiService);

  protected readonly plans = signal<MembershipPlan[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly assignSubmitting = signal(false);
  protected readonly assignError = signal<string | null>(null);
  protected readonly assignSuccess = signal<string | null>(null);
  protected readonly pagination = createPaginationState(() => this.plans());

  protected readonly assignForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    planId: ['', Validators.required],
  });

  constructor() {
    this.load();
  }

  private load(): void {
    this.planApi.getAll().subscribe({
      next: (items) => {
        this.plans.set(items);
        this.pagination.resetPage();
      },
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης πακέτων.'),
    });
  }

  assign(): void {
    if (this.assignForm.invalid) {
      this.assignForm.markAllAsTouched();
      return;
    }
    this.assignError.set(null);
    this.assignSuccess.set(null);
    this.assignSubmitting.set(true);
    this.subApi.assign(this.assignForm.getRawValue()).subscribe({
      next: () => {
        this.assignSubmitting.set(false);
        this.assignSuccess.set('Η συνδρομή ανατέθηκε.');
        this.assignForm.reset({ email: '', planId: '' });
      },
      error: (err) => {
        this.assignSubmitting.set(false);
        this.assignError.set(err?.error ?? 'Η ανάθεση απέτυχε.');
      },
    });
  }
}
