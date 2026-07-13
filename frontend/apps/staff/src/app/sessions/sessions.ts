import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { ClassSessionApiService } from '@frontend/data-access';
import { ClassSession } from '@frontend/models';
import { createPaginationState, ListPaginator, PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, RouterLink, DatePipe, PageHeader, ListPaginator],
  templateUrl: './sessions.html',
})
export class Sessions {
  private readonly sessionApi = inject(ClassSessionApiService);

  protected readonly sessions = signal<ClassSession[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pagination = createPaginationState(() => this.sessions());

  constructor() {
    this.load();
  }

  private load(): void {
    this.sessionApi.getAll().subscribe({
      next: (items) => {
        this.sessions.set(items);
        this.pagination.resetPage();
      },
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης sessions.'),
    });
  }
}
