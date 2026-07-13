import { Component, input } from '@angular/core';

const STATUS_LABELS: Record<string, string> = {
  Confirmed: 'Επιβεβαιωμένη',
  Cancelled: 'Ακυρωμένη',
};

@Component({
  selector: 'lib-status-badge',
  template: `
    <span
      class="inline-block px-2 py-0.5 rounded-full text-[11px] font-semibold"
      [class]="badgeClass()"
    >
      {{ label() }}
    </span>
  `,
})
export class StatusBadge {
  readonly status = input.required<string>();

  protected label(): string {
    return STATUS_LABELS[this.status()] ?? this.status();
  }

  protected badgeClass(): string {
    switch (this.status()) {
      case 'Confirmed':
        return 'app-status-badge--confirmed';
      case 'Cancelled':
        return 'app-status-badge--cancelled';
      default:
        return 'app-status-badge--default';
    }
  }
}
