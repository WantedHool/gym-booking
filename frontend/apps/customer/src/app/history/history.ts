import { Component, inject, OnInit } from '@angular/core';
import { BookingsStore } from '@frontend/data-access';
import {
  BookingCard,
  createPaginationState,
  CUSTOMER_DATE_FORMATS,
  EmptyState,
  ListPaginator,
  ListSkeleton,
  PageHeader,
} from '@frontend/ui';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [PageHeader, BookingCard, EmptyState, ListPaginator, ListSkeleton],
  templateUrl: './history.html',
})
export class History implements OnInit {
  protected readonly dateFormats = CUSTOMER_DATE_FORMATS;
  protected readonly bookingsStore = inject(BookingsStore);

  protected readonly history = this.bookingsStore.history;
  protected readonly pagination = createPaginationState(() => this.history(), {
    pageSize: { mobile: 7, desktop: 8 },
    pageSizeOptions: [3, 5, 7, 10],
  });

  ngOnInit(): void {
    this.bookingsStore.load();
  }
}
