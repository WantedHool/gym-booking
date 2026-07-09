# Frontend Visual Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Εφαρμογή του Bold Dark θέματος (neon lime accent) και ενός κοινού shell πλοήγησης (bottom tab bar στο customer, sidebar στο staff) σε όλες τις υπάρχουσες σελίδες των δύο Angular apps, χωρίς καμία λειτουργική αλλαγή.

**Architecture:** Ένα νέο `CustomerShell` / `StaffShell` standalone component ανά app, wrapped γύρω από τα authenticated routes μέσω parent route στο `app.routes.ts`. Χρωματικά tokens ορίζονται μία φορά ως CSS custom properties στο `styles.scss` κάθε app και καταναλώνονται στα templates μέσω Tailwind arbitrary values (`bg-[var(--app-bg-surface)]`). Καμία νέα βιβλιοθήκη στο `libs/ui` — ελαφρύ πέρασμα (βλ. design doc §4).

**Tech Stack:** Angular 21 standalone components, Angular Material 3 (`mat.theme()`), Angular CDK `BreakpointObserver`, Tailwind v4.

**Σχετικό spec:** [docs/2026-07-09-frontend-visual-redesign-design.md](../2026-07-09-frontend-visual-redesign-design.md)

**Προσαρμογή στο TDD:** Αυτό είναι καθαρά visual/template refactor. Τα app components (`Dashboard`, `Sessions`, κ.λπ.) δεν έχουν σήμερα κανένα `.spec.ts` — μόνο τα services/guards στα `libs/*` έχουν unit tests. Δεν εφευρίσκουμε νέο test convention για ένα καθαρά αισθητικό πέρασμα. Αντί για failing-test-first, κάθε task επαληθεύεται με `nx lint` / `nx build` (πιάνει σπασμένα templates/imports) και, όπου έχει νόημα, οπτικό έλεγχο μέσω preview tool. Καμία `git commit` δεν εκτελείται αυτόματα — κάθε task τελειώνει σε ένα **Checkpoint** για review από τον χρήστη, ο οποίος κάνει commit όταν είναι έτοιμος (καθιερωμένο workflow του project — βλ. `CLAUDE.md`).

---

### Task 1: Dark theme tokens (και τα δύο apps)

**Files:**
- Modify: `frontend/apps/customer/src/styles.scss`
- Modify: `frontend/apps/staff/src/styles.scss`

- [ ] **Step 1: Αντικατάστησε το `frontend/apps/customer/src/styles.scss`**

```scss
@use '@angular/material' as mat;

html {
  height: 100%;
  @include mat.theme((
    color: (
      theme-type: dark,
      primary: mat.$chartreuse-palette,
      tertiary: mat.$chartreuse-palette,
    ),
    typography: Roboto,
    density: 0,
  ));
}

:root {
  --app-bg-page: #15161a;
  --app-bg-surface: #1e2026;
  --app-border: #33353c;
  --app-text-secondary: #8a8d95;
  --app-accent: #c6ff3d;
  --mat-sys-primary: var(--app-accent);
}

body {
  color-scheme: dark;
  background-color: var(--app-bg-page);
  color: var(--mat-sys-on-surface);
  font: var(--mat-sys-body-medium);
  margin: 0;
  height: 100%;
}
```

- [ ] **Step 2: Αντικατάστησε το `frontend/apps/staff/src/styles.scss` με το ίδιο ακριβώς περιεχόμενο του Step 1**

- [ ] **Step 3: Verify — build και τα δύο apps**

Run: `cd frontend && npx nx run-many -t build -p customer,staff`
Expected: και τα δύο builds περνάνε χωρίς Sass/TypeScript errors (π.χ. άγνωστο `mat.$chartreuse-palette` θα σκάσει εδώ αν το όνομα είναι λάθος).

- [ ] **Checkpoint:** review του dark theme (χρώματα σε `:root`) πριν προχωρήσουμε στο shell.

---

### Task 2: Customer Shell (topbar + bottom tab bar) + routing

**Files:**
- Create: `frontend/apps/customer/src/app/shell/customer-shell.ts`
- Create: `frontend/apps/customer/src/app/shell/customer-shell.html`
- Modify: `frontend/apps/customer/src/app/app.routes.ts`

- [ ] **Step 1: Δημιούργησε το `frontend/apps/customer/src/app/shell/customer-shell.ts`**

```ts
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@frontend/auth';

@Component({
  selector: 'app-customer-shell',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatMenuModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './customer-shell.html',
})
export class CustomerShell {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
```

