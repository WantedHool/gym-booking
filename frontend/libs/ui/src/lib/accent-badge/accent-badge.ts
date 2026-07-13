import { Component, input } from '@angular/core';

@Component({
  selector: 'lib-accent-badge',
  template: `
    <span
      class="app-badge-accent inline-block rounded-full font-semibold"
      [class]="size() === 'md' ? 'px-3 py-1 text-xs' : 'px-2 py-0.5 text-[11px]'"
    >
      <ng-content />
    </span>
  `,
})
export class AccentBadge {
  readonly size = input<'sm' | 'md'>('sm');
}
