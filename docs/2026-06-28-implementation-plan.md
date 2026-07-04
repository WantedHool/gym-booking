# Gym Booking — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Multi-tenant SaaS web app για κρατήσεις & προγράμματα γυμναστηρίου (πτυχιακή + βάση προϊόντος).

**Architecture:** Angular Nx monorepo (`customer` mobile-first + `staff` desktop-first + shared libs) ↔ .NET Web API (layered, EF Core, Identity+JWT) ↔ PostgreSQL. Shared DB multi-tenancy με `TenantId` + EF global query filters.

**Tech Stack:** Angular (Nx, Material, Tailwind, Signals) · .NET 8 Web API · EF Core · PostgreSQL · ASP.NET Core Identity + JWT · Serilog · xUnit + Testcontainers · Docker · GitHub Actions.

> **Δομή του plan:** Setup + Φάση 1 = πλήρως αναλυτικά (ξεκίνα από εδώ). Φάσεις 2–8 = outline· κάθε μία θα επεκταθεί σε αναλυτικά bite-sized tasks όταν φτάσουμε εκεί (τα paths/APIs θα είναι τότε γνωστά). Spec: [docs/specs/2026-06-28-gym-booking-design.md](../specs/2026-06-28-gym-booking-design.md).

---

## Prerequisites (πριν το Task 1)

- [ ] **Node.js LTS** (20+): `node -v`
- [ ] **.NET 8 SDK**: `dotnet --version` (8.x)
- [ ] **Docker Desktop** (τρέχει): `docker --version`
- [ ] **EF Core CLI tools**: `dotnet tool install --global dotnet-ef`
- [ ] **GitHub account + repo** (private) έτοιμο για push
- [ ] Naming convention (placeholder σε αυτό το plan): Nx workspace `gym-booking`, .NET solution `GymBooking`. Άλλαξέ τα αν θες — απλώς κράτα συνέπεια.

---

## File Structure (στόχος μετά το Setup)

```
/gym-booking/                 # Nx workspace (frontend)
  apps/
    customer/                 # mobile-first Angular app (μέλη)
    staff/                    # desktop-first Angular app (instructors+admins)
  libs/
    models/                   # DTOs/types (scope:shared, type:util)
    data-access/              # API services (scope:shared, type:data-access)
    auth/                     # login, JWT interceptor, guards (scope:shared, type:feature)
    ui/                       # κοινά components (scope:shared, type:ui)
/backend/
  GymBooking.sln
  src/
    GymBooking.Api/           # Controllers, Program.cs, DI, auth, Serilog, Swagger
    GymBooking.Core/          # entities, services (business logic), interfaces
    GymBooking.Data/          # EF Core: AppDbContext, configurations, migrations
  tests/
    GymBooking.Tests/         # xUnit (+ Testcontainers)
/docker/
  docker-compose.yml          # Postgres + Papercut
/.github/workflows/
  ci.yml
```

> Σημ.: frontend & backend σε χωριστά roots για καθαρότητα. Εναλλακτικά όλα κάτω από ένα root — δική σου επιλογή· το plan υποθέτει το παραπάνω.

---

## PHASE: Setup (~14h)

### Task 1: Nx workspace + 2 apps

**Files:** Create `gym-booking/` (Nx workspace)

- [ ] **Step 1:** Δημιούργησε Nx workspace (apps preset, χωρίς αρχικό app):
```bash
npx create-nx-workspace@latest gym-booking --preset=apps --packageManager=npm --nxCloud=skip
cd gym-booking
npm i -D @nx/angular
```
- [ ] **Step 2:** Δημιούργησε τα δύο apps:
```bash
npx nx g @nx/angular:application customer --style=scss --routing=true --standalone=true
npx nx g @nx/angular:application staff   --style=scss --routing=true --standalone=true
```
- [ ] **Step 3:** Επιβεβαίωσε ότι σηκώνονται:
```bash
npx nx serve customer   # http://localhost:4200
npx nx serve staff      # δεύτερο port
```
Expected: και τα δύο φορτώνουν την default Angular σελίδα.
- [ ] **Step 4: Commit**
```bash
git add -A && git commit -m "chore: scaffold Nx workspace with customer and staff apps"
```

