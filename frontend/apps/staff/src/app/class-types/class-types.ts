import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { RouterLink } from '@angular/router';
import { ClassTypeApiService } from '@frontend/data-access';
import { ClassType } from '@frontend/models';

@Component({
  selector: 'app-class-types',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatListModule, RouterLink],
  templateUrl: './class-types.html',
})
export class ClassTypes {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ClassTypeApiService);

  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    defaultDurationMinutes: [60, [Validators.required, Validators.min(1)]],
    defaultCapacity: [10, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    this.load();
  }

  private load(): void {
    this.api.getAll().subscribe({
      next: (items) => this.classTypes.set(items),
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης ειδών μαθημάτων.'),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.submitting.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.form.reset({ defaultDurationMinutes: 60, defaultCapacity: 10 });
        this.load();
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Αποτυχία δημιουργίας είδους μαθήματος.');
      },
    });
  }
}
