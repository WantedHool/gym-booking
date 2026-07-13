import { Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'lib-empty-state',
  imports: [MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './empty-state.html',
})
export class EmptyState {
  readonly icon = input('info');
  readonly message = input.required<string>();
  readonly ctaLink = input<string>();
  readonly ctaLabel = input('Δες περισσότερα');
}