### Task 2: Shared libraries + module boundaries

**Files:** Create `libs/models`, `libs/data-access`, `libs/auth`, `libs/ui`

- [ ] **Step 1:** Δημιούργησε τις libs με tags:
```bash
npx nx g @nx/angular:library models      --tags="scope:shared,type:util"
npx nx g @nx/angular:library data-access  --tags="scope:shared,type:data-access"
npx nx g @nx/angular:library auth         --tags="scope:shared,type:feature"
npx nx g @nx/angular:library ui           --tags="scope:shared,type:ui"
```
- [ ] **Step 2:** Πρόσθεσε tags στα apps: στο `apps/customer/project.json` και `apps/staff/project.json` βάλε `"tags": ["scope:customer"]` / `["scope:staff"]`.
- [ ] **Step 3:** Ρύθμισε enforce-module-boundaries στο root `.eslintrc.json` (rule `@nx/enforce-module-boundaries`) με `depConstraints`:
```jsonc
{
  "sourceTag": "scope:customer", "onlyDependOnLibsWithTags": ["scope:customer", "scope:shared"]
},
{
  "sourceTag": "scope:staff", "onlyDependOnLibsWithTags": ["scope:staff", "scope:shared"]
},
{ "sourceTag": "type:feature", "onlyDependOnLibsWithTags": ["type:feature","type:ui","type:data-access","type:util"] },
{ "sourceTag": "type:ui",      "onlyDependOnLibsWithTags": ["type:ui","type:util"] },
{ "sourceTag": "type:data-access","onlyDependOnLibsWithTags": ["type:data-access","type:util"] }
```
- [ ] **Step 4:** Επιβεβαίωσε boundaries: βάλε προσωρινά ένα import staff→customer και τρέξε `npx nx lint staff`. Expected: lint **error** (μπλοκάρεται). Αφαίρεσέ το.
- [ ] **Step 5: Commit** `git commit -am "chore: add shared libs with enforced module boundaries"`

### Task 3: Tailwind + Angular Material

**Files:** Modify `apps/customer`, `apps/staff` (styles, config)

- [ ] **Step 1:** Material σε κάθε app:
```bash
npx nx g @angular/material:ng-add --project=customer
npx nx g @angular/material:ng-add --project=staff
```
- [ ] **Step 2:** Tailwind σε κάθε app (Nx generator):
```bash
npx nx g @nx/angular:setup-tailwind customer
npx nx g @nx/angular:setup-tailwind staff
```
- [ ] **Step 3:** Δοκιμή: βάλε `<button mat-raised-button class="m-4">Test</button>` σε ένα component, `npx nx serve customer`. Expected: Material κουμπί με Tailwind margin.
- [ ] **Step 4: Commit** `git commit -am "chore: add Material + Tailwind to both apps"`

### Task 4: .NET solution (layered)

**Files:** Create `backend/GymBooking.sln`, `GymBooking.Api`, `GymBooking.Core`, `GymBooking.Data`, `GymBooking.Tests`

- [ ] **Step 1:** Δημιούργησε solution & projects:
```bash
mkdir backend && cd backend
dotnet new sln -n GymBooking
dotnet new webapi   -o src/GymBooking.Api      --use-controllers
dotnet new classlib -o src/GymBooking.Core
dotnet new classlib -o src/GymBooking.Data
dotnet new xunit    -o tests/GymBooking.Tests
dotnet sln add src/GymBooking.Api src/GymBooking.Core src/GymBooking.Data tests/GymBooking.Tests
```
- [ ] **Step 2:** Project references: Api→Core,Data· Data→Core· Tests→Api,Core,Data:
```bash
dotnet add src/GymBooking.Api reference src/GymBooking.Core src/GymBooking.Data
dotnet add src/GymBooking.Data reference src/GymBooking.Core
dotnet add tests/GymBooking.Tests reference src/GymBooking.Api src/GymBooking.Core src/GymBooking.Data
```
- [ ] **Step 3:** `dotnet build`. Expected: build succeeded.
- [ ] **Step 4: Commit** `git commit -am "chore: scaffold .NET layered solution"`

