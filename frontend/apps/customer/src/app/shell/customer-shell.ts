import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@frontend/auth';
import { AccentBadge } from '@frontend/ui';

export interface CustomerNavTab {
  route: string;
  icon: string;
  label: string;
  exact?: boolean;
}

@Component({
  selector: 'app-customer-shell',
  standalone: true,
  imports: [AccentBadge, MatButtonModule, MatIconModule, MatMenuModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './customer-shell.html',
})
export class CustomerShell {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly tabs: CustomerNavTab[] = [
    { route: '/dashboard', icon: 'home', label: 'Αρχική', exact: true },
    { route: '/schedule', icon: 'event', label: 'Πρόγραμμα' },
    { route: '/history', icon: 'history', label: 'Ιστορικό' },
  ];

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
