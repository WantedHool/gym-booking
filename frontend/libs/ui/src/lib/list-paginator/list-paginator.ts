import { Component, input, output } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { DEFAULT_PAGE_SIZE, PAGE_SIZE_OPTIONS } from '../pagination/pagination';

@Component({
  selector: 'lib-list-paginator',
  imports: [MatPaginatorModule],
  template: `
    @if (length() > pageSize()) {
      <mat-paginator
        class="app-paginator"
        [length]="length()"
        [pageSize]="pageSize()"
        [pageIndex]="pageIndex()"
        [pageSizeOptions]="pageSizeOptions()"
        [showFirstLastButtons]="showFirstLastButtons()"
        [hidePageSize]="hidePageSize()"
        (page)="pageChange.emit($event)"
        aria-label="Σελίδες λίστας"
      />
    }
  `,
})
export class ListPaginator {
  readonly length = input.required<number>();
  readonly pageIndex = input(0);
  readonly pageSize = input(DEFAULT_PAGE_SIZE);
  readonly pageSizeOptions = input<number[]>([...PAGE_SIZE_OPTIONS]);
  readonly showFirstLastButtons = input(true);
  readonly hidePageSize = input(false);

  readonly pageChange = output<PageEvent>();
}
