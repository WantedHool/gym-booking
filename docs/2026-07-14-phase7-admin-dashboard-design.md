# Φάση 7 — Admin/Staff dashboard + Responsive QA (design)

> Spec της Φ7. Single source of truth για την υλοποίηση· το detailed implementation plan παράγεται από εδώ.
> Σχετικά: [spec](2026-06-28-gym-booking-design.md) · [implementation plan](2026-06-28-implementation-plan.md)

## Σκοπός
Εργαλεία διαχείρισης για το προσωπικό του γυμναστηρίου πέρα από τη δημιουργία μαθημάτων:
διαχείριση χρηστών/ρόλων, εποπτεία & χειρισμό κρατήσεων, βασικές ρυθμίσεις tenant. Μετά, responsive QA pass.

## Αποφάσεις (από brainstorming 2026-07-14)
- **User management:** λίστα + αλλαγή ρόλου + activate/deactivate (όχι πλήρες CRUD).
- **Booking oversight:** roster + ακύρωση εκ μέρους πελάτη + walk-in χειροκίνητη κράτηση.
- **Tenant settings:** MINIMAL (όνομα + `CancellationHours`)· εμπλουτισμός αργότερα.
- **SignalR:** εκτός Φ7.
- **Ρόλοι:** user management → μόνο Admin· booking oversight → Admin + Instructor.
- **Responsive QA:** έλεγχος + διορθώσεις όπου σπάει· customer mobile-first, staff desktop-first.

---

## 1. User management (Admin only)

### Backend — `UsersController` + `UserService`
- `GET /users` → λίστα χρηστών του tenant: `{ id, email, firstName, lastName, roles[], createdAt, isActive }`. TenantId από JWT.
- `PUT /users/{id}/role` `{ role }` → αλλαγή ρόλου (Customer / Instructor / Admin).
  - Guard: μόνο Admin.
  - **Guardrail:** ο Admin δεν μπορεί να αφαιρέσει τον **δικό του** Admin ρόλο (να μη μείνει tenant χωρίς admin). Επιστρέφει 409/400.
- `PUT /users/{id}/deactivate` / `PUT /users/{id}/activate` → soft toggle (`isActive`). Ανενεργός χρήστης δεν κάνει login/κρατήσεις.
  - **Guardrail:** ο Admin δεν απενεργοποιεί τον εαυτό του.

### Frontend — `staff/users`
Πίνακας Material: email, όνομα, chips ρόλων, ημ/νία, active toggle. Αλλαγή ρόλου μέσω dialog. Route `roleGuard('Admin')`.

---

## 2. Booking oversight (Admin + Instructor)

Επεκτείνουμε `BookingsController`/`BookingService` με **staff variants** — δεν αλλάζουμε τα self-service endpoints.

### Backend
- `GET /sessions/{id}/roster` → `{ confirmed: [{ bookingId, userId, name, email, createdAt }], waitlist: [{ userId, name, position }] }`.
- `POST /sessions/{id}/bookings` `{ userId }` → **walk-in**: staff-εκδοχή του `BookAsync(userId, sessionId)` χωρίς ownership. Ίδιο atomic tx + όλοι οι έλεγχοι (capacity, time conflict, subscription).
- `DELETE /bookings/{id}` (staff) → **cancel-on-behalf**: staff-εκδοχή του `CancelAsync` χωρίς ownership check, με **override** της cancellation-window πολιτικής (ο staff μπορεί να ακυρώσει και εκτός παραθύρου). Το auto-promote της waitlist δουλεύει ως έχει.

**Reuse:** αναδιοργάνωση ώστε τα self & staff paths να μοιράζονται τους helpers (`HasTimeConflictAsync`, `TryConsumeSubscriptionAsync`, auto-promote block). Η διαφορά είναι μόνο: (α) ποιος `userId`, (β) αν ελέγχεται ownership, (γ) αν επιβάλλεται η cancellation window.

### Frontend
Στη `staff/sessions`, κάθε session → **roster view**: λίστα confirmed (με cancel), waitlist, «+ Προσθήκη πελάτη» (walk-in· search χρήστη του tenant). Route `roleGuard('Admin','Instructor')`.

---

## 3. Tenant settings (Admin only) — MINIMAL
- Backend: `GET /tenant/settings` → `{ name, cancellationHours }` · `PUT /tenant/settings` `{ name, cancellationHours }` (validation: `cancellationHours >= 0`).
- Frontend: μικρή φόρμα `staff/settings`. Route `roleGuard('Admin')`.

---

## 4. Responsive QA
Μεθοδικό pass μέσω browser preview + resize:
- **Customer** (mobile-first): viewport ~375–430px — login, register, schedule, session cards, bookings.
- **Staff** (desktop-first): desktop + tablet — dashboard, sessions/roster, users, memberships, invitations, settings, sidenav.
Καταγραφή ό,τι σπάει (overflow, πίνακες που δεν χωράνε, dialogs, sidenav) → διορθώσεις. Screenshot proof στο τέλος.

---

## Navigation & guards
- Νέα sidenav items στο `staff-shell`: **Χρήστες** & **Ρυθμίσεις** (Admin-only via υπάρχον `isAdmin`). Roster μέσα από **Sessions** (Admin+Instructor).
- Routes: `roleGuard('Admin')` για users/settings· `roleGuard('Admin','Instructor')` για roster/walk-in.

## Data model changes
**Καμία schema αλλαγή / migration** — όλα τα πεδία υπάρχουν ήδη:
- `ApplicationUser`: έχει `IsActive` (bool) **και** `Status` (`UserStatus` enum). ⚠️ Επικάλυψη — στο implementation θα αποφασιστεί ποιο είναι η πηγή αλήθειας για deactivate (πρόταση: χρήση `IsActive`, ευθυγράμμιση/κατάργηση του `Status` αν δεν χρησιμοποιείται αλλού).
- `Tenant`: υπάρχοντα `Name`, `CancellationHours`.

## Testing
- Backend: unit/integration για role-change guardrails (self-demote block), walk-in (capacity/conflict/subscription), cancel-on-behalf (override window + auto-promote), tenant isolation σε όλα τα νέα endpoints.
- Ακολουθεί TDD ανά task.

## Εκτός Φ7 (ρητά)
SignalR live availability· πλήρες CRUD χρηστών· πλούσια tenant settings (booking window, capacity defaults, waitlist toggle, branding)· deployment/public URL (→ Φ8).

## Εκτίμηση
~12h: Users ~3h · Booking oversight ~4h · Settings ~1.5h · Responsive QA ~3h · glue/testing ~0.5h.
