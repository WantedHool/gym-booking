import { Component, input } from '@angular/core';

@Component({
  selector: 'lib-auth-brand',
  template: `
    <div class="flex flex-col items-center gap-3 mb-2">
      <div
        class="app-auth-brand-badge flex items-center justify-center rounded-full text-4xl"
        style="width: 76px; height: 76px; background: color-mix(in srgb, var(--app-accent) 18%, transparent);"
      >
        {{ emoji() }}
      </div>
      <span class="app-auth-brand-word text-2xl font-semibold text-white">GymBooking</span>
    </div>
  `,
})
export class AuthBrand {
  readonly emoji = input('🏋');
}