- [ ] **Step 2: Δημιούργησε το `frontend/apps/customer/src/app/shell/customer-shell.html`**

```html
<div class="min-h-screen flex flex-col bg-[var(--app-bg-page)]">
  <header class="flex items-center justify-between px-4 py-3 bg-[var(--app-bg-surface)] border-b border-[var(--app-border)]">
    <span class="font-semibold text-white">🏋 GymBooking</span>
    <button mat-icon-button [matMenuTriggerFor]="userMenu" aria-label="Μενού χρήστη">
      <mat-icon>account_circle</mat-icon>
    </button>
    <mat-menu #userMenu="matMenu">
      <button mat-menu-item (click)="logout()">
        <mat-icon>logout</mat-icon>
        <span>Αποσύνδεση</span>
      </button>
    </mat-menu>
  </header>

  <main class="flex-1 w-full max-w-2xl mx-auto p-4 pb-24">
    <router-outlet></router-outlet>
  </main>

  <nav
    class="fixed inset-x-0 bottom-0 flex bg-[var(--app-bg-surface)] border-t border-[var(--app-border)]"
    style="padding-bottom: env(safe-area-inset-bottom);"
  >
    <a
      routerLink="/dashboard"
      routerLinkActive="text-[var(--app-accent)]"
      [routerLinkActiveOptions]="{ exact: true }"
      class="flex-1 flex flex-col items-center gap-0.5 py-2 text-xs text-[var(--app-text-secondary)]"
    >
      <mat-icon>home</mat-icon>
      Αρχική
    </a>
    <a
      routerLink="/sessions"
      routerLinkActive="text-[var(--app-accent)]"
      class="flex-1 flex flex-col items-center gap-0.5 py-2 text-xs text-[var(--app-text-secondary)]"
    >
      <mat-icon>event</mat-icon>
      Μαθήματα
    </a>
  </nav>
</div>
```

**Σημείωση scope:** το bottom nav έχει σήμερα 2 στοιχεία (Αρχική, Μαθήματα) — όσα routes υπάρχουν πραγματικά σήμερα. Τα "Κρατήσεις μου"/"Προφίλ" από το αρχικό mockup μπαίνουν όταν υπάρξουν πραγματικές σελίδες γι' αυτά (Φ4+).

- [ ] **Step 3: Αντικατάστησε το `frontend/apps/customer/src/app/app.routes.ts`**

```ts
import { Route } from '@angular/router';
import { authGuard } from '@frontend/auth';
import { CustomerShell } from './shell/customer-shell';
import { Dashboard } from './dashboard/dashboard';
import { Login } from './login/login';
import { Register } from './register/register';
import { Sessions } from './sessions/sessions';

export const appRoutes: Route[] = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  {
    path: '',
    component: CustomerShell,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'sessions', component: Sessions },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
```

- [ ] **Step 4: Verify — lint + build**

Run: `cd frontend && npx nx run-many -t lint build -p customer`
Expected: PASS, χωρίς template/import errors.

- [ ] **Step 5: Verify — test**

Run: `cd frontend && npx nx test customer`
Expected: PASS (το `app.spec.ts` δεν εξαρτάται από routes, δεν επηρεάζεται).

- [ ] **Checkpoint:** review του CustomerShell πριν προχωρήσουμε στις σελίδες.

---

### Task 3: Restyle Customer Dashboard

**Files:**
- Modify: `frontend/apps/customer/src/app/dashboard/dashboard.ts`
- Modify: `frontend/apps/customer/src/app/dashboard/dashboard.html`

- [ ] **Step 1: Αντικατάστησε το `dashboard.ts`**

```ts
import { Component, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { AuthService } from '@frontend/auth';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MatIconModule, RouterLink],
  templateUrl: './dashboard.html',
})
export class Dashboard {
  protected readonly authService = inject(AuthService);
}
```

Το logout πλέον γίνεται από το `CustomerShell` (user menu) — αφαιρέθηκε από εδώ.

- [ ] **Step 2: Αντικατάστησε το `dashboard.html`**