### Task 5: EF Core + PostgreSQL + AppDbContext

**Files:** Create `src/GymBooking.Data/AppDbContext.cs`; Modify `GymBooking.Api/Program.cs`, `appsettings.Development.json`

- [ ] **Step 1:** Πακέτα:
```bash
dotnet add src/GymBooking.Data package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/GymBooking.Data package Microsoft.EntityFrameworkCore.Design
dotnet add src/GymBooking.Api  package Microsoft.EntityFrameworkCore.Design
```
- [ ] **Step 2:** Άδειο `AppDbContext : DbContext` στο `GymBooking.Data` (entities έρχονται στη Φάση 1).
- [ ] **Step 3:** Register στο `Program.cs`:
```csharp
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
```
- [ ] **Step 4:** Connection string στο `appsettings.Development.json` (δείχνει στο compose Postgres του Task 6).
- [ ] **Step 5:** `dotnet build`. Commit `git commit -am "chore: wire EF Core + Npgsql"`

### Task 6: Docker compose (Postgres + Papercut)

**Files:** Create `docker/docker-compose.yml`

- [ ] **Step 1:** `docker-compose.yml` με services: `postgres` (port 5432, volume, env POSTGRES_USER/PASSWORD/DB) + `papercut` (SMTP 25 + UI 37408).
- [ ] **Step 2:** `docker compose -f docker/docker-compose.yml up -d`. Expected: 2 containers running (`docker ps`).
- [ ] **Step 3:** Επιβεβαίωσε σύνδεση από το API (επόμενο migration στη Φάση 1 θα το δοκιμάσει σε πραγματικά).
- [ ] **Step 4: Commit** `git commit -am "chore: add docker-compose for Postgres + Papercut"`

### Task 7: Serilog + Swagger + Health check

**Files:** Modify `Program.cs`; package adds

- [ ] **Step 1:** `dotnet add src/GymBooking.Api package Serilog.AspNetCore`. Ρύθμισε Serilog (console sink) στο `Program.cs`. Πρόσθεσε enricher για `TenantId` αργότερα (Φάση 1).
- [ ] **Step 2:** Swagger ήδη έρχεται με το webapi template — επιβεβαίωσε `/swagger` ανοίγει.
- [ ] **Step 3:** `dotnet add ... AspNetCore.HealthChecks` και `app.MapHealthChecks("/health")`. Verify `/health` → Healthy.
- [ ] **Step 4: Commit** `git commit -am "chore: add Serilog, Swagger, health check"`

### Task 8: Git + GitHub Actions CI skeleton

**Files:** Create `.github/workflows/ci.yml`, `.gitignore`

- [ ] **Step 1:** `.gitignore` (node_modules, bin, obj, dist, .env). Push σε GitHub private repo.
- [ ] **Step 2:** `ci.yml`: jobs `backend` (`dotnet build` + `dotnet test`) και `frontend` (`npm ci` + `npx nx affected -t lint test build` ή full build), trigger σε push + PR.
- [ ] **Step 3:** Push → επιβεβαίωσε ✅ στο Actions tab.
- [ ] **Step 4:** Ενεργοποίησε **branch protection** στο `main` (require CI pass).
- [ ] **Step 5: Commit/push** `git commit -am "ci: add GitHub Actions build+test pipeline"`

### Task 9: Ενημέρωση CLAUDE.md

- [ ] Συμπλήρωσε στο `CLAUDE.md`: πραγματικές εντολές (serve/build/test, `docker compose up`, migrations), δομή repo, και τσέκαρε `[x] Setup`.
- [ ] Commit.

---

## PHASE 1: Auth + ρόλοι + invitation/registration (~12h)

> TDD για tenant isolation & auth logic. Scaffolding (Identity wiring) = pragmatic steps.

