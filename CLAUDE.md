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
- [ ] Φ4 — Weekly schedule + φίλτρα + customer dashboard
- [ ] Φ5 — Subscriptions/plans + consumption
- [ ] Φ6 — Waitlist + cancellation policy
- [ ] Φ7 — Admin/staff dashboard + responsive QA
- [ ] Φ8 — Hardening + security + deployment (Docker/CI-CD)

**Τώρα δουλεύω:** Setup ✅ + Φ1 ✅ + Φ2 ✅ + Φ3 ✅ (Tasks 25–34, ολοκληρωμένα 9 Ιουλίου· atomic booking με `SELECT...FOR UPDATE`, concurrency proof, anti-double-booking, cancel, HTTP API + customer UI). **Πρόοδος: ~52h / ~98h (~53%) — μπροστά από το χρονοδιάγραμμα.** **Επόμενο: Φ4 — Weekly schedule + φίλτρα + customer dashboard** (Tasks 35–40, ήδη αναλυτικά στο plan).
> ⚙️ **Μοτίβο υλοποίησης (από Φ2):** ο **Claude γράφει** τον κώδικα (+ εξηγεί), ο **χρήστης κάνει review**. Πριν από κάθε ενέργεια → επιβεβαίωση. Ποτέ commit (το κάνει ο χρήστης).
> 🚀 **Πριν κωδικοποίηση:** `nvm use 20` (frontend) · `docker compose -f docker/docker-compose.yml up -d` (DB).

## Σημειώσεις / ανοιχτά θέματα
_(ό,τι θες να θυμάσαι ή να συζητήσουμε αργότερα)_
- **Git:** local identity = προσωπικό (`Xrhstos Rimpas <christarasrib@gmail.com>`)· το global εργασιακό (`chrimpas@eurotel.gr`) μένει ανέπαφο. GitHub repo (push + CI activation + branch protection) = επόμενη φάση, το κάνει ο χρήστης.
- **NuGet gotcha:** εταιρικό feed `nuget.eurotel.gr` (401) → λύθηκε με `nuget.config` (`<clear/>` + nuget.org).
- **Deprecation:** ο `@nx/eslint:lint` executor θα καταργηθεί στο Nx v24 (migration `convert-to-inferred` σε hardening).
