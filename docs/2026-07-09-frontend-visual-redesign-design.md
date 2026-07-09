# Frontend Visual Redesign — Design Doc

**Ημερομηνία:** 2026-07-09
**Σκοπός:** Πρώτο πέρασμα οπτικού redesign στα customer & staff Angular apps — απλό, responsive, εύκολο να εξελιχθεί αργότερα.

## Πλαίσιο

Σήμερα και τα δύο apps χρησιμοποιούν το default Angular Material 3 theme (azure/blue palettes) χωρίς κοινή δομή γύρω από τις σελίδες — κάθε σελίδα είναι αυτόνομη, με ένα χειροκίνητο `← Πίσω στην αρχική` link (pending, uncommitted) αντί για πραγματική πλοήγηση. Δεν υπάρχει κανένα κοινό οπτικό component σε χρήση (το `libs/ui` είναι ακόμα το αρχικό Nx placeholder).

Το redesign αυτό καλύπτει: θέμα χρωμάτων, κοινό "shell" (πλοήγηση) ανά app, restyle των υπαρχουσών σελίδων. Δεν καλύπτει νέες λειτουργίες (αυτές μπαίνουν στη Φ4 κ.ε.).

## 1. Χρωματική παλέτα & θέμα

Κατεύθυνση: **Bold Dark** με neon lime accent, εγκεκριμένη μέσω mockups.

| Token | Τιμή | Χρήση |
|---|---|---|
| `--bg-page` | `#15161a` | φόντο σελίδας |
| `--bg-surface` | `#1e2026` | κάρτες, topbar, sidebar |
| `--border` | `#33353c` | περιγράμματα καρτών/διαχωριστικά |
| `--text-primary` | `#e8e8ea` | κύριο κείμενο |
| `--text-secondary` | `#8a8d95` | δευτερεύον κείμενο |
| `--accent` | `#c6ff3d` | CTA κουμπιά, active nav state, τίτλοι έμφασης |

- Μόνο dark theme προς το παρόν — **χωρίς** light/dark toggle (μπορεί να προστεθεί αργότερα ως ξεχωριστό task).
- Υλοποίηση: `mat.theme()` στο `styles.scss` (και τα δύο apps) με `theme-type: dark`, βάση παλέτα `chartreuse` (πιο κοντά στο lime από τις διαθέσιμες M3 παλέτες), και override των σχετικών `--mat-sys-primary` / `--mat-sys-*` CSS variables ώστε το accent να πέσει ακριβώς στο `#c6ff3d`. Το warn/error χρώμα του Material παραμένει ως έχει (ήδη σχεδιασμένο για αντίθεση σε dark themes).

## 2. Shared "Shell" ανά app

**Customer (`apps/customer/src/app/shell/`):** νέο standalone `CustomerShell` component.
- Topbar: λογότυπο/τίτλος + user menu (avatar → dropdown με "Αποσύνδεση").
- Bottom tab bar (4 στοιχεία): Αρχική, Μαθήματα, Κρατήσεις μου, Προφίλ.
- Wrap γύρω από τα authenticated routes (`dashboard`, `sessions`) μέσω routing (layout route με `<router-outlet>` παιδί, ή wrapping στο `app.html` με βάση auth state).
- Login/register **παραμένουν εκτός shell** — dark restyle μόνο στην κάρτα φόρμας, χωρίς topbar/bottom-nav.

**Staff (`apps/staff/src/app/shell/`):** νέο standalone `StaffShell` component.
- Sidebar (αριστερά): λογότυπο + nav items (Είδη μαθημάτων, Sessions).
- Topbar πάνω: user menu.
- Wrap γύρω από `dashboard`, `class-types`, `sessions`.
- Login/register εκτός shell, ίδιο restyle pattern με customer.

Κάθε shell ζει μέσα στο δικό του app — **όχι** σε shared lib, καθώς η πλοήγηση διαφέρει τελείως μεταξύ των δύο apps. Αν αργότερα οι δομές συγκλίνουν, μπορούν να εξαχθούν σε `libs/ui`.

Το pending uncommitted diff (τα `← Πίσω στην αρχική` links σε sessions/class-types) **αντικαθίσταται** — αφαιρείται μιας και η πλοήγηση καλύπτεται πλέον από το shell.

## 3. Responsiveness

- **Customer:** bottom nav πάντα ορατό/fixed, με `padding-bottom: env(safe-area-inset-bottom)` για συσκευές με home indicator (iOS). Περιεχόμενο σε container `max-w-2xl mx-auto` ώστε να μην απλώνεται υπερβολικά σε πλατύτερες οθόνες, διατηρώντας τη mobile-first λογική.
- **Staff:** sidebar ορατό σε `≥768px` (Tailwind `md` breakpoint). Κάτω από αυτό το πλάτος, το sidebar γίνεται topbar + hamburger button που ανοίγει `mat-sidenav` σε `mode="over"` (drawer overlay) — ώστε το staff app να μη σπάει σε tablet/κινητό.

## 4. Component depth

Ελαφρύ πέρασμα (YAGNI) — **όχι** νέο design-system στο `libs/ui` σε αυτό το task:
- Μόνο τα δύο Shell components (ένα ανά app).
- Κοινά Tailwind utility patterns (π.χ. `bg-[#1e2026] border border-[#33353c] rounded-lg p-4`) επαναλαμβανόμενα ως inline classes στις σελίδες όπου χρειάζεται κάρτα — αποδεκτό duplication σε αυτή την κλίμακα.
- Λίστες (sessions, class-types, bookings) γίνονται κάρτες αντί για text-only `mat-list-item` γραμμές, για καλύτερη οπτική ιεράρχηση.
- Extraction σε reusable components (`libs/ui`) μπαίνει ως μελλοντικό task αν/όταν υπάρξει πραγματική επανάληψη μοτίβου σε 3+ σημεία.

## 5. Σελίδες που αλλάζουν

**Customer:** `app.html`/`app.ts` (shell wiring), `dashboard`, `sessions`, `login`, `register`.
**Staff:** `app.html`/`app.ts` (shell wiring), `dashboard`, `class-types`, `sessions`, `login`, `register`.
**Global:** `styles.scss` σε `apps/customer/src` και `apps/staff/src` (theme).

## 6. Verification

Οπτικός έλεγχος μέσω preview tool (`nx serve customer` / `nx serve staff`):
- Mobile viewport (375px) → customer bottom-nav εμφανίζεται σωστά, δεν καλύπτει περιεχόμενο.
- Desktop + tablet (768px breakpoint) → staff sidebar collapse σε drawer.
- Dark contrast check σε κύρια στοιχεία (κείμενο, κουμπιά, form fields).
- Καμία λειτουργική αλλαγή αναμένεται (μόνο visual/structural) — τα υπάρχοντα unit tests δεν πρέπει να επηρεαστούν, αλλά θα ελεγχθούν (`nx test`) μετά τις αλλαγές template/imports.

## Εκτός εμβέλειας

- Light/dark toggle.
- Νέο design-system component library.
- Οποιαδήποτε νέα λειτουργία πέρα από τη Φ3 δομή (Φ4+ μένει ξεχωριστό task).
