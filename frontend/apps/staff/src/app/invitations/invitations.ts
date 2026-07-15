import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { InvitationApiService } from '@frontend/data-access';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-invitations',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, PageHeader],
  templateUrl: './invitations.html',
})
export class Invitations {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(InvitationApiService);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly registerLink = signal<string | null>(null);
  protected readonly copied = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: this.fb.nonNullable.control<'User' | 'Instructor'>('User', Validators.required),
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.registerLink.set(null);
    this.copied.set(false);
    this.submitting.set(true);
    const { email, role } = this.form.getRawValue();
    this.api.send({ email, role }).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.successMessage.set(`Η πρόσκληση για ${email} δημιουργήθηκε. Στείλε το παρακάτω link:`);
        this.registerLink.set(response.registerLink);
        this.form.reset({ email: '', role: 'User' });
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Αποτυχία δημιουργίας πρόσκλησης.');
      },
    });
  }

  async copyLink(): Promise<void> {
    const link = this.registerLink();
    if (!link) {
      return;
    }
    await navigator.clipboard.writeText(link);
    this.copied.set(true);
  }
}
