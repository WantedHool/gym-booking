# CLAUDE.md — Gym Booking Project

> Αυτό το αρχείο διαβάζεται αυτόματα σε κάθε νέο chat. Κράτα το **σύντομο** — λεπτομέρειες στο spec.

## Τι είναι
Multi-tenant SaaS web app για διαχείριση κρατήσεων & προγραμμάτων γυμναστηρίου.
Διπλός σκοπός: πτυχιακή + βάση για πραγματικό προϊόν.
**Πλήρες spec (single source of truth):** [docs/specs/2026-06-28-gym-booking-design.md](docs/specs/2026-06-28-gym-booking-design.md)

## Stack
- **Frontend:** Angular monorepo (1 workspace) → `customer` app (mobile-first) + `staff` app (desktop-first) + shared libs. Material + Tailwind + Signals.
- **Backend:** .NET Web API · EF Core · ASP.NET Core Identity + JWT · Serilog · Swagger.
- **DB:** PostgreSQL.
- **Infra:** Docker (local-first) · Git/GitHub · GitHub Actions (CI/CD).

## Δομή repo
_(συμπλήρωσε καθώς δημιουργείς φακέλους — βοηθάει να βρίσκω γρήγορα τα πράγματα)_
```
/backend        — .NET solution (TODO)
/frontend       — Angular workspace: apps/customer, apps/staff, libs/* (TODO)
/docs/specs     — spec document ✅
/docker         — docker-compose, Dockerfiles (TODO)
```

## Εντολές (build / run / test)
_(συμπλήρωσε μόλις υπάρχει κώδικας — αυτές τις τρέχω για verification)_
```
# Backend
dotnet build
dotnet test
dotnet run --project ...

# Frontend
npm ci
npm run start:customer   # TODO
npm run start:staff      # TODO
npm test

# Local infra
docker compose up -d      # Postgres + Papercut
```

## Βασικές συμβάσεις (περίληψη — πλήρη στο spec)
- Multi-tenancy: shared DB + `TenantId` + EF global query filters· TenantId πάντα από JWT.
- Auth phased: Φάση 1 access-token-only (πτυχιακή) → Φάση 2 refresh rotation/httpOnly (προϊόν).
- Atomic booking: transaction + `SELECT … FOR UPDATE` (no overbooking).
- Dates σε UTC (display Europe/Athens) · soft-delete παντού · i18n-ready (UI Ελληνικά).
- Real-time & CI/CD μπαίνουν phased (δες spec).

## Πρόοδος (progress tracker)
_(ενημέρωνέ το καθώς προχωράς — έτσι ένα νέο chat ξέρει πού είσαι)_
- [ ] Setup (monorepo, Postgres, EF, tenant infra)
- [ ] Φ1 — Auth + ρόλοι + invitation/registration
- [ ] Φ2 — ClassType/ClassSession CRUD + instructor
- [ ] Φ3 — Booking core (atomic) + tests
- [ ] Φ4 — Weekly schedule + φίλτρα + customer dashboard
- [ ] Φ5 — Subscriptions/plans + consumption
- [ ] Φ6 — Waitlist + cancellation policy
- [ ] Φ7 — Admin/staff dashboard + responsive QA
- [ ] Φ8 — Hardening + security + deployment (Docker/CI-CD)

**Τώρα δουλεύω:** _(γράψε εδώ τι κάνεις αυτή τη στιγμή)_

## Σημειώσεις / ανοιχτά θέματα
_(ό,τι θες να θυμάσαι ή να συζητήσουμε αργότερα)_
- Git 2 accounts (προσωπικό/δουλειά) στο ίδιο PC: [docs/notes/git-multi-account-setup.md](docs/notes/git-multi-account-setup.md)
