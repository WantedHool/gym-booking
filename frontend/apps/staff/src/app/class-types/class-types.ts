import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { ClassTypeApiService } from '@frontend/data-access';
import { ClassType } from '@frontend/models';
import { createPaginationState, ListPaginator, PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-class-types',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, RouterLink, PageHeader, ListPaginator],
  templateUrl: './class-types.html',
})
export class ClassTypes {
  private readonly api = inject(ClassTypeApiService);

  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pagination = createPaginationState(() => this.classTypes());

  constructor() {
    this.load();
  }

  private load(): void {
    this.api.getAll().subscribe({
      next: (items) => {
        this.classTypes.set(items);
        this.pagination.resetPage();
      },
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης ειδών μαθημάτων.'),
    });
  }
}