```html
<div class="flex flex-col gap-4">
  <h1 class="text-2xl font-bold text-white">Καλωσήρθες!</h1>
  <p class="text-sm text-[var(--app-text-secondary)]">
    Ρόλοι: {{ authService.user()?.roles?.join(', ') }}
  </p>

  <a
    routerLink="/sessions"
    class="flex items-center gap-3 bg-[var(--app-bg-surface)] border border-[var(--app-border)] rounded-lg p-4 hover:border-[var(--app-accent)] transition-colors"
  >
    <mat-icon class="text-[var(--app-accent)]">event</mat-icon>
    <span class="text-white">Μαθήματα</span>
  </a>
</div>
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npx nx run-many -t lint build test -p customer`
Expected: PASS.

- [ ] **Checkpoint:** review.

---

### Task 4: Restyle Customer Sessions (κάρτες αντί για mat-list)

**Files:**
- Modify: `frontend/apps/customer/src/app/sessions/sessions.ts`
- Modify: `frontend/apps/customer/src/app/sessions/sessions.html`

- [ ] **Step 1: Αντικατάστησε το `sessions.ts`**

```ts
import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { BookingApiService, ClassSessionApiService } from '@frontend/data-access';
import { Booking, ClassSession } from '@frontend/models';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [DatePipe, MatButtonModule],
  templateUrl: './sessions.html',
})
export class Sessions {
  private readonly sessionApi = inject(ClassSessionApiService);
  private readonly bookingApi = inject(BookingApiService);

  protected readonly sessions = signal<ClassSession[]>([]);
  protected readonly myBookings = signal<Booking[]>([]);
  protected readonly message = signal<string | null>(null);

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.sessionApi.getAll().subscribe((items) => this.sessions.set(items));
    this.bookingApi.getMine().subscribe((items) => this.myBookings.set(items));
  }

  book(session: ClassSession): void {
    this.message.set(null);
    this.bookingApi.book({ classSessionId: session.id }).subscribe({
      next: () => this.reload(),
      error: (err) => this.message.set(err?.error ?? 'Η κράτηση απέτυχε.'),
    });
  }

  cancel(booking: Booking): void {
    this.bookingApi.cancel(booking.id).subscribe({ next: () => this.reload() });
  }
}
```

(Ίδια λογική με πριν — αφαιρέθηκαν μόνο τα `MatListModule`/`RouterLink` που δεν χρειάζονται πια.)

- [ ] **Step 2: Αντικατάστησε το `sessions.html`**

```html
<h1 class="text-2xl font-bold text-white mb-4">Διαθέσιμα μαθήματα</h1>

@if (message(); as m) {
  <p class="text-red-400 mb-4">{{ m }}</p>
}

<div class="flex flex-col gap-3 mb-8">
  @for (s of sessions(); track s.id) {
    <div class="flex items-center justify-between gap-3 bg-[var(--app-bg-surface)] border border-[var(--app-border)] rounded-lg p-4">
      <div class="text-sm text-white">
        <div class="font-medium">{{ s.classTypeName }}</div>
        <div class="text-[var(--app-text-secondary)]">
          {{ s.startsAt | date: 'dd/MM HH:mm' }} — {{ s.bookedCount }}/{{ s.capacity }}
        </div>
      </div>
      <button
        mat-flat-button
        color="primary"
        [disabled]="s.bookedCount >= s.capacity || s.isCancelled"
        (click)="book(s)"
      >
        Κράτηση
      </button>
    </div>
  } @empty {
    <p class="text-[var(--app-text-secondary)]">Δεν υπάρχουν διαθέσιμα μαθήματα.</p>
  }
</div>

<h2 class="text-xl font-bold text-white mb-4">Οι κρατήσεις μου</h2>
<div class="flex flex-col gap-3">
  @for (b of myBookings(); track b.id) {
    <div class="flex items-center justify-between gap-3 bg-[var(--app-bg-surface)] border border-[var(--app-border)] rounded-lg p-4">
      <div class="text-sm text-white">
        <div class="font-medium">{{ b.classTypeName }}</div>
        <div class="text-[var(--app-text-secondary)]">
          {{ b.startsAt | date: 'dd/MM HH:mm' }} — {{ b.status }}
        </div>
      </div>
      @if (b.status === 'Confirmed') {
        <button mat-button color="warn" (click)="cancel(b)">Ακύρωση</button>
      }
    </div>
  } @empty {
    <p class="text-[var(--app-text-secondary)]">Δεν έχεις κρατήσεις.</p>
  }
</div>
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npx nx run-many -t lint build test -p customer`
Expected: PASS.

- [ ] **Checkpoint:** review.

---

### Task 5: Restyle Customer Login + Register

**Files:**
- Modify: `frontend/apps/customer/src/app/login/login.html`
- Modify: `frontend/apps/customer/src/app/register/register.html`