### Αποφάσεις Φάσης 1 (κλειδωμένες)
- **Authorization = named policies πάνω από roles** _(2026-07-04)_. Ορίζουμε policies (`RequireAdmin`, `RequireInstructor`) στο `Program.cs` και χρησιμοποιούμε `[Authorize(Policy=...)]` — όχι σκόρπια `[Authorize(Roles="...")]` strings. Λόγοι: καθαρότερο, future-proof (πρόσθετα requirements χωρίς αλλαγή controllers).
  - **Resource ownership** ("ακυρώνεις μόνο τη δική σου κράτηση") = ρητός έλεγχος στο **service layer** (`entity.UserId == currentUserId`), **όχι** custom `AuthorizationHandler` ακόμα.
  - **Tenant isolation** = EF global query filters + tenant middleware (ορθογώνιο, όχι authorization policy).
  - **Αναβάθμιση** σε custom requirements/handlers μόνο αν εμφανιστεί κανόνας πέρα από «ρόλος + ownership» (granular permissions, gating βάσει subscription status, feature flags).
- **Invitation & registration flow = ενιαίος μηχανισμός για όλους** _(2026-07-04)_:
  - **Invite-only**, κανένα self-signup. Ο **Admin** είναι seeded (Task 14), όχι invited.
  - Ο user **δεν** δημιουργείται στο invite — μόνο **Invitation** record. Δημιουργείται **Active** στην εγγραφή.
  - **Ενιαία πρόσκληση** και για members (role=User) και για instructors (role=Instructor) μέσω του ίδιου `POST /invitations`. **Μία role-agnostic register σελίδα** για όλους· ο ρόλος καθορίζει μόνο σε ποιο app γίνεται login μετά (User→customer, Instructor→staff). Κανείς δεν χειρίζεται passwords χειροκίνητα.
  - Register πεδία: **password + FirstName/LastName** (προστίθενται στον `ApplicationUser`).
  - Token: τυχαίο 32 bytes, αποθηκευμένο **hashed**, **7 μέρες** λήξη, **μιας χρήσης**· raw token μόνο στο email link. Dev email → Papercut. Μετά την εγγραφή → redirect σε login.

### Task 10: Base entities + Identity

**Files:** Create entities σε `GymBooking.Core` (Tenant, ApplicationUser, Role, Invitation); Modify `AppDbContext`

- [ ] **Step 1:** Entities: `Tenant` (Id, Name, Slug, CancellationHours, IsActive), `ApplicationUser : IdentityUser<Guid>` (+ TenantId, Status, IsActive), `Invitation` (Id, TenantId, Email, Role, TokenHash, ExpiresAt, UsedAt). Roles ως const strings: User/Instructor/Admin.
- [ ] **Step 2:** Πακέτα Identity:
```bash
dotnet add src/GymBooking.Api package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/GymBooking.Api package Microsoft.AspNetCore.Authentication.JwtBearer
```
- [ ] **Step 3:** `AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` + DbSet<Tenant>, DbSet<Invitation>.
- [ ] **Step 4:** Migration + apply:
```bash
dotnet ef migrations add InitialIdentity -p src/GymBooking.Data -s src/GymBooking.Api
dotnet ef database update -p src/GymBooking.Data -s src/GymBooking.Api
```
Expected: πίνακες δημιουργούνται στο Postgres (έλεγξε με ψit/pgAdmin ή `docker exec`).
- [ ] **Step 5: Commit**

### Task 11: Tenant infrastructure (TDD — η καρδιά της ασφάλειας)

**Files:** Create `ICurrentTenant`, `CurrentTenant`, tenant middleware; Modify `AppDbContext` (global query filters); Test `GymBooking.Tests/TenantIsolationTests.cs`

- [ ] **Step 1 (test first):** Γράψε test: δύο tenants, δύο εγγραφές tenant-owned· με `CurrentTenant=A`, query επιστρέφει **μόνο** της A.
- [ ] **Step 2:** Τρέξε → FAIL.
- [ ] **Step 3:** `ICurrentTenant { Guid TenantId }` + scoped impl. Στο `AppDbContext.OnModelCreating`, `HasQueryFilter(e => e.TenantId == _currentTenant.TenantId)` σε κάθε tenant-owned entity.
- [ ] **Step 4:** Τρέξε → PASS.
- [ ] **Step 5:** Middleware/handler που γεμίζει το `CurrentTenant` από το JWT claim `tenantId` (στη Φάση μετά το login). Commit.

