import { DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BreakpointObserver } from '@angular/cdk/layout';
import { PageEvent } from '@angular/material/paginator';
import {
  MOBILE_BREAKPOINT,
  PaginationStateOptions,
  maxPageIndex,
  paginatedSlice,
  resolvePageSize,
  isResponsivePageSize,
  PAGE_SIZE_OPTIONS,
} from '../pagination/pagination';

/** Client-side pagination state for signal-based lists. Must run in an injection context. */
export function createPaginationState<T>(items: () => readonly T[], options?: PaginationStateOptions) {
  const breakpointObserver = inject(BreakpointObserver);
  const destroyRef = inject(DestroyRef);

  const pageSizeOptions = [...(options?.pageSizeOptions ?? PAGE_SIZE_OPTIONS)];
  const pageIndex = signal(0);
  const pageSize = signal(resolvePageSize(options?.pageSize, breakpointObserver.isMatched(MOBILE_BREAKPOINT)));
  const userOverrodePageSize = signal(false);

  const paginatedItems = computed(() => paginatedSlice(items(), pageIndex(), pageSize()));

  effect(() => {
    const clamped = maxPageIndex(items().length, pageSize());
    if (pageIndex() > clamped) {
      pageIndex.set(clamped);
    }
  });

  if (isResponsivePageSize(options?.pageSize)) {
    breakpointObserver
      .observe([MOBILE_BREAKPOINT])
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe((state) => {
        if (userOverrodePageSize()) {
          return;
        }
        pageSize.set(resolvePageSize(options.pageSize, state.matches));
      });
  }

  function onPage(event: PageEvent): void {
    if (event.pageSize !== pageSize()) {
      userOverrodePageSize.set(true);
    }
    pageIndex.set(event.pageIndex);
    pageSize.set(event.pageSize);
  }

  function resetPage(): void {
    pageIndex.set(0);
    userOverrodePageSize.set(false);
    pageSize.set(resolvePageSize(options?.pageSize, breakpointObserver.isMatched(MOBILE_BREAKPOINT)));
  }

  return { pageIndex, pageSize, pageSizeOptions, paginatedItems, onPage, resetPage };
}