(Τα `.ts` αρχεία δεν αλλάζουν — μόνο τα templates.)

- [ ] **Step 1: Αντικατάστησε το `login.html`**

```html
<div class="min-h-screen flex">
  <div class="hidden lg:flex flex-1 items-center justify-center bg-[var(--mat-sys-primary)] text-black p-12">
    <div class="max-w-md text-center">
      <h1 class="text-3xl font-semibold mb-2">Demo Gym</h1>
      <p class="text-black/70">Οι κρατήσεις σου, ένα κλικ μακριά.</p>
    </div>
  </div>

  <div class="flex flex-1 items-center justify-center p-4 lg:p-12 bg-[var(--app-bg-page)]">
    <form [formGroup]="form" (ngSubmit)="submit()" class="w-full max-w-sm flex flex-col gap-4">
      <h2 class="text-xl font-medium text-center text-white">Σύνδεση</h2>

      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Email</mat-label>
        <input matInput type="email" formControlName="email" autocomplete="email" />
      </mat-form-field>

      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Κωδικός</mat-label>
        <input matInput type="password" formControlName="password" autocomplete="current-password" />
      </mat-form-field>

      @if (errorMessage(); as message) {
        <p class="text-sm text-red-400">{{ message }}</p>
      }

      <button mat-flat-button color="primary" type="submit" [disabled]="submitting()" class="w-full">
        {{ submitting() ? 'Σύνδεση...' : 'Σύνδεση' }}
      </button>
    </form>
  </div>
</div>
```

- [ ] **Step 2: Αντικατάστησε το `register.html`**

```html
<div class="min-h-screen flex">
  <div class="hidden lg:flex flex-1 items-center justify-center bg-[var(--mat-sys-primary)] text-black p-12">
    <div class="max-w-md text-center">
      <h1 class="text-3xl font-semibold mb-2">Demo Gym</h1>
      <p class="text-black/70">Καλωσόρισες στην ομάδα.</p>
    </div>
  </div>

  <div class="flex flex-1 items-center justify-center p-4 lg:p-12 bg-[var(--app-bg-page)]">
    @if (invalidLink) {
      <p class="text-sm text-red-400">Μη έγκυρο link εγγραφής.</p>
    } @else {
      <form [formGroup]="form" (ngSubmit)="submit()" class="w-full max-w-sm flex flex-col gap-4">
        <h2 class="text-xl font-medium text-center text-white">Ολοκλήρωση εγγραφής</h2>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Όνομα</mat-label>
          <input matInput formControlName="firstName" autocomplete="given-name" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Επώνυμο</mat-label>
          <input matInput formControlName="lastName" autocomplete="family-name" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Κωδικός</mat-label>
          <input matInput type="password" formControlName="password" autocomplete="new-password" />
          <mat-hint>Τουλάχιστον 6 χαρακτήρες, ένα κεφαλαίο, ένα πεζό, έναν αριθμό και έναν ειδικό χαρακτήρα.</mat-hint>
          <mat-error>Τουλάχιστον 6 χαρακτήρες, ένα κεφαλαίο, ένα πεζό, έναν αριθμό και έναν ειδικό χαρακτήρα.</mat-error>
        </mat-form-field>

        @if (errorMessage(); as message) {
          <p class="text-sm text-red-400">{{ message }}</p>
        }

        <button mat-flat-button color="primary" type="submit" [disabled]="submitting()" class="w-full">
          {{ submitting() ? 'Υποβολή...' : 'Δημιουργία λογαριασμού' }}
        </button>
      </form>
    }
  </div>
</div>
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npx nx run-many -t lint build test -p customer`
Expected: PASS.

- [ ] **Checkpoint:** review — customer app ολοκληρώθηκε.

---

### Task 6: Staff Shell (topbar + responsive sidebar) + routing

**Files:**
- Create: `frontend/apps/staff/src/app/shell/staff-shell.ts`
- Create: `frontend/apps/staff/src/app/shell/staff-shell.html`
- Modify: `frontend/apps/staff/src/app/app.routes.ts`

- [ ] **Step 1: Δημιούργησε το `frontend/apps/staff/src/app/shell/staff-shell.ts`**

```ts
import { Component, inject } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '@frontend/auth';

@Component({
  selector: 'app-staff-shell',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatMenuModule,
    MatSidenavModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './staff-shell.html',
})
export class StaffShell {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly breakpointObserver = inject(BreakpointObserver);

  protected readonly isNarrow = toSignal(
    this.breakpointObserver.observe('(max-width: 767px)').pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
```

