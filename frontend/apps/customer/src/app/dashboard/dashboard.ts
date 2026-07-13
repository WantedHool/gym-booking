import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { BookingsStore, SubscriptionApiService } from '@frontend/data-access';
import { Subscription } from '@frontend/models';
import {
  BookingCard,
  CUSTOMER_DATE_FORMATS,
  EmptyState,
  ListSkeleton,
  PageHeader,
} from '@frontend/ui';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, MatIconModule, RouterLink, PageHeader, BookingCard, EmptyState, ListSkeleton],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  protected readonly dateFormats = CUSTOMER_DATE_FORMATS;
  protected readonly bookingsStore = inject(BookingsStore);
  private readonly subscriptionApi = inject(SubscriptionApiService);

  protected readonly upcoming = this.bookingsStore.upcoming;
  protected readonly subscription = signal<Subscription | null>(null);
  protected readonly subscriptionLoading = signal(false);

  ngOnInit(): void {
    this.bookingsStore.load();
    this.subscriptionLoading.set(true);
    this.subscriptionApi.getMine().subscribe({
      next: (s) => {
        this.subscription.set(s);
        this.subscriptionLoading.set(false);
      },
      error: () => this.subscriptionLoading.set(false),
    });
  }

  protected subscriptionProgress(sub: Subscription): number {
    if (!sub.sessionsTotal || sub.remainingSessions === null) {
      return 0;
    }
    return Math.max(0, Math.min(100, (sub.remainingSessions / sub.sessionsTotal) * 100));
  }
}
