import { Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'lib-page-header',
  imports: [MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './page-header.html',
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly backLink = input<string>();
  readonly backLabel = input('Πίσω');
  readonly parentLink = input<string>();
  readonly parentLabel = input<string>();
}
