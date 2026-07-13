import { Component, input } from '@angular/core';

@Component({
  selector: 'lib-list-skeleton',
  template: `
    <div class="flex flex-col gap-2">
      @for (_ of placeholders(); track $index) {
        <div class="app-skeleton-card" aria-hidden="true">
          <div class="app-skeleton-line app-skeleton-line--title"></div>
          <div class="app-skeleton-line app-skeleton-line--subtitle"></div>
        </div>
      }
    </div>
  `,
})
export class ListSkeleton {
  readonly count = input(3);

  protected placeholders(): number[] {
    return Array.from({ length: this.count() }, (_, i) => i);
  }
}
