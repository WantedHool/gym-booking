import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { ClassTypeApiService } from '@frontend/data-access';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-class-type-new',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, PageHeader],
  templateUrl: './class-type-new.html',
})
export class ClassTypeNew {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ClassTypeApiService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    defaultDurationMinutes: [60, [Validators.required, Validators.min(1)]],
    defaultCapacity: [10, [Validators.required, Validators.min(1)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.submitting.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: () => {
        this.router.navigate(['/class-types']);
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Αποτυχία δημιουργίας είδους μαθήματος.');
      },
    });
  }
}
