import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TenantApiService } from '@frontend/data-access';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, PageHeader],
  templateUrl: './settings.html',
})
export class Settings {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(TenantApiService);

  protected readonly saving = signal(false);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    cancellationHours: [0, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    this.api.get().subscribe({
      next: (s) => this.form.setValue({ name: s.name, cancellationHours: s.cancellationHours }),
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης ρυθμίσεων.'),
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.successMessage.set(null);
    this.errorMessage.set(null);
    this.saving.set(true);
    this.api.update(this.form.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set('Οι ρυθμίσεις αποθηκεύτηκαν.');
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err?.error ?? 'Η αποθήκευση απέτυχε.');
      },
    });
  }
}