- [ ] **Step 2: Δημιούργησε το `frontend/apps/staff/src/app/shell/staff-shell.html`**

```html
<mat-sidenav-container class="min-h-screen bg-[var(--app-bg-page)]">
  <mat-sidenav
    #drawer
    [mode]="isNarrow() ? 'over' : 'side'"
    [opened]="!isNarrow()"
    class="w-56 bg-[var(--app-bg-surface)] border-r border-[var(--app-border)]"
  >
    <div class="p-4 font-semibold text-white">🏋 GymBooking</div>
    <mat-nav-list>
      <a mat-list-item routerLink="/dashboard" routerLinkActive="text-[var(--app-accent)]" [routerLinkActiveOptions]="{ exact: true }">
        Αρχική
      </a>
      <a mat-list-item routerLink="/class-types" routerLinkActive="text-[var(--app-accent)]">
        Είδη μαθημάτων
      </a>
      <a mat-list-item routerLink="/sessions" routerLinkActive="text-[var(--app-accent)]">
        Sessions
      </a>
    </mat-nav-list>
  </mat-sidenav>

  <mat-sidenav-content>
    <header class="flex items-center justify-between px-4 py-3 bg-[var(--app-bg-surface)] border-b border-[var(--app-border)]">
      @if (isNarrow()) {
        <button mat-icon-button (click)="drawer.toggle()" aria-label="Μενού">
          <mat-icon>menu</mat-icon>
        </button>
      } @else {
        <span></span>
      }
      <button mat-icon-button [matMenuTriggerFor]="userMenu" aria-label="Μενού χρήστη">
        <mat-icon>account_circle</mat-icon>
      </button>
      <mat-menu #userMenu="matMenu">
        <button mat-menu-item (click)="logout()">
          <mat-icon>logout</mat-icon>
          <span>Αποσύνδεση</span>
        </button>
      </mat-menu>
    </header>
    <main class="p-4 lg:p-8 max-w-5xl mx-auto">
      <router-outlet></router-outlet>
    </main>
  </mat-sidenav-content>
</mat-sidenav-container>
```

- [ ] **Step 3: Αντικατάστησε το `frontend/apps/staff/src/app/app.routes.ts`**

```ts
import { Route } from '@angular/router';
import { authGuard, roleGuard } from '@frontend/auth';
import { ClassTypes } from './class-types/class-types';
import { Dashboard } from './dashboard/dashboard';
import { Login } from './login/login';
import { Register } from './register/register';
import { Sessions } from './sessions/sessions';
import { StaffShell } from './shell/staff-shell';

export const appRoutes: Route[] = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  {
    path: '',
    component: StaffShell,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'class-types', component: ClassTypes, canActivate: [roleGuard('Instructor', 'Admin')] },
      { path: 'sessions', component: Sessions, canActivate: [roleGuard('Instructor', 'Admin')] },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
```

- [ ] **Step 4: Verify**

Run: `cd frontend && npx nx run-many -t lint build test -p staff`
Expected: PASS.

- [ ] **Checkpoint:** review του StaffShell (ιδιαίτερα το responsive sidenav) πριν προχωρήσουμε στις σελίδες.

---

### Task 7: Restyle Staff Dashboard

**Files:**
- Modify: `frontend/apps/staff/src/app/dashboard/dashboard.ts`
- Modify: `frontend/apps/staff/src/app/dashboard/dashboard.html`

- [ ] **Step 1: Αντικατάστησε το `dashboard.ts`**

```ts
import { Component, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { AuthService } from '@frontend/auth';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MatIconModule, RouterLink],
  templateUrl: './dashboard.html',
})
export class Dashboard {
  protected readonly authService = inject(AuthService);
}
```

- [ ] **Step 2: Αντικατάστησε το `dashboard.html`**

