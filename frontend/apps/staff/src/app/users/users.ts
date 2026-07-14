import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { UserApiService } from '@frontend/data-access';
import { AppUser } from '@frontend/models';
import { createPaginationState, ListPaginator, PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, RouterLink, PageHeader, ListPaginator],
  templateUrl: './users.html',
})
export class Users {
  private readonly api = inject(UserApiService);

  protected readonly users = signal<AppUser[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pagination = createPaginationState(() => this.users());

  constructor() {
    this.load();
  }

  private load(): void {
    this.api.getAll().subscribe({
      next: (items) => {
        this.users.set(items);
        this.pagination.resetPage();
      },
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης χρηστών.'),
    });
  }
}
