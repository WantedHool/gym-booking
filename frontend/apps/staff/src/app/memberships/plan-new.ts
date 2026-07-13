import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';
import { MembershipPlanApiService } from '@frontend/data-access';
import { PlanType } from '@frontend/models';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-plan-new',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    PageHeader,
  ],
  templateUrl: './plan-new.html',
})
export class PlanNew {
  private readonly fb = inject(FormBuilder);
  private readonly planApi = inject(MembershipPlanApiService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly planForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    type: this.fb.nonNullable.control<PlanType>('SessionPack', Validators.required),
    sessionsCount: [10, [Validators.required, Validators.min(0)]],
    durationDays: [30, [Validators.required, Validators.min(1)]],
    price: [50, [Validators.required, Validators.min(0)]],
  });

  createPlan(): void {
    if (this.planForm.invalid) {
      this.planForm.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.submitting.set(true);
    this.planApi.create(this.planForm.getRawValue()).subscribe({
      next: () => {
        this.router.navigate(['/memberships']);
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Αποτυχία δημιουργίας πακέτου.');
      },
    });
  }
}