### Task 12: JWT issuance + login

**Files:** Create `AuthController`, `TokenService`; Modify `Program.cs` (JwtBearer)

- [ ] **Step 1:** `TokenService.CreateAccessToken(user, roles, tenantId)` → JWT με claims `sub/userId`, `tenantId`, `role(s)`, exp ~2h. Signing key από config/secrets.
- [ ] **Step 2:** JwtBearer auth στο `Program.cs` (validate issuer/audience/lifetime/signature).
- [ ] **Step 3:** `POST /auth/login` (email+password → Identity `CheckPasswordSignInAsync` → token). Test: σωστά creds → 200+token· λάθος → 401.
- [ ] **Step 4:** `[Authorize]` + `[Authorize(Roles="Admin")]` smoke test endpoint.
- [ ] **Step 5: Commit**

### Task 13: Invitation + register flow

**Files:** Create `InvitationService`, `IEmailSender` + dev impl, endpoints; Test invitation logic

- [ ] **Step 1 (test):** create invitation → token· register με valid token → user Active με σωστό role/tenant· expired/used token → απορρίπτεται.
- [ ] **Step 2:** Impl: `POST /invitations` (Admin) → random token, αποθήκευση **hashed** + expiry → `IEmailSender` (dev: γράφει στο Papercut/log το link).
- [ ] **Step 3:** `POST /auth/register?token=...` → επαλήθευση token → set password → activate → mark used.
- [ ] **Step 4:** Tests PASS. Commit.

### Task 14: Seed (1 tenant + 1 admin)

**Files:** Create `DbSeeder`; Modify `Program.cs`

- [ ] **Step 1:** Seeder: αν άδεια βάση → 1 Tenant + roles + 1 Admin user (από config creds).
- [ ] **Step 2:** Τρέξε API → login με τον admin → 200. Commit.

### Task 15: Shared `auth` lib (frontend)

**Files:** `libs/auth` — login, token storage, JWT interceptor, guards

- [ ] **Step 1:** `AuthService` (Signals για `user`/`isAuthenticated`), `login()` καλεί `/auth/login`, token σε `localStorage` (Φάση 1).
- [ ] **Step 2:** `jwtInterceptor` (βάζει `Authorization: Bearer`), `authGuard`, `roleGuard`.
- [ ] **Step 3:** Unit test interceptor + guard (FE).
- [ ] **Step 4: Commit**

### Task 16: Login UI (customer + staff)

**Files:** `apps/customer` login route, `apps/staff` login route (χρησιμοποιούν τη shared `auth` lib)

- [ ] **Step 1:** Login page component (Material form) στη `ui`/`auth` lib, reused και στα δύο apps.
- [ ] **Step 2:** Register-via-invite page (διαβάζει `?token=`).
- [ ] **Step 3:** E2E smoke: login στο staff → redirect σε protected route· logout καθαρίζει token.
- [ ] **Step 4: Commit + ενημέρωση CLAUDE.md (`[x] Φ1`)**

---

## PHASE 2–8: Outline (επέκταση σε αναλυτικά tasks όταν φτάνουμε)

> Κάθε φάση = δικό της σετ bite-sized TDD tasks, που θα γραφτούν όταν ξεκινά (τότε τα paths/DTOs υπάρχουν). Εδώ μόνο το περίγραμμα + τα tricky σημεία.

### Phase 2 — ClassType/ClassSession CRUD + instructor (~12h)
- [ ] Entities `ClassType`, `ClassSession` (+ migrations, soft-delete flags, UTC dates).
- [ ] Core services + Instructor endpoints (CRUD, one-off sessions, ορισμός capacity).
- [ ] `staff` app: instructor σελίδες (λίστα/δημιουργία sessions, λίστα συμμετεχόντων).
- [ ] Tests: authorization (μόνο instructor/admin), tenant isolation στα νέα entities.