```html
<div class="flex flex-col gap-4 max-w-md">
  <h1 class="text-2xl font-bold text-white">Καλωσήρθες!</h1>
  <p class="text-sm text-[var(--app-text-secondary)]">
    Ρόλοι: {{ authService.user()?.roles?.join(', ') }}
  </p>

  <a
    routerLink="/class-types"
    class="flex items-center gap-3 bg-[var(--app-bg-surface)] border border-[var(--app-border)] rounded-lg p-4 hover:border-[var(--app-accent)] transition-colors"
  >
    <mat-icon class="text-[var(--app-accent)]">category</mat-icon>
    <span class="text-white">Είδη μαθημάτων</span>
  </a>

  <a
    routerLink="/sessions"
    class="flex items-center gap-3 bg-[var(--app-bg-surface)] border border-[var(--app-border)] rounded-lg p-4 hover:border-[var(--app-accent)] transition-colors"
  >
    <mat-icon class="text-[var(--app-accent)]">event</mat-icon>
    <span class="text-white">Ώρες μαθημάτων</span>
  </a>
</div>
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npx nx run-many -t lint build test -p staff`
Expected: PASS.

- [ ] **Checkpoint:** review.

---

### Task 8: Restyle Staff Class-Types (φόρμα + κάρτες)

**Files:**
- Modify: `frontend/apps/staff/src/app/class-types/class-types.ts`
- Modify: `frontend/apps/staff/src/app/class-types/class-types.html`

- [ ] **Step 1: Αντικατάστησε το `class-types.ts`**

```ts
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ClassTypeApiService } from '@frontend/data-access';
import { ClassType } from '@frontend/models';

@Component({
  selector: 'app-class-types',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './class-types.html',
})
export class ClassTypes {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ClassTypeApiService);

  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    defaultDurationMinutes: [60, [Validators.required, Validators.min(1)]],
    defaultCapacity: [10, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    this.load();
  }

  private load(): void {
    this.api.getAll().subscribe({
      next: (items) => this.classTypes.set(items),
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης ειδών μαθημάτων.'),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.submitting.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.form.reset({ defaultDurationMinutes: 60, defaultCapacity: 10 });
        this.load();
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Αποτυχία δημιουργίας είδους μαθήματος.');
      },
    });
  }
}
```

- [ ] **Step 2: Αντικατάστησε το `class-types.html`**

```html
<h1 class="text-2xl font-bold text-white mb-4">Είδη μαθημάτων</h1>

<form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 max-w-md mb-8">
  <mat-form-field appearance="outline">
    <mat-label>Όνομα</mat-label>
    <input matInput formControlName="name" />
  </mat-form-field>
  <mat-form-field appearance="outline">
    <mat-label>Περιγραφή</mat-label>
    <input matInput formControlName="description" />
  </mat-form-field>
  <mat-form-field appearance="outline">
    <mat-label>Διάρκεια (λεπτά)</mat-label>
    <input matInput type="number" formControlName="defaultDurationMinutes" />
  </mat-form-field>
  <mat-form-field appearance="outline">
    <mat-label>Χωρητικότητα</mat-label>
    <input matInput type="number" formControlName="defaultCapacity" />
  </mat-form-field>
  @if (errorMessage(); as message) {
    <p class="text-sm text-red-400">{{ message }}</p>
  }

  <button mat-flat-button color="primary" type="submit" [disabled]="submitting()">
    Προσθήκη
  </button>
</form>

<div class="flex flex-col gap-3 max-w-md">
  @for (ct of classTypes(); track ct.id) {
    <div class="bg-[var(--app-bg-surface)] border border-[var(--app-border)] rounded-lg p-4 text-sm text-white">
      <div class="font-medium">{{ ct.name }}</div>
      <div class="text-[var(--app-text-secondary)]">{{ ct.defaultDurationMinutes }}′ / {{ ct.defaultCapacity }} θέσεις</div>
    </div>
  } @empty {
    <p class="text-[var(--app-text-secondary)]">Δεν υπάρχουν είδη μαθημάτων ακόμα.</p>
  }
</div>
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npx nx run-many -t lint build test -p staff`
Expected: PASS.

- [ ] **Checkpoint:** review.

---

### Task 9: Restyle Staff Sessions (φόρμα + κάρτες)

**Files:**
- Modify: `frontend/apps/staff/src/app/sessions/sessions.ts`
- Modify: `frontend/apps/staff/src/app/sessions/sessions.html`

- [ ] **Step 1: Αντικατάστησε το `sessions.ts`**

