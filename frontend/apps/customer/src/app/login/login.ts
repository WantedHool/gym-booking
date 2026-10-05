import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { AuthService } from '@frontend/auth';
import { AccentBadge, AuthBrand } from '@frontend/ui';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, AccentBadge, AuthBrand],
  templateUrl: './login.html',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly hidePassword = signal(true);

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);
    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);

        if (error.status === 429) {
          this.errorMessage.set('Πάρα πολλές προσπάθειες. Δοκιμάστε ξανά σε λίγο.');
          return;
        }

        if (error.status === 423) {
          this.errorMessage.set('Ο λογαριασμός κλειδώθηκε προσωρινά λόγω πολλών αποτυχημένων προσπαθειών. Δοκιμάστε ξανά σε λίγα λεπτά.');
          return;
        }

        this.errorMessage.set('Λάθος email ή κωδικός.');
        this.form.controls.email.setErrors({ invalidCredentials: true });
        this.form.controls.password.setErrors({ invalidCredentials: true });
        this.form.markAllAsTouched();
      },
    });
  }
}
