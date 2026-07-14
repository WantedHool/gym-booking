import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AuthService } from '@frontend/auth';
import { MembershipPlanApiService, SubscriptionApiService, UserApiService } from '@frontend/data-access';
import { AppUser, MembershipPlan } from '@frontend/models';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-user-edit',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatSlideToggleModule,
    PageHeader,
  ],
  templateUrl: './user-edit.html',
})
export class UserEdit {
  private readonly route = inject(ActivatedRoute);
  private readonly userApi = inject(UserApiService);
  private readonly planApi = inject(MembershipPlanApiService);
  private readonly subscriptionApi = inject(SubscriptionApiService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  private readonly userId = this.route.snapshot.paramMap.get('id')!;

  protected readonly isSelf = this.userId === this.authService.user()?.id;
  protected readonly user = signal<AppUser | null>(null);
  protected readonly plans = signal<MembershipPlan[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly assignError = signal<string | null>(null);
  protected readonly assignSuccess = signal<string | null>(null);
  protected readonly assignSubmitting = signal(false);
  protected readonly roles = ['User', 'Instructor', 'Admin'] as const;

  protected readonly planForm = this.fb.nonNullable.group({
    planId: ['', Validators.required],
  });

  constructor() {
    this.loadUser();
    this.planApi.getAll().subscribe({ next: (items) => this.plans.set(items), error: () => void 0 });
  }

  private loadUser(): void {
    this.userApi.getAll().subscribe({
      next: (items) => {
        const found = items.find((u) => u.id === this.userId);
        this.user.set(found ?? null);
        if (!found) {
          this.errorMessage.set('Ο χρήστης δεν βρέθηκε.');
        }
      },
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης χρήστη.'),
    });
  }

  changeRole(role: 'User' | 'Instructor' | 'Admin'): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.userApi.changeRole(this.userId, { role }).subscribe({
      next: () => {
        this.successMessage.set('Ο ρόλος ενημερώθηκε.');
        this.loadUser();
      },
      error: (err) => this.errorMessage.set(err?.error ?? 'Η αλλαγή ρόλου απέτυχε.'),
    });
  }

  toggleActive(): void {
    const current = this.user();
    if (!current) {
      return;
    }
    this.errorMessage.set(null);
    this.successMessage.set(null);
    const op = current.isActive ? this.userApi.deactivate(this.userId) : this.userApi.activate(this.userId);
    op.subscribe({
      next: () => {
        this.successMessage.set('Η κατάσταση ενημερώθηκε.');
        this.loadUser();
      },
      error: (err) => this.errorMessage.set(err?.error ?? 'Η ενέργεια απέτυχε.'),
    });
  }

  assignSubscription(): void {
    const current = this.user();
    if (!current || this.planForm.invalid) {
      this.planForm.markAllAsTouched();
      return;
    }
    this.assignError.set(null);
    this.assignSuccess.set(null);
    this.assignSubmitting.set(true);
    this.subscriptionApi.assign({ email: current.email, planId: this.planForm.getRawValue().planId }).subscribe({
      next: () => {
        this.assignSubmitting.set(false);
        this.assignSuccess.set('Η συνδρομή ανατέθηκε.');
        this.planForm.reset({ planId: '' });
      },
      error: (err) => {
        this.assignSubmitting.set(false);
        this.assignError.set(err?.error ?? 'Η ανάθεση απέτυχε.');
      },
    });
  }
}