```ts
import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ClassSessionApiService, ClassTypeApiService } from '@frontend/data-access';
import { ClassSession, ClassType } from '@frontend/models';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, DatePipe],
  templateUrl: './sessions.html',
})
export class Sessions {
  private readonly fb = inject(FormBuilder);
  private readonly sessionApi = inject(ClassSessionApiService);
  private readonly classTypeApi = inject(ClassTypeApiService);

  protected readonly sessions = signal<ClassSession[]>([]);
  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    classTypeId: ['', Validators.required],
    startsAt: ['', Validators.required], // datetime-local (τοπική) → μετατροπή σε ISO UTC στο submit
    durationMinutes: [60, [Validators.required, Validators.min(1)]],
    capacity: [10, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    this.classTypeApi.getAll().subscribe({
      next: (items) => this.classTypes.set(items),
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης ειδών μαθημάτων.'),
    });
    this.load();
  }

  private load(): void {
    this.sessionApi.getAll().subscribe({
      next: (items) => this.sessions.set(items),
      error: () => this.errorMessage.set('Αποτυχία φόρτωσης sessions.'),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    this.errorMessage.set(null);
    this.submitting.set(true);
    this.sessionApi
      .create({
        classTypeId: raw.classTypeId,
        // instructorId παραλείπεται → το backend βάζει τον τρέχοντα instructor.
        startsAt: new Date(raw.startsAt).toISOString(),
        durationMinutes: raw.durationMinutes,
        capacity: raw.capacity,
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.load();
        },
        error: () => {
          this.submitting.set(false);
          this.errorMessage.set('Αποτυχία δημιουργίας session.');
        },
      });
  }
}
```

- [ ] **Step 2: Αντικατάστησε το `sessions.html`**

```html
<h1 class="text-2xl font-bold text-white mb-4">Ώρες μαθημάτων (sessions)</h1>

<form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 max-w-md mb-8">
  <mat-form-field appearance="outline">
    <mat-label>Είδος μαθήματος</mat-label>
    <mat-select formControlName="classTypeId">
      @for (ct of classTypes(); track ct.id) {
        <mat-option [value]="ct.id">{{ ct.name }}</mat-option>
      }
    </mat-select>
  </mat-form-field>
  <mat-form-field appearance="outline">
    <mat-label>Έναρξη</mat-label>
    <input matInput type="datetime-local" formControlName="startsAt" />
  </mat-form-field>
  <mat-form-field appearance="outline">
    <mat-label>Διάρκεια (λεπτά)</mat-label>
    <input matInput type="number" formControlName="durationMinutes" />
  </mat-form-field>
  <mat-form-field appearance="outline">
    <mat-label>Χωρητικότητα</mat-label>
    <input matInput type="number" formControlName="capacity" />
  </mat-form-field>
  @if (errorMessage(); as message) {
    <p class="text-sm text-red-400">{{ message }}</p>
  }

  <button mat-flat-button color="primary" type="submit" [disabled]="submitting()">
    Δημιουργία session
  </button>
</form>

<div class="flex flex-col gap-3 max-w-md">
  @for (s of sessions(); track s.id) {
    <div class="bg-[var(--app-bg-surface)] border border-[var(--app-border)] rounded-lg p-4 text-sm text-white">
      <div class="font-medium">{{ s.classTypeName }}</div>
      <div class="text-[var(--app-text-secondary)]">
        {{ s.startsAt | date: 'dd/MM HH:mm' }} — {{ s.instructorName }} — {{ s.bookedCount }}/{{ s.capacity }}
      </div>
    </div>
  } @empty {
    <p class="text-[var(--app-text-secondary)]">Δεν υπάρχουν sessions ακόμα.</p>
  }
</div>
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npx nx run-many -t lint build test -p staff`
Expected: PASS.

- [ ] **Checkpoint:** review.

---

### Task 10: Restyle Staff Login + Register

**Files:**
- Modify: `frontend/apps/staff/src/app/login/login.html`
- Modify: `frontend/apps/staff/src/app/register/register.html`

(Τα `.ts` αρχεία δεν αλλάζουν.)

- [ ] **Step 1: Αντικατάστησε το `login.html`**

```html
<div class="min-h-screen flex">
  <div class="hidden lg:flex flex-1 items-center justify-center bg-[var(--mat-sys-primary)] text-black p-12">
    <div class="max-w-md text-center">
      <h1 class="text-3xl font-semibold mb-2">Demo Gym</h1>
      <p class="text-black/70">Πάνελ διαχείρισης προσωπικού</p>
    </div>
  </div>

  <div class="flex flex-1 items-center justify-center p-4 lg:p-12 bg-[var(--app-bg-page)]">
    <form [formGroup]="form" (ngSubmit)="submit()" class="w-full max-w-sm flex flex-col gap-4">
      <h2 class="text-xl font-medium text-center text-white">Σύνδεση</h2>

      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Email</mat-label>
        <input matInput type="email" formControlName="email" autocomplete="email" />
      </mat-form-field>

      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Κωδικός</mat-label>
        <input matInput type="password" formControlName="password" autocomplete="current-password" />
      </mat-form-field>

      @if (errorMessage(); as message) {
        <p class="text-sm text-red-400">{{ message }}</p>
      }

      <button mat-flat-button color="primary" type="submit" [disabled]="submitting()" class="w-full">
        {{ submitting() ? 'Σύνδεση...' : 'Σύνδεση' }}
      </button>
    </form>
  </div>
</div>
```