### Phase 3 — 🔥 Booking core (atomic) + tests (~14h)
- [ ] `Booking`, `WaitlistEntry` entities + migrations.
- [ ] **Atomic booking** (TDD, κρίσιμο): transaction + `SELECT … FOR UPDATE` (raw SQL **με parameters**) → check `bookedCount < capacity` → create booking → increment. Concurrency test (παράλληλες κρατήσεις → no overbooking).
- [ ] Anti-double-booking (ίδιο session ή overlapping χρόνος).
- [ ] Cancel + (επιστροφή θέσης). 
- [ ] `customer` app: book/cancel UI πάνω σε session.

### Phase 4 — Weekly schedule + φίλτρα + customer dashboard (~12h)
- [ ] `GET /sessions?week=...&filters` (τύπος/instructor/μέρα).
- [ ] `customer` app: εβδομαδιαίο πρόγραμμα (mobile-first), ένδειξη διαθεσιμότητας, φίλτρα.
- [ ] Dashboard: επερχόμενες + ιστορικό κρατήσεων.

### Phase 5 — Subscriptions/plans + consumption (~12h)
- [ ] `MembershipPlan` (Type SessionPack|Unlimited), `Subscription` (+ migrations).
- [ ] Booking consumption: SessionPack → decrement remaining (μέσα στο atomic transaction)· Unlimited → έλεγχος validFrom/validTo. Cancel → refund SessionPack αν εντός policy.
- [ ] `staff` app (admin): δημιουργία plans + χειροκίνητη ανάθεση συνδρομής σε χρήστη.
- [ ] `customer` app: προβολή υπολοίπου/ισχύος.

### Phase 6 — Waitlist + cancellation policy (~10h)
- [ ] Join waitlist όταν γεμάτο· auto-promote #1 σε cancel (μέσα σε transaction).
- [ ] Cancellation policy: block ακύρωσης < `Tenant.CancellationHours` πριν.
- [ ] (Προαιρετικά SHOULD: recurring template generation· background job για auto-promote/expiry — Hangfire/Quartz.)

### Phase 7 — Admin/staff dashboard + responsive QA (~12h)
- [ ] `staff` app admin: διαχείριση χρηστών/ρόλων, εποπτεία κρατήσεων, tenant settings.
- [ ] Responsive QA customer (mobile) + staff (desktop).
- [ ] (SHOULD: SignalR live availability — hub `SpotsUpdated` + Signals στο customer· emit από το single point του booking service.)

### Phase 8 — Hardening + security + deployment (~12h)
- [ ] Security pass: OWASP checklist, resource-ownership checks παντού, .NET analyzers, (SHOULD: rate limiting/lockout, refresh token rotation, httpOnly cookie).
- [ ] Seed/demo data για παρουσίαση (2 tenants → δείξε isolation).
- [ ] Dockerize: `Dockerfile` Api + `Dockerfile` (nginx) Angular + compose. CD job (build image → ghcr.io → SSH deploy, manual approval).
- [ ] (SHOULD: PWA — πρώτο που κόβεται.)
- [ ] ER diagram + architecture diagram (`nx graph`) για το κείμενο πτυχιακής.

---

## Self-review (έγινε)
- **Spec coverage:** όλα τα MUST έχουν task (auth/ρόλοι/multi-tenant Φ1· CRUD Φ2· atomic booking Φ3· schedule/dashboard Φ4· cancel/anti-double Φ3· responsive Φ4/7). SHOULD αναφέρονται στις σχετικές φάσεις ως προαιρετικά. WON'T εκτός.
- **Placeholders:** Setup+Φ1 πλήρη με εντολές/κώδικα. Φ2–8 σκόπιμα outline (progressive elaboration για μεγάλο project) — όχι placeholders προς υλοποίηση τώρα.
- **Συνέπεια ονομάτων:** `ICurrentTenant`, `AppDbContext`, `TokenService`, `InvitationService`, entities Tenant/ApplicationUser/Invitation/ClassType/ClassSession/Booking/WaitlistEntry/MembershipPlan/Subscription — σταθερά σε όλο το plan & spec.
