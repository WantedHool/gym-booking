import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'lib-error-banner',
  imports: [MatIconModule],
  template: `
    <div class="app-error-banner" role="alert">
      <mat-icon class="shrink-0 !w-5 !h-5 !text-[20px]">error_outline</mat-icon>
      <span>{{ message() }}</span>
    </div>
  `,
})
export class ErrorBanner {
  readonly message = input.required<string>();
}