- [ ] **Step 2: Αντικατάστησε το `register.html`**

```html
<div class="min-h-screen flex">
  <div class="hidden lg:flex flex-1 items-center justify-center bg-[var(--mat-sys-primary)] text-black p-12">
    <div class="max-w-md text-center">
      <h1 class="text-3xl font-semibold mb-2">Demo Gym</h1>
      <p class="text-black/70">Πάνελ διαχείρισης προσωπικού</p>
    </div>
  </div>

  <div class="flex flex-1 items-center justify-center p-4 lg:p-12 bg-[var(--app-bg-page)]">
    @if (invalidLink) {
      <p class="text-sm text-red-400">Μη έγκυρο link εγγραφής.</p>
    } @else {
      <form [formGroup]="form" (ngSubmit)="submit()" class="w-full max-w-sm flex flex-col gap-4">
        <h2 class="text-xl font-medium text-center text-white">Ολοκλήρωση εγγραφής</h2>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Όνομα</mat-label>
          <input matInput formControlName="firstName" autocomplete="given-name" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Επώνυμο</mat-label>
          <input matInput formControlName="lastName" autocomplete="family-name" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Κωδικός</mat-label>
          <input matInput type="password" formControlName="password" autocomplete="new-password" />
          <mat-hint>Τουλάχιστον 6 χαρακτήρες, ένα κεφαλαίο, ένα πεζό, έναν αριθμό και έναν ειδικό χαρακτήρα.</mat-hint>
          <mat-error>Τουλάχιστον 6 χαρακτήρες, ένα κεφαλαίο, ένα πεζό, έναν αριθμό και έναν ειδικό χαρακτήρα.</mat-error>
        </mat-form-field>

        @if (errorMessage(); as message) {
          <p class="text-sm text-red-400">{{ message }}</p>
        }

        <button mat-flat-button color="primary" type="submit" [disabled]="submitting()" class="w-full">
          {{ submitting() ? 'Υποβολή...' : 'Δημιουργία λογαριασμού' }}
        </button>
      </form>
    }
  </div>
</div>
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npx nx run-many -t lint build test -p staff`
Expected: PASS.

- [ ] **Checkpoint:** review — staff app ολοκληρώθηκε.

---

### Task 11: Τελικός έλεγχος (και τα δύο apps)

**Files:** καμία αλλαγή — μόνο verification.

- [ ] **Step 1: Πλήρες lint + build + test**

Run: `cd frontend && npx nx run-many -t lint build test`
Expected: PASS για customer, staff, και τα libs (models, data-access, auth, ui).

- [ ] **Step 2: Οπτικός έλεγχος customer (mobile)**

Run preview tool: `nx serve customer`, resize σε mobile (375×812), δες:
- Bottom tab bar εμφανίζεται σωστά, δεν καλύπτει το τελευταίο στοιχείο της λίστας.
- Active tab (Αρχική/Μαθήματα) highlighted με `--app-accent`.
- Dark contrast σε κείμενο/κουμπιά/form fields OK.

- [ ] **Step 3: Οπτικός έλεγχος staff (desktop + tablet)**

Run preview tool: `nx serve staff`, δες σε desktop (1280px) το sidebar μόνιμα ορατό, μετά resize σε 375-767px και επιβεβαίωσε ότι το sidebar γίνεται drawer (hamburger button εμφανίζεται, ανοίγει/κλείνει με κλικ).

- [ ] **Step 4: Contrast spot-check σε primary buttons**

Αν το κείμενο πάνω σε `mat-flat-button color="primary"` (λέξεις πάνω σε neon lime) δεν διαβάζεται καθαρά, πρόσθεσε override στο αντίστοιχο `styles.scss`:

```scss
:root {
  --mat-sys-on-primary: #15161a;
}
```

- [ ] **Checkpoint τελικό:** πλήρες review και από τα δύο apps πριν ο χρήστης κάνει commit.
