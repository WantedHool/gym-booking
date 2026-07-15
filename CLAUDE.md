# CLAUDE.md — Gym Booking Project

> Αυτό το αρχείο διαβάζεται αυτόματα σε κάθε νέο chat. Κράτα το **σύντομο** — λεπτομέρειες στο spec.

## Τι είναι
Multi-tenant SaaS web app για διαχείριση κρατήσεων & προγραμμάτων γυμναστηρίου.
Διπλός σκοπός: πτυχιακή + βάση για πραγματικό προϊόν.
**Πλήρες spec (single source of truth):** [docs/2026-06-28-gym-booking-design.md](docs/2026-06-28-gym-booking-design.md) · **Implementation plan:** [docs/2026-06-28-implementation-plan.md](docs/2026-06-28-implementation-plan.md)

## Stack
- **Frontend:** Angular 21 **Nx** monorepo (1 workspace) → `customer` app (mobile-first) + `staff` app (desktop-first) + shared libs. Material + Tailwind v4 + Signals.
- **Backend:** .NET 10 Web API (layered) · EF Core · ASP.NET Core Identity + JWT · Serilog · Scalar (OpenAPI).
- **DB:** PostgreSQL.
- **Infra:** Docker (local-first) · Git/GitHub · GitHub Actions (CI/CD).

## Δομή repo
```
/backend            — .NET 10 solution (GymBooking.slnx): src/{Api,Core,Data} + tests/GymBooking.Tests ✅
/frontend           — Nx workspace: apps/{customer,staff} + libs/{models,data-access,auth,ui} ✅
/docker             — docker-compose.yml (Postgres 16 + Papercut) ✅
/docs               — spec + implementation plan ✅
/.github/workflows  — ci.yml (build+lint· ανενεργό μέχρι να ανέβει σε GitHub)
nuget.config        — pin σε nuget.org (παρακάμπτει το εταιρικό feed nuget.eurotel.gr)
```
Import paths FE libs: `@frontend/{models,data-access,auth,ui}`. Module boundaries επιβάλλονται μέσω tags (scope/type) στο `eslint.config.mjs`.

## Εντολές (build / run / test)
```
# Local infra (πρώτα)
docker compose -f docker/docker-compose.yml up -d   # Postgres + Papercut · UI: http://localhost:8080

# Backend (από /backend)
dotnet build
dotnet run --project src/GymBooking.Api             # Scalar docs: /scalar/v1 · health: /health
dotnet ef migrations add <Name> -p src/GymBooking.Data -s src/GymBooking.Api
dotnet ef database update       -p src/GymBooking.Data -s src/GymBooking.Api

# Frontend (από /frontend · θέλει Node 20 → `nvm use 20`)
npm ci
npx nx serve customer            # mobile-first app
npx nx serve staff               # desktop-first app
npx nx run-many -t lint build    # ό,τι τρέχει το CI
```

## Βασικές συμβάσεις (περίληψη — πλήρη στο spec)
- Multi-tenancy: shared DB + `TenantId` + EF global query filters· TenantId πάντα από JWT.
- Auth phased: Φάση 1 access-token-only (πτυχιακή) → Φάση 2 refresh rotation/httpOnly (προϊόν).
- Atomic booking: transaction + `SELECT … FOR UPDATE` (no overbooking).
- Dates σε UTC (display Europe/Athens) · soft-delete παντού · i18n-ready (UI Ελληνικά).
- Real-time & CI/CD μπαίνουν phased (δες spec).

## Πρόοδος (progress tracker)
_(ενημέρωνέ το καθώς προχωράς — έτσι ένα νέο chat ξέρει πού είσαι)_
- [x] Setup (Nx monorepo, Postgres+Papercut, EF, backend layered, Serilog/Scalar/health) — _tenant infra μετακινήθηκε στη Φ1_
- [x] Φ1 — Auth + ρόλοι + invitation/registration
- [x] Φ2 — ClassType/ClassSession CRUD + instructor
- [x] Φ3 — Booking core (atomic) + tests
- [x] Φ4 — Weekly schedule + φίλτρα + customer dashboard
- [x] Φ5 — Subscriptions/plans + consumption
- [x] Φ6 — Waitlist + cancellation policy
- [x] Φ7 — Admin/staff dashboard + responsive QA
- [ ] Φ8 — Hardening + security + deployment (Docker/CI-CD)

**Τώρα δουλεύω:** Setup ✅ + Φ1 ✅ + Φ2 ✅ + Φ3 ✅ + Φ4 ✅ + Φ5 ✅ + Φ6 ✅ + Φ7 ✅ (ολοκληρωμένο 14 Ιουλίου, βάσει [docs/2026-07-14-phase7-admin-dashboard-plan.md](docs/2026-07-14-phase7-admin-dashboard-plan.md)· `UserService`/`UsersController` (list/role/active, self-modify guard), `ApplicationUser.CreatedAt` + migration, staff booking oversight (`GetRosterAsync`, `BookForAsync` walk-in, `CancelByStaffAsync` — refactor `CancelAsync`→`CancelCoreAsync` κοινός πυρήνας), `TenantSettingsService`/`TenantController` (name + `CancellationHours`), reject-inactive-login· FE: `users`/`settings`/`sessions/:id/roster` staff pages, νέα data-access services, admin-only nav wiring, responsive QA pass (customer mobile 390px + staff desktop 1280px/tablet 768px — κανένα overflow στις νέες σελίδες). **Εύρημα εκτός scope:** προϋπάρχον bug στο staff sidenav drawer σε στενές οθόνες (μένει εκτός οθόνης όταν ανοίγει, stuck CSS transition) — καταγράφηκε ως ξεχωριστό background task, δεν το προκάλεσε η Φ7. **Πρόοδος: ~98h / ~98h (~100% του αρχικού πλάνου).** **Επόμενο: Φ8 — Hardening + security + deployment** (θα αναλυθεί σε bite-sized tasks όταν ξεκινήσουμε· θα χρειαστεί να συμπεριλάβει και το drawer bug).
> ⚙️ **Μοτίβο υλοποίησης (από Φ2):** ο **Claude γράφει** τον κώδικα (+ εξηγεί), ο **χρήστης κάνει review**. Πριν από κάθε ενέργεια → επιβεβαίωση. Ποτέ commit (το κάνει ο χρήστης).
> 🚀 **Πριν κωδικοποίηση:** `nvm use 20` (frontend) · `docker compose -f docker/docker-compose.yml up -d` (DB).

## Σημειώσεις / ανοιχτά θέματα
_(ό,τι θες να θυμάσαι ή να συζητήσουμε αργότερα)_
- **Git:** local identity = προσωπικό (`Xrhstos Rimpas <christarasrib@gmail.com>`)· το global εργασιακό (`chrimpas@eurotel.gr`) μένει ανέπαφο. GitHub repo (push + CI activation + branch protection) = επόμενη φάση, το κάνει ο χρήστης.
- **NuGet gotcha:** εταιρικό feed `nuget.eurotel.gr` (401) → λύθηκε με `nuget.config` (`<clear/>` + nuget.org).
- **Deprecation:** ο `@nx/eslint:lint` executor θα καταργηθεί στο Nx v24 (migration `convert-to-inferred` σε hardening).
