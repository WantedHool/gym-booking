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
- **Seed values** _(2026-07-04)_: Tenant `Demo Gym` / slug `demo-gym` / CancellationHours `24`· Admin `admin@demo.gym` / `Admin123!`· ρόλοι User/Instructor/Admin. Τιμές σε **`appsettings.Development.json` → `Seed`** (όχι hardcoded), τρέχει μόνο σε άδεια βάση. Identity default password policy. Πραγματικό branding (όνομα/logo) = μελλοντικό cosmetic πέρασμα.
- **JWT storage (FE) = `localStorage`** _(2026-07-04)_: access-token-only για τη Φ1 (απλό, επιβιώνει refresh). Αποδεκτό λόγω short-lived token (~1-2h) + Angular auto-sanitize. Φ2 σκληραίνει: access σε μνήμη + refresh σε httpOnly cookie.

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

## PHASE 2: ClassType/ClassSession CRUD + instructor (~12h)

> TDD για tenant isolation & authorization των νέων entities. Scaffolding (entities/DTOs/DI wiring) = pragmatic steps. Ακολουθεί ακριβώς τα Φ1 patterns: flat entities (μόνο FK Guids, χωρίς navigation props), `AppDbContext` global query filters ανά tenant-owned entity, services στο `GymBooking.Api/Services`, contracts (records) στο `GymBooking.Core/Contracts`, integration tests μέσω `TestApiFactory` (InMemory).

### Αποφάσεις Φάσης 2 (κλειδωμένες)
- **ClassType CRUD = Instructor + Admin** _(2026-07-09)_. Ο instructor «διαχειρίζεται μαθήματα & ώρες» (spec). Και οι δύο ρόλοι δημιουργούν/επεξεργάζονται είδη μαθημάτων.
- **Session `InstructorId`: Instructor → πάντα ο εαυτός του· Admin → οποιονδήποτε** _(2026-07-09)_. Ο controller διαβάζει το `sub` claim· αν ο caller είναι Admin **και** έδωσε `InstructorId`, χρησιμοποιείται αυτό· αλλιώς ο τρέχων χρήστης. Απλό & ασφαλές (ο instructor δεν φτιάχνει sessions για άλλον).
- **Authorization = named policies** _(2026-07-09)_. Υλοποιούμε επιτέλους την κλειδωμένη απόφαση Φ1: `Policies.RequireAdmin` (μόνο Admin) + `Policies.RequireInstructor` (Instructor **ή** Admin) στο `Program.cs`. Refactor του υπάρχοντος `InvitationsController` από `[Authorize(Roles=...)]` σε `[Authorize(Policy=...)]`.
- **Λίστα συμμετεχόντων = αναβολή στη Φ3** _(2026-07-09)_. Το `Booking` entity μπαίνει στη Φ3· εδώ φτιάχνουμε **μόνο** ClassType/ClassSession CRUD. Το `BookedCount` υπάρχει στο entity (default 0) αλλά **δεν** το πειράζει κανείς ακόμα — θα το αυξάνει το atomic booking της Φ3 (single point).
- **Soft delete** _(2026-07-09)_: `IsActive` flag· τα list endpoints επιστρέφουν μόνο `IsActive == true`. Το DELETE κάνει `IsActive = false` (όχι πραγματική διαγραφή). Sessions έχουν επιπλέον `IsCancelled` (ακύρωση από instructor, ξεχωριστό από soft-delete). Dates **UTC**.

### Task 17: ClassType + ClassSession entities + tenant isolation + migration

**Files:**
- Create: `backend/src/GymBooking.Core/Entities/Models/ClassType.cs`
- Create: `backend/src/GymBooking.Core/Entities/Models/ClassSession.cs`
- Modify: `backend/src/GymBooking.Data/AppDbContext.cs`
- Test: `backend/tests/GymBooking.Tests/TenantIsolationTests.cs`

- [ ] **Step 1:** Δημιούργησε `ClassType.cs` (flat entity, ίδιο style με `Tenant`):

```csharp
namespace GymBooking.Core.Entities.Models;

public class ClassType
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DefaultDurationMinutes { get; set; }
    public int DefaultCapacity { get; set; }
    public bool IsActive { get; set; } = true;
}
```

- [ ] **Step 2:** Δημιούργησε `ClassSession.cs`:

```csharp
namespace GymBooking.Core.Entities.Models;

public class ClassSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClassTypeId { get; set; }
    public Guid InstructorId { get; set; }
    public DateTime StartsAt { get; set; }        // UTC
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }          // αρχικά 0· θα το αυξάνει το booking της Φ3
    public bool IsCancelled { get; set; }
    public bool IsActive { get; set; } = true;
}
```

- [ ] **Step 3:** Στο `AppDbContext.cs` πρόσθεσε DbSets (μετά το `Invitations`) και query filters (μέσα στο `OnModelCreating`, μετά το filter του `Invitation`):

```csharp
    public DbSet<ClassType> ClassTypes => Set<ClassType>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
```

```csharp
        modelBuilder.Entity<ClassType>().HasQueryFilter(c => c.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<ClassSession>().HasQueryFilter(s => s.TenantId == _currentTenant.TenantId);
```

- [ ] **Step 4 (test first):** Πρόσθεσε στο `TenantIsolationTests.cs` ένα test (ίδιο μοτίβο με το `Invitations_query_returns_only_current_tenant_rows`):

```csharp
    [Fact]
    public void ClassSessions_query_returns_only_current_tenant_rows()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var seedContext = CreateContext(dbName, new FakeCurrentTenant(tenantA)))
        {
            seedContext.ClassSessions.Add(new ClassSession
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                ClassTypeId = Guid.NewGuid(),
                InstructorId = Guid.NewGuid(),
                StartsAt = DateTime.UtcNow.AddDays(1),
                DurationMinutes = 60,
                Capacity = 10,
            });
            seedContext.ClassSessions.Add(new ClassSession
            {
                Id = Guid.NewGuid(),
                TenantId = tenantB,
                ClassTypeId = Guid.NewGuid(),
                InstructorId = Guid.NewGuid(),
                StartsAt = DateTime.UtcNow.AddDays(1),
                DurationMinutes = 45,
                Capacity = 8,
            });
            seedContext.SaveChanges();
        }

        using var queryContext = CreateContext(dbName, new FakeCurrentTenant(tenantA));
        var results = queryContext.ClassSessions.ToList();

        Assert.Single(results);
        Assert.Equal(tenantA, results[0].TenantId);
    }
```

- [ ] **Step 5:** Τρέξε → PASS:

Run: `cd backend && dotnet test --filter "FullyQualifiedName~TenantIsolationTests"`
Expected: PASS (και τα 3 tests).

- [ ] **Step 6:** Migration + apply (η DB πρέπει να τρέχει: `docker compose -f docker/docker-compose.yml up -d`):

```bash
cd backend
dotnet ef migrations add AddClassTypesAndSessions -p src/GymBooking.Data -s src/GymBooking.Api
dotnet ef database update -p src/GymBooking.Data -s src/GymBooking.Api
```
Expected: πίνακες `ClassTypes` + `ClassSessions` στο Postgres.

- [ ] **Step 7: Commit** `git add -A && git commit -m "feat: add ClassType and ClassSession entities + tenant isolation"`

### Task 18: Named authorization policies + refactor

**Files:**
- Create: `backend/src/GymBooking.Core/Entities/Constants/Policies.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs:96`
- Modify: `backend/src/GymBooking.Api/Controllers/InvitationsController.cs`

- [ ] **Step 1:** Δημιούργησε `Policies.cs` (ίδιο style με `Roles.cs`):

```csharp
namespace GymBooking.Core.Entities.Constants;

public static class Policies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireInstructor = "RequireInstructor";
}
```

- [ ] **Step 2:** Στο `Program.cs`, αντικατέστησε τη γραμμή `builder.Services.AddAuthorization();` με:

```csharp
builder.Services.AddAuthorization(options =>
{
    // RequireInstructor = Instructor Ή Admin (ο Admin μπορεί ό,τι κι ο instructor).
    options.AddPolicy(Policies.RequireAdmin, policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.RequireInstructor, policy => policy.RequireRole(Roles.Instructor, Roles.Admin));
});
```
Πρόσθεσε στα usings του `Program.cs`: `using GymBooking.Core.Entities.Constants;`

- [ ] **Step 3:** Στο `InvitationsController.cs` άλλαξε το attribute της `Create` από `[Authorize(Roles = Roles.Admin)]` σε `[Authorize(Policy = Policies.RequireAdmin)]` (κράτα το `using GymBooking.Core.Entities.Constants;` — ήδη υπάρχει).

- [ ] **Step 4:** Τρέξε ΟΛΑ τα υπάρχοντα tests — δεν πρέπει να σπάσει τίποτα (το `InvitationsTests.CreateInvitation_as_non_admin_returns_403` επικυρώνει το refactor):

Run: `cd backend && dotnet test`
Expected: PASS (όλα).

- [ ] **Step 5: Commit** `git commit -am "refactor: authorization via named policies (RequireAdmin/RequireInstructor)"`

### Task 19: ClassType CRUD (service + controller + tests)

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/ClassTypeContracts.cs`
- Create: `backend/src/GymBooking.Api/Services/ClassTypeService.cs`
- Create: `backend/src/GymBooking.Api/Controllers/ClassTypesController.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs` (DI registration)
- Test: `backend/tests/GymBooking.Tests/ClassTypesTests.cs`

- [ ] **Step 1:** Contracts (records, ένα αρχείο):

```csharp
namespace GymBooking.Core.Contracts;

public record CreateClassTypeRequest(string Name, string Description, int DefaultDurationMinutes, int DefaultCapacity);
public record UpdateClassTypeRequest(string Name, string Description, int DefaultDurationMinutes, int DefaultCapacity);
public record ClassTypeResponse(Guid Id, string Name, string Description, int DefaultDurationMinutes, int DefaultCapacity, bool IsActive);
```

- [ ] **Step 2:** `ClassTypeService.cs` (ίδιο DI style με `InvitationService`· TenantId μπαίνει χειροκίνητα στο create, τα reads τα φιλτράρει το query filter):

```csharp
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class ClassTypeService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public ClassTypeService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<ClassType> CreateAsync(string name, string description, int defaultDurationMinutes, int defaultCapacity)
    {
        var classType = new ClassType
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            Name = name,
            Description = description,
            DefaultDurationMinutes = defaultDurationMinutes,
            DefaultCapacity = defaultCapacity,
            IsActive = true,
        };
        _dbContext.ClassTypes.Add(classType);
        await _dbContext.SaveChangesAsync();
        return classType;
    }

    public async Task<List<ClassType>> GetAllAsync() =>
        await _dbContext.ClassTypes.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

    public async Task<ClassType?> GetByIdAsync(Guid id) =>
        await _dbContext.ClassTypes.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<bool> UpdateAsync(Guid id, string name, string description, int defaultDurationMinutes, int defaultCapacity)
    {
        var classType = await _dbContext.ClassTypes.FirstOrDefaultAsync(c => c.Id == id);
        if (classType is null)
        {
            return false;
        }

        classType.Name = name;
        classType.Description = description;
        classType.DefaultDurationMinutes = defaultDurationMinutes;
        classType.DefaultCapacity = defaultCapacity;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var classType = await _dbContext.ClassTypes.FirstOrDefaultAsync(c => c.Id == id);
        if (classType is null)
        {
            return false;
        }

        classType.IsActive = false;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
```

- [ ] **Step 3:** `ClassTypesController.cs` (όλο το controller πίσω από `RequireInstructor`):

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("class-types")]
[Authorize(Policy = Policies.RequireInstructor)]
public class ClassTypesController : ControllerBase
{
    private readonly ClassTypeService _service;

    public ClassTypesController(ClassTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassTypeResponse>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClassTypeResponse>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ClassTypeResponse>> Create([FromBody] CreateClassTypeRequest request)
    {
        var created = await _service.CreateAsync(request.Name, request.Description, request.DefaultDurationMinutes, request.DefaultCapacity);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassTypeRequest request)
    {
        var updated = await _service.UpdateAsync(id, request.Name, request.Description, request.DefaultDurationMinutes, request.DefaultCapacity);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeactivateAsync(id);
        return ok ? NoContent() : NotFound();
    }

    private static ClassTypeResponse ToResponse(ClassType c) =>
        new(c.Id, c.Name, c.Description, c.DefaultDurationMinutes, c.DefaultCapacity, c.IsActive);
}
```

- [ ] **Step 4:** DI στο `Program.cs` (κοντά στο `AddScoped<InvitationService>()`):

```csharp
builder.Services.AddScoped<ClassTypeService>();
```

- [ ] **Step 5 (tests):** `ClassTypesTests.cs`. Επαναχρησιμοποίησε τα helpers (`CreateUserAsync`, `LoginAsync`) — αντέγραψέ τα από το `InvitationsTests.cs` (ο executor δεν διαβάζει άλλα αρχεία). Τεστ: (α) User (μη-instructor) → 403· (β) instructor create → 201 + roundtrip στο GetAll:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class ClassTypesTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ClassTypesTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string password, params string[] roles)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        foreach (var role in roles)
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task CreateClassType_as_plain_user_returns_403()
    {
        await CreateUserAsync("ct-user@demo.gym", "Test1234!", Roles.User);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "ct-user@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("Yoga", "Χαλαρωτικό", 60, 12));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateClassType_as_instructor_then_appears_in_list()
    {
        await CreateUserAsync("ct-instr@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "ct-instr@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("Pilates", "Core", 45, 10));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var list = await client.GetFromJsonAsync<List<ClassTypeResponse>>("/class-types");
        Assert.Contains(list!, c => c.Name == "Pilates" && c.DefaultCapacity == 10);
    }
}
```

- [ ] **Step 6:** Τρέξε → PASS:

Run: `cd backend && dotnet test --filter "FullyQualifiedName~ClassTypesTests"`
Expected: PASS (2 tests).

- [ ] **Step 7: Commit** `git commit -am "feat: ClassType CRUD (instructor/admin) + tests"`

### Task 20: ClassSession CRUD (service + controller + tests)

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/ClassSessionContracts.cs`
- Create: `backend/src/GymBooking.Api/Services/ClassSessionService.cs`
- Create: `backend/src/GymBooking.Api/Controllers/ClassSessionsController.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs` (DI registration)
- Test: `backend/tests/GymBooking.Tests/ClassSessionsTests.cs`

- [ ] **Step 1:** Contracts (`InstructorId` nullable — μόνο ο Admin το χρησιμοποιεί):

```csharp
namespace GymBooking.Core.Contracts;

public record CreateClassSessionRequest(Guid ClassTypeId, Guid? InstructorId, DateTime StartsAt, int DurationMinutes, int Capacity);
public record ClassSessionResponse(
    Guid Id,
    Guid ClassTypeId,
    string ClassTypeName,
    Guid InstructorId,
    string InstructorName,
    DateTime StartsAt,
    int DurationMinutes,
    int Capacity,
    int BookedCount,
    bool IsCancelled);
```

- [ ] **Step 2:** `ClassSessionService.cs`. Το `GetAllAsync` κάνει join σε `ClassTypes` + `Users` για τα ονόματα· και τα τρία tables φιλτράρονται ανά tenant από τα query filters, άρα δεν χρειάζεται explicit tenant check:

```csharp
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class ClassSessionService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public ClassSessionService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    /// <summary>Επιστρέφει null αν το ClassType δεν υπάρχει/δεν είναι ενεργό (ίδιο tenant).</summary>
    public async Task<Guid?> CreateAsync(Guid classTypeId, Guid instructorId, DateTime startsAtUtc, int durationMinutes, int capacity)
    {
        var classTypeExists = await _dbContext.ClassTypes.AnyAsync(c => c.Id == classTypeId && c.IsActive);
        if (!classTypeExists)
        {
            return null;
        }

        var session = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            ClassTypeId = classTypeId,
            InstructorId = instructorId,
            StartsAt = startsAtUtc,
            DurationMinutes = durationMinutes,
            Capacity = capacity,
            BookedCount = 0,
            IsCancelled = false,
            IsActive = true,
        };
        _dbContext.ClassSessions.Add(session);
        await _dbContext.SaveChangesAsync();
        return session.Id;
    }

    public async Task<List<ClassSessionResponse>> GetAllAsync()
    {
        return await (
            from s in _dbContext.ClassSessions
            where s.IsActive
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            join u in _dbContext.Users on s.InstructorId equals u.Id
            orderby s.StartsAt
            select new ClassSessionResponse(
                s.Id,
                s.ClassTypeId,
                ct.Name,
                s.InstructorId,
                u.FirstName + " " + u.LastName,
                s.StartsAt,
                s.DurationMinutes,
                s.Capacity,
                s.BookedCount,
                s.IsCancelled))
            .ToListAsync();
    }

    public async Task<ClassSessionResponse?> GetByIdAsync(Guid id)
    {
        return await (
            from s in _dbContext.ClassSessions
            where s.Id == id
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            join u in _dbContext.Users on s.InstructorId equals u.Id
            select new ClassSessionResponse(
                s.Id,
                s.ClassTypeId,
                ct.Name,
                s.InstructorId,
                u.FirstName + " " + u.LastName,
                s.StartsAt,
                s.DurationMinutes,
                s.Capacity,
                s.BookedCount,
                s.IsCancelled))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var session = await _dbContext.ClassSessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session is null)
        {
            return false;
        }

        session.IsCancelled = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
```

- [ ] **Step 3:** `ClassSessionsController.cs`. Ο instructor παίρνει πάντα το δικό του `sub`· ο Admin μπορεί να ορίσει `InstructorId`:

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("class-sessions")]
[Authorize(Policy = Policies.RequireInstructor)]
public class ClassSessionsController : ControllerBase
{
    private readonly ClassSessionService _service;

    public ClassSessionsController(ClassSessionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassSessionResponse>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClassSessionResponse>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ClassSessionResponse>> Create([FromBody] CreateClassSessionRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirst("sub")!.Value);
        var isAdmin = User.IsInRole(Roles.Admin);

        // Instructor → πάντα ο εαυτός του. Admin → ό,τι έδωσε (ή ο εαυτός του αν δεν έδωσε).
        var instructorId = isAdmin && request.InstructorId.HasValue
            ? request.InstructorId.Value
            : currentUserId;

        var newId = await _service.CreateAsync(request.ClassTypeId, instructorId, request.StartsAt, request.DurationMinutes, request.Capacity);
        if (newId is null)
        {
            return BadRequest("Invalid or inactive class type.");
        }

        var created = await _service.GetByIdAsync(newId.Value);
        return CreatedAtAction(nameof(GetById), new { id = newId.Value }, created);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var ok = await _service.CancelAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
```

- [ ] **Step 4:** DI στο `Program.cs`:

```csharp
builder.Services.AddScoped<ClassSessionService>();
```

- [ ] **Step 5 (tests):** `ClassSessionsTests.cs` — αντέγραψε ξανά τα helpers `CreateUserAsync`/`LoginAsync` (όπως στο Task 19). Τεστ: (α) instructor create → session με `InstructorId == δικό του`· (β) create με ανύπαρκτο `ClassTypeId` → 400. Χρειάζεται πρώτα ένα ClassType — φτιάξ' το μέσω του `/class-types` endpoint με τον ίδιο instructor:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class ClassSessionsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ClassSessionsTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    // (Αντέγραψε CreateUserAsync + LoginAsync από το ClassTypesTests/InvitationsTests — ίδια υλοποίηση.)

    [Fact]
    public async Task CreateSession_as_instructor_assigns_self_as_instructor()
    {
        var instructor = await CreateUserAsync("sess-instr@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "sess-instr@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ctResponse = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("Spin", "Ποδήλατο", 50, 15));
        var classType = await ctResponse.Content.ReadFromJsonAsync<ClassTypeResponse>();

        var sessionResponse = await client.PostAsJsonAsync("/class-sessions",
            new CreateClassSessionRequest(classType!.Id, null, DateTime.UtcNow.AddDays(2), 50, 15));

        Assert.Equal(HttpStatusCode.Created, sessionResponse.StatusCode);
        var session = await sessionResponse.Content.ReadFromJsonAsync<ClassSessionResponse>();
        Assert.Equal(instructor.Id, session!.InstructorId);
        Assert.Equal("Spin", session.ClassTypeName);
        Assert.Equal(0, session.BookedCount);
    }

    [Fact]
    public async Task CreateSession_with_unknown_class_type_returns_400()
    {
        await CreateUserAsync("sess-instr2@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "sess-instr2@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/class-sessions",
            new CreateClassSessionRequest(Guid.NewGuid(), null, DateTime.UtcNow.AddDays(2), 60, 10));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 6:** Τρέξε → PASS:

Run: `cd backend && dotnet test --filter "FullyQualifiedName~ClassSessionsTests"`
Expected: PASS (2 tests). Μετά τρέξε ΟΛΑ (`dotnet test`) → όλα PASS.

- [ ] **Step 7: Commit** `git commit -am "feat: ClassSession CRUD (instructor self-assign) + tests"`

### Task 21: Frontend models + data-access services

**Files:**
- Modify: `frontend/libs/models/src/lib/models.ts`
- Create: `frontend/libs/data-access/src/lib/class-type-api.service.ts`
- Create: `frontend/libs/data-access/src/lib/class-session-api.service.ts`
- Modify: `frontend/libs/data-access/src/index.ts`

> Node 20: `nvm use 20` πριν οτιδήποτε frontend.

- [ ] **Step 1:** Πρόσθεσε στο τέλος του `models.ts` (τα request types καθρεφτίζουν τα .NET contracts):

```typescript
export interface ClassType {
  id: string;
  name: string;
  description: string;
  defaultDurationMinutes: number;
  defaultCapacity: number;
  isActive: boolean;
}

export interface CreateClassTypeRequest {
  name: string;
  description: string;
  defaultDurationMinutes: number;
  defaultCapacity: number;
}

export type UpdateClassTypeRequest = CreateClassTypeRequest;

export interface ClassSession {
  id: string;
  classTypeId: string;
  classTypeName: string;
  instructorId: string;
  instructorName: string;
  startsAt: string; // ISO UTC
  durationMinutes: number;
  capacity: number;
  bookedCount: number;
  isCancelled: boolean;
}

export interface CreateClassSessionRequest {
  classTypeId: string;
  instructorId?: string | null;
  startsAt: string; // ISO UTC
  durationMinutes: number;
  capacity: number;
}
```

- [ ] **Step 2:** `class-type-api.service.ts` (ίδιο style με `auth-api.service.ts`):

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ClassType, CreateClassTypeRequest, UpdateClassTypeRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class ClassTypeApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<ClassType[]> {
    return this.http.get<ClassType[]>('/class-types');
  }

  create(request: CreateClassTypeRequest): Observable<ClassType> {
    return this.http.post<ClassType>('/class-types', request);
  }

  update(id: string, request: UpdateClassTypeRequest): Observable<void> {
    return this.http.put<void>(`/class-types/${id}`, request);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`/class-types/${id}`);
  }
}
```

- [ ] **Step 3:** `class-session-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ClassSession, CreateClassSessionRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class ClassSessionApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<ClassSession[]> {
    return this.http.get<ClassSession[]>('/class-sessions');
  }

  create(request: CreateClassSessionRequest): Observable<ClassSession> {
    return this.http.post<ClassSession>('/class-sessions', request);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`/class-sessions/${id}/cancel`, {});
  }
}
```

- [ ] **Step 4:** Πρόσθεσε στο `frontend/libs/data-access/src/index.ts`:

```typescript
export * from './lib/class-type-api.service';
export * from './lib/class-session-api.service';
```

- [ ] **Step 5:** Επιβεβαίωσε ότι χτίζουν οι libs:

Run: `cd frontend && npx nx run-many -t lint -p models data-access`
Expected: PASS.

- [ ] **Step 6: Commit** `git commit -am "feat(fe): ClassType/ClassSession models + API services"`

### Task 22: staff app — ClassTypes σελίδα (list + create)

**Files:**
- Create: `frontend/apps/staff/src/app/class-types/class-types.ts`
- Create: `frontend/apps/staff/src/app/class-types/class-types.html`
- Modify: `frontend/apps/staff/src/app/app.routes.ts`
- Modify: `frontend/apps/staff/src/app/dashboard/dashboard.html` (link προς τη νέα σελίδα)

> Η σελίδα προστατεύεται με `roleGuard(Roles.Instructor, Roles.Admin)`. Οι ρόλοι στο FE είναι απλά strings — χρησιμοποίησε literals `'Instructor'`/`'Admin'` (δεν υπάρχει shared roles const στο FE ακόμα).

- [ ] **Step 1:** `class-types.ts` (standalone, Signals, ReactiveForms — ίδιο style με `login.ts`):

```typescript
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { ClassTypeApiService } from '@frontend/data-access';
import { ClassType } from '@frontend/models';

@Component({
  selector: 'app-class-types',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatListModule],
  templateUrl: './class-types.html',
})
export class ClassTypes {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ClassTypeApiService);

  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly submitting = signal(false);

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
    this.api.getAll().subscribe((items) => this.classTypes.set(items));
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.form.reset({ defaultDurationMinutes: 60, defaultCapacity: 10 });
        this.load();
      },
      error: () => this.submitting.set(false),
    });
  }
}
```

- [ ] **Step 2:** `class-types.html`:

```html
<h1 class="text-2xl font-bold m-4">Είδη μαθημάτων</h1>

<form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 m-4 max-w-md">
  <mat-form-field>
    <mat-label>Όνομα</mat-label>
    <input matInput formControlName="name" />
  </mat-form-field>
  <mat-form-field>
    <mat-label>Περιγραφή</mat-label>
    <input matInput formControlName="description" />
  </mat-form-field>
  <mat-form-field>
    <mat-label>Διάρκεια (λεπτά)</mat-label>
    <input matInput type="number" formControlName="defaultDurationMinutes" />
  </mat-form-field>
  <mat-form-field>
    <mat-label>Χωρητικότητα</mat-label>
    <input matInput type="number" formControlName="defaultCapacity" />
  </mat-form-field>
  <button mat-raised-button color="primary" type="submit" [disabled]="submitting()">
    Προσθήκη
  </button>
</form>

<mat-list class="m-4">
  @for (ct of classTypes(); track ct.id) {
    <mat-list-item>{{ ct.name }} — {{ ct.defaultDurationMinutes }}′ / {{ ct.defaultCapacity }} θέσεις</mat-list-item>
  } @empty {
    <p class="m-4 text-gray-500">Δεν υπάρχουν είδη μαθημάτων ακόμα.</p>
  }
</mat-list>
```

- [ ] **Step 3:** Route στο `app.routes.ts` — import `ClassTypes` + `roleGuard`, πρόσθεσε πριν το wildcard:

```typescript
import { authGuard, roleGuard } from '@frontend/auth';
import { ClassTypes } from './class-types/class-types';
```
```typescript
  { path: 'class-types', component: ClassTypes, canActivate: [roleGuard('Instructor', 'Admin')] },
```

- [ ] **Step 4:** Στο `dashboard.html` πρόσθεσε ένα link (π.χ. `<a mat-button routerLink="/class-types">Είδη μαθημάτων</a>` — θέλει `RouterLink` στα imports του `Dashboard` component· πρόσθεσέ το: `import { RouterLink } from '@angular/router';` και βάλ' το στο `imports: [MatButtonModule, RouterLink]`).

- [ ] **Step 5:** Verify build + lint:

Run: `cd frontend && npx nx lint staff && npx nx build staff`
Expected: PASS.

- [ ] **Step 6: Commit** `git commit -am "feat(staff): class types management page"`

### Task 23: staff app — ClassSessions σελίδα (list + create)

**Files:**
- Create: `frontend/apps/staff/src/app/sessions/sessions.ts`
- Create: `frontend/apps/staff/src/app/sessions/sessions.html`
- Modify: `frontend/apps/staff/src/app/app.routes.ts`
- Modify: `frontend/apps/staff/src/app/dashboard/dashboard.html` (link)

- [ ] **Step 1:** `sessions.ts` (φορτώνει class types για dropdown + list sessions· instructor δεν στέλνει instructorId → self):

```typescript
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatSelectModule } from '@angular/material/select';
import { ClassSessionApiService, ClassTypeApiService } from '@frontend/data-access';
import { ClassSession, ClassType } from '@frontend/models';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatListModule,
  ],
  templateUrl: './sessions.html',
})
export class Sessions {
  private readonly fb = inject(FormBuilder);
  private readonly sessionApi = inject(ClassSessionApiService);
  private readonly classTypeApi = inject(ClassTypeApiService);

  protected readonly sessions = signal<ClassSession[]>([]);
  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    classTypeId: ['', Validators.required],
    startsAt: ['', Validators.required], // datetime-local (τοπική) → μετατροπή σε ISO UTC στο submit
    durationMinutes: [60, [Validators.required, Validators.min(1)]],
    capacity: [10, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    this.classTypeApi.getAll().subscribe((items) => this.classTypes.set(items));
    this.load();
  }

  private load(): void {
    this.sessionApi.getAll().subscribe((items) => this.sessions.set(items));
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
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
        error: () => this.submitting.set(false),
      });
  }
}
```

- [ ] **Step 2:** `sessions.html`:

```html
<h1 class="text-2xl font-bold m-4">Ώρες μαθημάτων (sessions)</h1>

<form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 m-4 max-w-md">
  <mat-form-field>
    <mat-label>Είδος μαθήματος</mat-label>
    <mat-select formControlName="classTypeId">
      @for (ct of classTypes(); track ct.id) {
        <mat-option [value]="ct.id">{{ ct.name }}</mat-option>
      }
    </mat-select>
  </mat-form-field>
  <mat-form-field>
    <mat-label>Έναρξη</mat-label>
    <input matInput type="datetime-local" formControlName="startsAt" />
  </mat-form-field>
  <mat-form-field>
    <mat-label>Διάρκεια (λεπτά)</mat-label>
    <input matInput type="number" formControlName="durationMinutes" />
  </mat-form-field>
  <mat-form-field>
    <mat-label>Χωρητικότητα</mat-label>
    <input matInput type="number" formControlName="capacity" />
  </mat-form-field>
  <button mat-raised-button color="primary" type="submit" [disabled]="submitting()">
    Δημιουργία session
  </button>
</form>

<mat-list class="m-4">
  @for (s of sessions(); track s.id) {
    <mat-list-item>
      {{ s.classTypeName }} — {{ s.startsAt | date: 'dd/MM HH:mm' }} —
      {{ s.instructorName }} — {{ s.bookedCount }}/{{ s.capacity }}
    </mat-list-item>
  } @empty {
    <p class="m-4 text-gray-500">Δεν υπάρχουν sessions ακόμα.</p>
  }
</mat-list>
```

> Το `| date` pipe θέλει `CommonModule` ή το standalone `DatePipe`. Πρόσθεσε `import { DatePipe } from '@angular/common';` και βάλε `DatePipe` στα `imports` του `Sessions` component.

- [ ] **Step 3:** Route στο `app.routes.ts`:

```typescript
import { Sessions } from './sessions/sessions';
```
```typescript
  { path: 'sessions', component: Sessions, canActivate: [roleGuard('Instructor', 'Admin')] },
```

- [ ] **Step 4:** Link στο `dashboard.html`: `<a mat-button routerLink="/sessions">Ώρες μαθημάτων</a>`.

- [ ] **Step 5:** Verify: `cd frontend && npx nx lint staff && npx nx build staff` → PASS. (Optional e2e με το backend up: login ως instructor → `/class-types` πρόσθεσε είδος → `/sessions` δημιούργησε session → εμφανίζεται στη λίστα.)

- [ ] **Step 6: Commit** `git commit -am "feat(staff): class sessions management page"`

### Task 24: CI check + ενημέρωση progress

**Files:** Modify `CLAUDE.md`

- [ ] **Step 1:** Τρέξε ό,τι τρέχει το CI:

```bash
cd backend && dotnet build && dotnet test
cd ../frontend && npx nx run-many -t lint build
```
Expected: όλα PASS.

- [ ] **Step 2:** Στο `CLAUDE.md` → progress tracker: τσέκαρε `[x] Φ2 — ClassType/ClassSession CRUD + instructor` και ενημέρωσε το «Τώρα δουλεύω / Επόμενο» σε **Φ3 — Booking core (atomic)**.

- [ ] **Step 3: Commit** `git commit -am "docs: mark Phase 2 complete"`

---

## PHASE 3: 🔥 Booking core (atomic) + tests (~14h)

> Η κρίσιμη φάση. TDD παντού. **Το atomic booking απαιτεί πραγματική PostgreSQL** (transactions + row locks) — το EF InMemory δεν αρκεί, άρα εισάγουμε **Testcontainers** ως νέο test harness (παράλληλα με το υπάρχον InMemory `TestApiFactory` που μένει για τα Φ1/Φ2 tests). Ακολουθεί τα Φ1/Φ2 patterns: flat entities, query filters ανά tenant, services στο `GymBooking.Api/Services`, contracts (records) στο `GymBooking.Core/Contracts`.

### Αποφάσεις Φάσης 3 (κλειδωμένες)
- **Test infra = Testcontainers (πραγματική Postgres 16)** _(2026-07-09)_. Νέο `PostgresApiFactory` που σηκώνει container, εφαρμόζει migrations, δίνει real DbContext. Απαραίτητο: το `SELECT … FOR UPDATE` και τα transactions δεν υπάρχουν στο InMemory. Το CI έχει Docker.
- **Locking = pessimistic `SELECT … FOR UPDATE`** _(2026-07-09)_ μέσα σε transaction, με `FromSqlInterpolated` (parameterized — no SQL injection) + `IgnoreQueryFilters()` + **ρητό `TenantId` στο SQL**. Το `IgnoreQueryFilters` είναι κρίσιμο: αλλιώς το EF τυλίγει το `FOR UPDATE` σε subquery (invalid στην Postgres). Το `BookedCount` αλλάζει **σε ένα μόνο σημείο** (`BookingService`) — εκεί θα μπει η γραμμή SignalR σε μελλοντική φάση.
- **Anti-double-booking = ίδιο session + overlapping χρόνος** _(2026-07-09)_. Ο overlap έλεγχος γίνεται σε C# (in-memory) πάνω στα confirmed sessions του χρήστη — ο Npgsql δεν μεταφράζει `AddMinutes(column)` σε SQL.
- **Booking = κάθε authenticated χρήστης** _(2026-07-09)_: απλό `[Authorize]` στον `BookingsController`. Χωρίς νέα policy (στην πράξη role=User κάνει κρατήσεις). Ownership («ακυρώνεις μόνο τη δική σου») = ρητός έλεγχος `booking.UserId == currentUserId` στο service layer (ίδιο σκεπτικό με την απόφαση Φ1).
- **WaitlistEntry = αναβολή στη Φ6** _(2026-07-09)_. Εδώ **μόνο** `Booking`. Καμία subscription consumption (Φ5) και καμία cancellation-policy χρονικού ορίου (Φ6) — ο cancel της Φ3 επιστρέφει πάντα τη θέση.
- **Session GET → ανοιχτό σε authenticated** _(2026-07-09)_: χαλαρώνουμε το `[Authorize(Policy=RequireInstructor)]` του `ClassSessionsController` ώστε τα **GET** να είναι προσβάσιμα σε κάθε συνδεδεμένο (ο customer πρέπει να βλέπει sessions για να κρατήσει)· τα **write** (POST/cancel) μένουν `RequireInstructor`.

### Task 25: Booking entity + enums + DbContext + migration

**Files:**
- Create: `backend/src/GymBooking.Core/Entities/Enums/BookingStatus.cs`
- Create: `backend/src/GymBooking.Core/Entities/Enums/BookingOutcome.cs`
- Create: `backend/src/GymBooking.Core/Entities/Models/Booking.cs`
- Modify: `backend/src/GymBooking.Data/AppDbContext.cs`
- Test: `backend/tests/GymBooking.Tests/TenantIsolationTests.cs`

- [ ] **Step 1:** `BookingStatus.cs`:

```csharp
namespace GymBooking.Core.Entities.Enums;

public enum BookingStatus
{
    Confirmed,   // = 0
    Cancelled,   // = 1
}
```

- [ ] **Step 2:** `BookingOutcome.cs` (το επιστρέφει το service· ο controller το χαρτογραφεί σε HTTP):

```csharp
namespace GymBooking.Core.Entities.Enums;

public enum BookingOutcome
{
    Success,
    SessionNotFound,
    SessionCancelled,
    SessionFull,
    AlreadyBooked,
    TimeConflict,
}
```

- [ ] **Step 3:** `Booking.cs` (flat entity):

```csharp
using GymBooking.Core.Entities.Enums;

namespace GymBooking.Core.Entities.Models;

public class Booking
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid ClassSessionId { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
```

- [ ] **Step 4:** Στο `AppDbContext.cs` πρόσθεσε DbSet + query filter + **partial unique index** (μία confirmed κράτηση ανά χρήστη/session· cancelled δεν μετράει, άρα επιτρέπεται re-book). Στο `OnModelCreating`, μετά τα υπάρχοντα:

```csharp
    public DbSet<Booking> Bookings => Set<Booking>();
```

```csharp
        modelBuilder.Entity<Booking>().HasQueryFilter(b => b.TenantId == _currentTenant.TenantId);

        // DB-level δικλείδα ασφαλείας ενάντια σε διπλή confirmed κράτηση (πέρα από τον έλεγχο στο service).
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.UserId, b.ClassSessionId })
            .IsUnique()
            .HasFilter("\"Status\" = 0");
```

- [ ] **Step 5 (test first):** Πρόσθεσε στο `TenantIsolationTests.cs` (ίδιο μοτίβο με τα υπάρχοντα):

```csharp
    [Fact]
    public void Bookings_query_returns_only_current_tenant_rows()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var seedContext = CreateContext(dbName, new FakeCurrentTenant(tenantA)))
        {
            seedContext.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                UserId = Guid.NewGuid(),
                ClassSessionId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
            });
            seedContext.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                TenantId = tenantB,
                UserId = Guid.NewGuid(),
                ClassSessionId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
            });
            seedContext.SaveChanges();
        }

        using var queryContext = CreateContext(dbName, new FakeCurrentTenant(tenantA));
        var results = queryContext.Bookings.ToList();

        Assert.Single(results);
        Assert.Equal(tenantA, results[0].TenantId);
    }
```
Πρόσθεσε `using GymBooking.Core.Entities.Models;` αν λείπει. (Το InMemory αγνοεί το `HasFilter` — OK, το test ελέγχει μόνο το tenant query filter.)

- [ ] **Step 6:** Τρέξε → PASS: `cd backend && dotnet test --filter "FullyQualifiedName~TenantIsolationTests"`

- [ ] **Step 7:** Migration + apply (DB up: `docker compose -f docker/docker-compose.yml up -d`):

```bash
cd backend
dotnet ef migrations add AddBookings -p src/GymBooking.Data -s src/GymBooking.Api
dotnet ef database update -p src/GymBooking.Data -s src/GymBooking.Api
```
Expected: πίνακας `Bookings` + partial unique index στο Postgres.

- [ ] **Step 8: Commit** `git add -A && git commit -m "feat: add Booking entity + partial unique index + tenant isolation"`

### Task 26: Testcontainers test harness (PostgresApiFactory)

**Files:**
- Modify: `backend/tests/GymBooking.Tests/GymBooking.Tests.csproj`
- Create: `backend/tests/GymBooking.Tests/PostgresApiFactory.cs`
- Create: `backend/tests/GymBooking.Tests/PostgresCollection.cs`
- Test: `backend/tests/GymBooking.Tests/PostgresHarnessSmokeTests.cs`

- [ ] **Step 1:** Πρόσθεσε το package (και άφησε το InMemory — χρησιμοποιείται ακόμα):

```bash
cd backend
dotnet add tests/GymBooking.Tests package Testcontainers.PostgreSql
```

- [ ] **Step 2:** `PostgresApiFactory.cs`. Σηκώνει container, τρέχει τα migrations **πριν** χτιστεί ο host (αλλιώς ο seeder του `Program.cs` σκάει σε ανύπαρκτους πίνακες), και καταχωρεί Npgsql DbContext (το `Program.cs` παραλείπει το δικό του σε env `Testing`):

```csharp
using GymBooking.Api.Interfaces;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GymBooking.Tests;

public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_db.GetConnectionString())
            .Options;
        await using var ctx = new AppDbContext(options, new NoTenant());
        await ctx.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));

            // Χωρίς πραγματικό SMTP στα tests.
            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<FakeEmailSender>());
        });
    }

    private sealed class NoTenant : ICurrentTenant
    {
        public Guid TenantId => Guid.Empty;
    }
}
```

- [ ] **Step 3:** `PostgresCollection.cs` — μοιράζεται **ένα** container σε όλα τα Postgres test classes (γρήγορο):

```csharp
namespace GymBooking.Tests;

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresApiFactory>
{
}
```

- [ ] **Step 4 (smoke test):** `PostgresHarnessSmokeTests.cs` — αποδεικνύει ότι ο host σηκώνεται πάνω σε πραγματική Postgres (ο seeder έτρεξε → ο admin μπορεί να κάνει login):

```csharp
using System.Net.Http.Json;
using GymBooking.Core.Contracts;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class PostgresHarnessSmokeTests
{
    private readonly PostgresApiFactory _factory;

    public PostgresHarnessSmokeTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seeded_admin_can_login_against_real_postgres()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest("admin@demo.gym", "Admin123!"));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
    }
}
```

- [ ] **Step 5:** Verify (θέλει Docker running): `cd backend && dotnet test --filter "FullyQualifiedName~PostgresHarnessSmokeTests"`
Expected: PASS (κατεβάζει το `postgres:16-alpine` την πρώτη φορά).

- [ ] **Step 6: Commit** `git commit -am "test: add Testcontainers Postgres harness"`

### Task 27: Atomic BookingService — happy path + capacity (TDD, real Postgres)

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/BookingContracts.cs`
- Create: `backend/src/GymBooking.Api/Services/BookingService.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs` (DI)
- Test: `backend/tests/GymBooking.Tests/BookingTests.cs`

- [ ] **Step 1:** Contracts:

```csharp
namespace GymBooking.Core.Contracts;

public record CreateBookingRequest(Guid ClassSessionId);
public record BookingResponse(Guid Id, Guid ClassSessionId, string ClassTypeName, DateTime StartsAt, string Status, DateTime CreatedAt);
```

- [ ] **Step 2:** `BookingService.cs` — **πλήρες** (περιλαμβάνει ήδη double-booking/overlap/cancel/getmine για να μη χρειαστεί ξαναγράψιμο στα επόμενα tasks· αυτό το task τεστάρει μόνο happy-path + capacity):

```csharp
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class BookingService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public BookingService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<(BookingOutcome Outcome, Booking? Booking)> BookAsync(Guid userId, Guid classSessionId)
    {
        var tenantId = _currentTenant.TenantId;

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        // Κλειδώνει τη ΣΕΙΡΑ του session μέχρι το COMMIT — no overbooking σε ταυτόχρονες κρατήσεις.
        var session = await _dbContext.ClassSessions
            .FromSqlInterpolated($@"SELECT * FROM ""ClassSessions"" WHERE ""Id"" = {classSessionId} AND ""TenantId"" = {tenantId} FOR UPDATE")
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync();

        if (session is null || !session.IsActive)
        {
            return (BookingOutcome.SessionNotFound, null);
        }

        if (session.IsCancelled)
        {
            return (BookingOutcome.SessionCancelled, null);
        }

        var alreadyBooked = await _dbContext.Bookings
            .AnyAsync(b => b.UserId == userId && b.ClassSessionId == classSessionId && b.Status == BookingStatus.Confirmed);
        if (alreadyBooked)
        {
            return (BookingOutcome.AlreadyBooked, null);
        }

        // Overlap: φέρνουμε τα confirmed sessions του χρήστη στη μνήμη (λίγα) — το AddMinutes γίνεται σε C#.
        var newStart = session.StartsAt;
        var newEnd = session.StartsAt.AddMinutes(session.DurationMinutes);
        var userSessions = await (
            from b in _dbContext.Bookings
            where b.UserId == userId && b.Status == BookingStatus.Confirmed
            join s in _dbContext.ClassSessions on b.ClassSessionId equals s.Id
            select new { s.StartsAt, s.DurationMinutes }).ToListAsync();

        var overlaps = userSessions.Any(x => x.StartsAt < newEnd && newStart < x.StartsAt.AddMinutes(x.DurationMinutes));
        if (overlaps)
        {
            return (BookingOutcome.TimeConflict, null);
        }

        if (session.BookedCount >= session.Capacity)
        {
            return (BookingOutcome.SessionFull, null);
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ClassSessionId = classSessionId,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
        };
        _dbContext.Bookings.Add(booking);
        session.BookedCount += 1; // ΤΟ ΜΟΝΑΔΙΚΟ σημείο αλλαγής του BookedCount

        await _dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return (BookingOutcome.Success, booking);
    }

    public async Task<(BookingOutcome Outcome, Booking? Booking)> CancelAsync(Guid userId, Guid bookingId)
    {
        var tenantId = _currentTenant.TenantId;

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null || booking.UserId != userId)
        {
            return (BookingOutcome.SessionNotFound, null); // ownership: μη-δική-σου → σαν να μην υπάρχει
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return (BookingOutcome.Success, booking); // idempotent
        }

        var session = await _dbContext.ClassSessions
            .FromSqlInterpolated($@"SELECT * FROM ""ClassSessions"" WHERE ""Id"" = {booking.ClassSessionId} AND ""TenantId"" = {tenantId} FOR UPDATE")
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync();

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;

        if (session is not null && session.BookedCount > 0)
        {
            session.BookedCount -= 1; // επιστροφή θέσης
        }

        await _dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return (BookingOutcome.Success, booking);
    }

    public async Task<List<BookingResponse>> GetMineAsync(Guid userId)
    {
        return await (
            from b in _dbContext.Bookings
            where b.UserId == userId
            join s in _dbContext.ClassSessions on b.ClassSessionId equals s.Id
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            orderby s.StartsAt
            select new BookingResponse(
                b.Id,
                b.ClassSessionId,
                ct.Name,
                s.StartsAt,
                b.Status.ToString(),
                b.CreatedAt))
            .ToListAsync();
    }
}
```

- [ ] **Step 3:** DI στο `Program.cs`: `builder.Services.AddScoped<BookingService>();`

- [ ] **Step 4 (tests):** `BookingTests.cs` με helpers που στήνουν session + καλούν το service μέσω scope (κάθε κλήση = δικό της scope/DbContext/transaction, όπως στην πραγματικότητα):

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class BookingTests
{
    private readonly PostgresApiFactory _factory;

    public BookingTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    // Στήνει ClassType + ClassSession σε νέο tenant· επιστρέφει (tenantId, sessionId).
    private async Task<(Guid TenantId, Guid SessionId)> SeedSessionAsync(int capacity, DateTime startsAtUtc, int durationMinutes = 60)
    {
        var tenantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var classType = new ClassType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Yoga",
            DefaultDurationMinutes = durationMinutes,
            DefaultCapacity = capacity,
            IsActive = true,
        };
        var session = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClassTypeId = classType.Id,
            InstructorId = Guid.NewGuid(),
            StartsAt = startsAtUtc,
            DurationMinutes = durationMinutes,
            Capacity = capacity,
            BookedCount = 0,
            IsActive = true,
        };
        db.ClassTypes.Add(classType);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return (tenantId, session.Id);
    }

    private async Task<(BookingOutcome Outcome, Booking? Booking)> BookAsync(Guid tenantId, Guid userId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var service = scope.ServiceProvider.GetRequiredService<BookingService>();
        return await service.BookAsync(userId, sessionId);
    }

    private async Task<ClassSession> GetSessionAsync(Guid tenantId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ClassSessions.FirstAsync(s => s.Id == sessionId);
    }

    [Fact]
    public async Task Book_available_session_succeeds_and_increments_bookedCount()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));

        var (outcome, booking) = await BookAsync(tenantId, Guid.NewGuid(), sessionId);

        Assert.Equal(BookingOutcome.Success, outcome);
        Assert.NotNull(booking);
        Assert.Equal(1, (await GetSessionAsync(tenantId, sessionId)).BookedCount);
    }

    [Fact]
    public async Task Book_full_session_returns_SessionFull()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1, startsAtUtc: DateTime.UtcNow.AddDays(1));
        await BookAsync(tenantId, Guid.NewGuid(), sessionId); // γεμίζει

        var (outcome, _) = await BookAsync(tenantId, Guid.NewGuid(), sessionId);

        Assert.Equal(BookingOutcome.SessionFull, outcome);
        Assert.Equal(1, (await GetSessionAsync(tenantId, sessionId)).BookedCount);
    }
}
```

- [ ] **Step 5:** Verify → PASS: `cd backend && dotnet test --filter "FullyQualifiedName~BookingTests"`

- [ ] **Step 6: Commit** `git commit -am "feat: atomic BookingService (FOR UPDATE) + capacity tests"`

### Task 28: 🔥 Concurrency test — no overbooking

**Files:** Modify `backend/tests/GymBooking.Tests/BookingTests.cs`

> Το κορυφαίο test της πτυχιακής: αποδεικνύει ότι ταυτόχρονες κρατήσεις **δεν** ξεπερνούν το capacity. Δεν χρειάζεται νέος κώδικας — μόνο test (το `FOR UPDATE` του Task 27 είναι ήδη η λύση).

- [ ] **Step 1:** Πρόσθεσε στο `BookingTests.cs`:

```csharp
    [Fact]
    public async Task Concurrent_bookings_never_exceed_capacity()
    {
        const int capacity = 3;
        const int attempts = 12;
        var (tenantId, sessionId) = await SeedSessionAsync(capacity, DateTime.UtcNow.AddDays(1));

        // 12 διαφορετικοί χρήστες κρατούν ΤΑΥΤΟΧΡΟΝΑ την ίδια (capacity=3) ώρα.
        var tasks = Enumerable.Range(0, attempts)
            .Select(_ => BookAsync(tenantId, Guid.NewGuid(), sessionId))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        var successes = results.Count(r => r.Outcome == BookingOutcome.Success);
        var full = results.Count(r => r.Outcome == BookingOutcome.SessionFull);

        Assert.Equal(capacity, successes);          // ακριβώς 3 πέρασαν
        Assert.Equal(attempts - capacity, full);    // οι υπόλοιποι πήραν "γεμάτο"
        Assert.Equal(capacity, (await GetSessionAsync(tenantId, sessionId)).BookedCount);
    }
```

- [ ] **Step 2:** Verify → PASS: `cd backend && dotnet test --filter "FullyQualifiedName~BookingTests.Concurrent_bookings_never_exceed_capacity"`
Expected: PASS σταθερά (τρέξ' το 2–3 φορές για σιγουριά — δεν πρέπει να "τρεμοπαίζει").

- [ ] **Step 3: Commit** `git commit -am "test: concurrency proof — no overbooking under parallel bookings"`

### Task 29: Anti-double-booking (ίδιο session + overlapping χρόνος)

**Files:** Modify `backend/tests/GymBooking.Tests/BookingTests.cs`

> Η λογική είναι ήδη στο `BookingService` (Task 27). Εδώ την επικυρώνουμε.

- [ ] **Step 1:** Πρόσθεσε helper για δεύτερο session στον **ίδιο** tenant + τα tests:

```csharp
    private async Task<Guid> SeedAnotherSessionAsync(Guid tenantId, DateTime startsAtUtc, int durationMinutes = 60, int capacity = 5)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var classType = new ClassType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Spin",
            DefaultDurationMinutes = durationMinutes,
            DefaultCapacity = capacity,
            IsActive = true,
        };
        var session = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClassTypeId = classType.Id,
            InstructorId = Guid.NewGuid(),
            StartsAt = startsAtUtc,
            DurationMinutes = durationMinutes,
            Capacity = capacity,
            BookedCount = 0,
            IsActive = true,
        };
        db.ClassTypes.Add(classType);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    [Fact]
    public async Task Booking_same_session_twice_returns_AlreadyBooked()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var userId = Guid.NewGuid();
        await BookAsync(tenantId, userId, sessionId);

        var (outcome, _) = await BookAsync(tenantId, userId, sessionId);

        Assert.Equal(BookingOutcome.AlreadyBooked, outcome);
    }

    [Fact]
    public async Task Booking_time_overlapping_session_returns_TimeConflict()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var (tenantId, sessionA) = await SeedSessionAsync(capacity: 5, startsAtUtc: start, durationMinutes: 60);
        var sessionB = await SeedAnotherSessionAsync(tenantId, start.AddMinutes(30), durationMinutes: 60); // 30' επικάλυψη
        var userId = Guid.NewGuid();
        await BookAsync(tenantId, userId, sessionA);

        var (outcome, _) = await BookAsync(tenantId, userId, sessionB);

        Assert.Equal(BookingOutcome.TimeConflict, outcome);
    }
```

- [ ] **Step 2:** Verify → PASS: `cd backend && dotnet test --filter "FullyQualifiedName~BookingTests"`

- [ ] **Step 3: Commit** `git commit -am "test: anti-double-booking (same session + time overlap)"`

### Task 30: Cancel + επιστροφή θέσης

**Files:** Modify `backend/tests/GymBooking.Tests/BookingTests.cs`

> `CancelAsync` ήδη υλοποιήθηκε στο Task 27. Εδώ επικύρωση: επιστροφή θέσης, ownership, re-book μετά από cancel.

- [ ] **Step 1:** Πρόσθεσε helper + tests:

```csharp
    private async Task<(BookingOutcome Outcome, Booking? Booking)> CancelAsync(Guid tenantId, Guid userId, Guid bookingId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var service = scope.ServiceProvider.GetRequiredService<BookingService>();
        return await service.CancelAsync(userId, bookingId);
    }

    [Fact]
    public async Task Cancel_returns_the_spot_and_allows_rebooking()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var userId = Guid.NewGuid();
        var (_, booking) = await BookAsync(tenantId, userId, sessionId);
        Assert.Equal(1, (await GetSessionAsync(tenantId, sessionId)).BookedCount);

        var (cancelOutcome, _) = await CancelAsync(tenantId, userId, booking!.Id);
        Assert.Equal(BookingOutcome.Success, cancelOutcome);
        Assert.Equal(0, (await GetSessionAsync(tenantId, sessionId)).BookedCount); // θέση επέστρεψε

        // Ο ίδιος χρήστης μπορεί να ξανακρατήσει (η cancelled δεν μπλοκάρει από το partial unique index).
        var (rebookOutcome, _) = await BookAsync(tenantId, userId, sessionId);
        Assert.Equal(BookingOutcome.Success, rebookOutcome);
    }

    [Fact]
    public async Task Cancel_someone_elses_booking_is_rejected()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var (_, booking) = await BookAsync(tenantId, Guid.NewGuid(), sessionId);

        var (outcome, _) = await CancelAsync(tenantId, Guid.NewGuid(), booking!.Id); // άλλος χρήστης

        Assert.Equal(BookingOutcome.SessionNotFound, outcome);
        Assert.Equal(1, (await GetSessionAsync(tenantId, sessionId)).BookedCount); // δεν επέστρεψε θέση
    }
```

- [ ] **Step 2:** Verify → PASS: `cd backend && dotnet test --filter "FullyQualifiedName~BookingTests"`

- [ ] **Step 3: Commit** `git commit -am "test: cancel returns spot, ownership enforced, rebooking allowed"`

### Task 31: BookingsController + endpoints + relax session GET

**Files:**
- Create: `backend/src/GymBooking.Api/Controllers/BookingsController.cs`
- Modify: `backend/src/GymBooking.Api/Controllers/ClassSessionsController.cs`
- Test: `backend/tests/GymBooking.Tests/BookingEndpointsTests.cs`

- [ ] **Step 1:** `BookingsController.cs` (userId από το `sub` claim· outcome → HTTP):

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly BookingService _service;

    public BookingsController(BookingService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetMine()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        return Ok(await _service.GetMineAsync(userId));
    }

    [HttpPost]
    public async Task<IActionResult> Book([FromBody] CreateBookingRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var (outcome, _) = await _service.BookAsync(userId, request.ClassSessionId);
        return outcome switch
        {
            BookingOutcome.Success => CreatedAtAction(nameof(GetMine), null, null),
            BookingOutcome.SessionNotFound => NotFound(),
            BookingOutcome.SessionCancelled => Conflict("Το session έχει ακυρωθεί."),
            BookingOutcome.SessionFull => Conflict("Το session είναι πλήρες."),
            BookingOutcome.AlreadyBooked => Conflict("Έχεις ήδη κράτηση σε αυτό το session."),
            BookingOutcome.TimeConflict => Conflict("Έχεις άλλη κράτηση που επικαλύπτεται χρονικά."),
            _ => BadRequest(),
        };
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var (outcome, _) = await _service.CancelAsync(userId, id);
        return outcome == BookingOutcome.SessionNotFound ? NotFound() : NoContent();
    }
}
```

- [ ] **Step 2:** Χαλάρωσε το authorization στο `ClassSessionsController.cs`: άλλαξε το class-level attribute από `[Authorize(Policy = Policies.RequireInstructor)]` σε σκέτο `[Authorize]`, και **πρόσθεσε** `[Authorize(Policy = Policies.RequireInstructor)]` πάνω στις `Create` και `Cancel` (τα GET μένουν ανοιχτά σε κάθε authenticated):

```csharp
[ApiController]
[Route("class-sessions")]
[Authorize]
public class ClassSessionsController : ControllerBase
{
    // ... GetAll / GetById: χωρίς επιπλέον attribute (κάθε authenticated) ...

    [HttpPost]
    [Authorize(Policy = Policies.RequireInstructor)]
    public async Task<ActionResult<ClassSessionResponse>> Create(...) { ... }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.RequireInstructor)]
    public async Task<IActionResult> Cancel(...) { ... }
}
```

- [ ] **Step 3 (tests):** `BookingEndpointsTests.cs` — HTTP-level μέσω Postgres factory. Αντέγραψε τα helpers `CreateUserAsync`/`LoginAsync` (όπως στα Φ2 test files). Το session πρέπει να στηθεί στο **tenant του χρήστη** (πάρ' το από τον created user):

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class BookingEndpointsTests
{
    private readonly PostgresApiFactory _factory;

    public BookingEndpointsTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    // (Αντέγραψε CreateUserAsync + LoginAsync από τα Φ2 test files — ίδια υλοποίηση.)

    private async Task<Guid> SeedSessionForTenantAsync(Guid tenantId, int capacity)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ct = new ClassType { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Yoga", DefaultDurationMinutes = 60, DefaultCapacity = capacity, IsActive = true };
        var s = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = Guid.NewGuid(), StartsAt = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Capacity = capacity, IsActive = true };
        db.ClassTypes.Add(ct);
        db.ClassSessions.Add(s);
        await db.SaveChangesAsync();
        return s.Id;
    }

    [Fact]
    public async Task Member_books_session_then_it_appears_in_my_bookings()
    {
        var user = await CreateUserAsync("booker@demo.gym", "Test1234!", Roles.User);
        var sessionId = await SeedSessionForTenantAsync(user.TenantId, capacity: 5);

        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "booker@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookResponse = await client.PostAsJsonAsync("/bookings", new CreateBookingRequest(sessionId));
        Assert.Equal(HttpStatusCode.Created, bookResponse.StatusCode);

        var mine = await client.GetFromJsonAsync<List<BookingResponse>>("/bookings/me");
        Assert.Contains(mine!, b => b.ClassSessionId == sessionId && b.Status == "Confirmed");
    }

    [Fact]
    public async Task Booking_full_session_returns_409()
    {
        var user = await CreateUserAsync("booker-full@demo.gym", "Test1234!", Roles.User);
        var sessionId = await SeedSessionForTenantAsync(user.TenantId, capacity: 1);
        var other = await CreateUserAsync("booker-first@demo.gym", "Test1234!", Roles.User);

        // Ο "other" πρέπει να είναι στο ΙΔΙΟ tenant για να δει το session — απλούστερο: γέμισε το session
        // απευθείας μέσω του service σε scope του tenant.
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(user.TenantId);
            var svc = scope.ServiceProvider.GetRequiredService<GymBooking.Api.Services.BookingService>();
            await svc.BookAsync(Guid.NewGuid(), sessionId);
        }

        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "booker-full@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/bookings", new CreateBookingRequest(sessionId));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

- [ ] **Step 4:** Verify ΟΛΑ → PASS: `cd backend && dotnet test`
Expected: όλα PASS (Φ1/Φ2 InMemory + Φ3 Postgres).

- [ ] **Step 5: Commit** `git commit -am "feat: bookings endpoints + open session GET to authenticated users"`

### Task 32: Frontend — booking models + API service

**Files:**
- Modify: `frontend/libs/models/src/lib/models.ts`
- Create: `frontend/libs/data-access/src/lib/booking-api.service.ts`
- Modify: `frontend/libs/data-access/src/index.ts`

> `nvm use 20` πρώτα.

- [ ] **Step 1:** Πρόσθεσε στο `models.ts`:

```typescript
export interface Booking {
  id: string;
  classSessionId: string;
  classTypeName: string;
  startsAt: string; // ISO UTC
  status: string;   // 'Confirmed' | 'Cancelled'
  createdAt: string;
}

export interface CreateBookingRequest {
  classSessionId: string;
}
```

- [ ] **Step 2:** `booking-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Booking, CreateBookingRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class BookingApiService {
  private readonly http = inject(HttpClient);

  getMine(): Observable<Booking[]> {
    return this.http.get<Booking[]>('/bookings/me');
  }

  book(request: CreateBookingRequest): Observable<void> {
    return this.http.post<void>('/bookings', request);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`/bookings/${id}/cancel`, {});
  }
}
```

- [ ] **Step 3:** Πρόσθεσε στο `frontend/libs/data-access/src/index.ts`: `export * from './lib/booking-api.service';`

- [ ] **Step 4:** Verify: `cd frontend && npx nx run-many -t lint -p models data-access` → PASS.

- [ ] **Step 5: Commit** `git commit -am "feat(fe): booking models + API service"`

### Task 33: Frontend — customer app: book & cancel UI

**Files:**
- Create: `frontend/apps/customer/src/app/sessions/sessions.ts`
- Create: `frontend/apps/customer/src/app/sessions/sessions.html`
- Modify: `frontend/apps/customer/src/app/app.routes.ts`
- Modify: `frontend/apps/customer/src/app/dashboard/dashboard.ts` + `dashboard.html` (link — πρόσθεσε `RouterLink` στα imports όπως στη Φ2)

> Reuse το υπάρχον `ClassSessionApiService` (GET `/class-sessions`, πλέον ανοιχτό σε authenticated) + το νέο `BookingApiService`. Οι κρατήσεις μου φαίνονται στην ίδια σελίδα με cancel.

- [ ] **Step 1:** `sessions.ts` (standalone, Signals):

```typescript
import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { BookingApiService, ClassSessionApiService } from '@frontend/data-access';
import { Booking, ClassSession } from '@frontend/models';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [DatePipe, MatButtonModule, MatListModule],
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

- [ ] **Step 2:** `sessions.html`:

```html
<h1 class="text-2xl font-bold m-4">Διαθέσιμα μαθήματα</h1>

@if (message(); as m) {
  <p class="m-4 text-red-600">{{ m }}</p>
}

<mat-list class="m-4">
  @for (s of sessions(); track s.id) {
    <mat-list-item>
      {{ s.classTypeName }} — {{ s.startsAt | date: 'dd/MM HH:mm' }} —
      {{ s.bookedCount }}/{{ s.capacity }}
      <button mat-raised-button color="primary" class="ml-4"
              [disabled]="s.bookedCount >= s.capacity || s.isCancelled"
              (click)="book(s)">Κράτηση</button>
    </mat-list-item>
  } @empty {
    <p class="text-gray-500">Δεν υπάρχουν διαθέσιμα μαθήματα.</p>
  }
</mat-list>

<h2 class="text-xl font-bold m-4">Οι κρατήσεις μου</h2>
<mat-list class="m-4">
  @for (b of myBookings(); track b.id) {
    <mat-list-item>
      {{ b.classTypeName }} — {{ b.startsAt | date: 'dd/MM HH:mm' }} — {{ b.status }}
      @if (b.status === 'Confirmed') {
        <button mat-button color="warn" class="ml-4" (click)="cancel(b)">Ακύρωση</button>
      }
    </mat-list-item>
  } @empty {
    <p class="text-gray-500">Δεν έχεις κρατήσεις.</p>
  }
</mat-list>
```

- [ ] **Step 3:** Route στο customer `app.routes.ts` — import `Sessions`, πρόσθεσε πριν το wildcard:

```typescript
  { path: 'sessions', component: Sessions, canActivate: [authGuard] },
```

- [ ] **Step 4:** Link στο customer `dashboard.html`: `<a mat-button routerLink="/sessions">Μαθήματα</a>` (πρόσθεσε `RouterLink` στα imports του `Dashboard`).

- [ ] **Step 5:** Verify: `cd frontend && npx nx lint customer && npx nx build customer` → PASS. (Optional e2e με backend up: login ως member → `/sessions` → Κράτηση → εμφανίζεται στις κρατήσεις μου → Ακύρωση.)

- [ ] **Step 6: Commit** `git commit -am "feat(customer): book & cancel sessions UI"`

### Task 34: CI check + ενημέρωση progress

**Files:** Modify `CLAUDE.md`

- [ ] **Step 1:** Τρέξε ό,τι τρέχει το CI (Docker up για τα Testcontainers):

```bash
cd backend && dotnet build && dotnet test
cd ../frontend && npx nx run-many -t lint build
```
Expected: όλα PASS.

- [ ] **Step 2:** Στο `CLAUDE.md` → progress tracker: τσέκαρε `[x] Φ3 — Booking core (atomic) + tests` και ενημέρωσε το «Τώρα δουλεύω / Επόμενο» σε **Φ4 — Weekly schedule + φίλτρα + customer dashboard**.

- [ ] **Step 3: Commit** `git commit -am "docs: mark Phase 3 complete"`

---

## PHASE 4: Weekly schedule + φίλτρα + customer dashboard (~12h)

> Κυρίως customer-facing. Το backend query είναι απλό read (χωρίς transactions/locks), οπότε τα backend tests τρέχουν στο **Postgres harness** (Φ3) για ρεαλιστικά dates/joins. Ακολουθεί τα Φ1–Φ3 patterns.

### Αποφάσεις Φάσης 4 (κλειδωμένες)
- **Νέο `GET /schedule` endpoint** _(2026-07-09)_ (ξεχωριστό από το instructor-facing `GET /class-sessions`): επιστρέφει τα sessions μιας εβδομάδας, εμπλουτισμένα για τον **τρέχοντα χρήστη**.
- **Week range = client-driven, tz-agnostic server** _(2026-07-09)_: ο client στέλνει `from`/`to` ως **UTC ISO** (υπολογίζει Δευτέρα 00:00 τοπικής ώρας → UTC). Ο controller τα δέχεται ως `DateTimeOffset` και περνά `.UtcDateTime` — αποφεύγει το `DateTime.Kind` gotcha του Npgsql (timestamptz απαιτεί Utc).
- **`IsBookedByMe` + `MyBookingId` flags στο response** _(2026-07-09)_: το service κάνει lookup τις confirmed κρατήσεις του χρήστη· ο customer βλέπει «Κράτηση» ή «Ακύρωση» απευθείας από το πρόγραμμα.
- **Filter options από backend** _(2026-07-09)_: reuse `GET /class-types` (**χαλαρώνουμε** το authorization του σε `[Authorize]` — writes μένουν `RequireInstructor`) + νέο `GET /instructors`. Πλήρεις λίστες ακόμα κι αν μια εβδομάδα δεν έχει sessions.
- **Dashboard split = client-side** _(2026-07-09)_: το `/bookings/me` επιστρέφει τα πάντα· ο customer χωρίζει σε «επερχόμενες» (Confirmed & StartsAt ≥ τώρα) και «ιστορικό». Καμία νέα endpoint.
- **Το weekly schedule αντικαθιστά** τη flat `/sessions` σελίδα της Φ3 στο customer (superseded) — τη διαγράφουμε.

### Task 35: Schedule service + endpoint (backend)

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/ScheduleContracts.cs`
- Create: `backend/src/GymBooking.Api/Services/ScheduleService.cs`
- Create: `backend/src/GymBooking.Api/Controllers/ScheduleController.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs` (DI)
- Test: `backend/tests/GymBooking.Tests/ScheduleTests.cs`

- [ ] **Step 1:** Contract:

```csharp
namespace GymBooking.Core.Contracts;

public record ScheduleSessionResponse(
    Guid Id,
    Guid ClassTypeId,
    string ClassTypeName,
    Guid InstructorId,
    string InstructorName,
    DateTime StartsAt,
    int DurationMinutes,
    int Capacity,
    int BookedCount,
    bool IsBookedByMe,
    Guid? MyBookingId);
```

- [ ] **Step 2:** `ScheduleService.cs` (κρύβει cancelled/inactive· εμπλουτισμός με τις κρατήσεις μου):

```csharp
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Enums;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class ScheduleService
{
    private readonly AppDbContext _dbContext;

    public ScheduleService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ScheduleSessionResponse>> GetScheduleAsync(
        Guid userId, DateTime fromUtc, DateTime toUtc, Guid? classTypeId, Guid? instructorId)
    {
        var query =
            from s in _dbContext.ClassSessions
            where s.IsActive && !s.IsCancelled && s.StartsAt >= fromUtc && s.StartsAt < toUtc
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            join u in _dbContext.Users on s.InstructorId equals u.Id
            select new { s, ClassTypeName = ct.Name, InstructorName = u.FirstName + " " + u.LastName };

        if (classTypeId.HasValue)
        {
            query = query.Where(x => x.s.ClassTypeId == classTypeId.Value);
        }
        if (instructorId.HasValue)
        {
            query = query.Where(x => x.s.InstructorId == instructorId.Value);
        }

        var rows = await query.OrderBy(x => x.s.StartsAt).ToListAsync();

        var mine = (await _dbContext.Bookings
                .Where(b => b.UserId == userId && b.Status == BookingStatus.Confirmed)
                .Select(b => new { b.Id, b.ClassSessionId })
                .ToListAsync())
            .ToDictionary(b => b.ClassSessionId, b => b.Id);

        return rows.Select(x => new ScheduleSessionResponse(
            x.s.Id,
            x.s.ClassTypeId,
            x.ClassTypeName,
            x.s.InstructorId,
            x.InstructorName,
            x.s.StartsAt,
            x.s.DurationMinutes,
            x.s.Capacity,
            x.s.BookedCount,
            mine.ContainsKey(x.s.Id),
            mine.TryGetValue(x.s.Id, out var bid) ? bid : (Guid?)null))
            .ToList();
    }
}
```

- [ ] **Step 3:** `ScheduleController.cs` (`DateTimeOffset` για ασφαλές UTC binding):

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("schedule")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly ScheduleService _service;

    public ScheduleController(ScheduleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleSessionResponse>>> Get(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? classTypeId,
        [FromQuery] Guid? instructorId)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var result = await _service.GetScheduleAsync(userId, from.UtcDateTime, to.UtcDateTime, classTypeId, instructorId);
        return Ok(result);
    }
}
```

- [ ] **Step 4:** DI στο `Program.cs`: `builder.Services.AddScoped<ScheduleService>();`

- [ ] **Step 5 (tests):** `ScheduleTests.cs` (Postgres collection):

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class ScheduleTests
{
    private readonly PostgresApiFactory _factory;

    public ScheduleTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Seeded(Guid TenantId, Guid ClassTypeId, Guid InstructorId, Guid SessionThisWeek, Guid SessionNextWeek);

    private async Task<Seeded> SeedAsync(DateTime weekStartUtc)
    {
        var tenantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var instructor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = $"instr-{tenantId}@demo.gym",
            Email = $"instr-{tenantId}@demo.gym",
            FirstName = "Maria",
            LastName = "Papadopoulou",
        };
        var ct = new ClassType { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Yoga", DefaultDurationMinutes = 60, DefaultCapacity = 10, IsActive = true };
        var s1 = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = instructor.Id, StartsAt = weekStartUtc.AddDays(1).AddHours(18), DurationMinutes = 60, Capacity = 10, IsActive = true };
        var s2 = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = instructor.Id, StartsAt = weekStartUtc.AddDays(8).AddHours(18), DurationMinutes = 60, Capacity = 10, IsActive = true };

        db.Users.Add(instructor);
        db.ClassTypes.Add(ct);
        db.ClassSessions.AddRange(s1, s2);
        await db.SaveChangesAsync();

        return new Seeded(tenantId, ct.Id, instructor.Id, s1.Id, s2.Id);
    }

    private async Task<List<ScheduleSessionResponse>> GetScheduleAsync(
        Guid tenantId, Guid userId, DateTime fromUtc, DateTime toUtc, Guid? classTypeId = null)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var svc = scope.ServiceProvider.GetRequiredService<ScheduleService>();
        return await svc.GetScheduleAsync(userId, fromUtc, toUtc, classTypeId, null);
    }

    [Fact]
    public async Task Schedule_returns_only_this_week_with_booked_flag()
    {
        var weekStart = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);
        var seeded = await SeedAsync(weekStart);
        var userId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(seeded.TenantId);
            var booking = scope.ServiceProvider.GetRequiredService<BookingService>();
            await booking.BookAsync(userId, seeded.SessionThisWeek);
        }

        var result = await GetScheduleAsync(seeded.TenantId, userId, weekStart, weekStart.AddDays(7));

        Assert.Single(result); // το session της επόμενης εβδομάδας εξαιρείται
        Assert.Equal(seeded.SessionThisWeek, result[0].Id);
        Assert.True(result[0].IsBookedByMe);
        Assert.NotNull(result[0].MyBookingId);
        Assert.Equal("Maria Papadopoulou", result[0].InstructorName);
    }

    [Fact]
    public async Task Schedule_filters_by_class_type()
    {
        var weekStart = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);
        var seeded = await SeedAsync(weekStart);

        var none = await GetScheduleAsync(seeded.TenantId, Guid.NewGuid(), weekStart, weekStart.AddDays(7), classTypeId: Guid.NewGuid());
        Assert.Empty(none);

        var some = await GetScheduleAsync(seeded.TenantId, Guid.NewGuid(), weekStart, weekStart.AddDays(7), classTypeId: seeded.ClassTypeId);
        Assert.Single(some);
    }
}
```

- [ ] **Step 6:** Verify → PASS: `cd backend && dotnet test --filter "FullyQualifiedName~ScheduleTests"`

- [ ] **Step 7: Commit** `git commit -am "feat: GET /schedule (weekly, filtered, booked-by-me flag)"`

### Task 36: GET /instructors + άνοιγμα του class-types GET

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/InstructorContracts.cs`
- Create: `backend/src/GymBooking.Api/Controllers/InstructorsController.cs`
- Modify: `backend/src/GymBooking.Api/Controllers/ClassTypesController.cs`
- Test: `backend/tests/GymBooking.Tests/InstructorsTests.cs`

- [ ] **Step 1:** Contract:

```csharp
namespace GymBooking.Core.Contracts;

public record InstructorResponse(Guid Id, string Name);
```

- [ ] **Step 2:** `InstructorsController.cs` (`GetUsersInRoleAsync` σέβεται το tenant query filter μέσω του CurrentTenant που έθεσε το middleware):

```csharp
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("instructors")]
[Authorize]
public class InstructorsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public InstructorsController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InstructorResponse>>> GetAll()
    {
        var instructors = await _userManager.GetUsersInRoleAsync(Roles.Instructor);
        return Ok(instructors
            .OrderBy(u => u.LastName)
            .Select(u => new InstructorResponse(u.Id, u.FirstName + " " + u.LastName)));
    }
}
```

- [ ] **Step 3:** Χαλάρωσε το `ClassTypesController.cs`: άλλαξε το class-level `[Authorize(Policy = Policies.RequireInstructor)]` σε σκέτο `[Authorize]`, και βάλε `[Authorize(Policy = Policies.RequireInstructor)]` πάνω στις `Create`, `Update`, `Delete` (τα GET ανοιχτά σε κάθε authenticated — ο customer τα θέλει για το filter dropdown).

- [ ] **Step 4 (tests):** `InstructorsTests.cs`. Χρειάζεται caller + instructor στο **ίδιο** tenant (το κοινό `CreateUserAsync` βάζει τυχαίο TenantId ανά χρήστη), οπότε ο helper εδώ δέχεται ρητό tenantId:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class InstructorsTests
{
    private readonly PostgresApiFactory _factory;

    public InstructorsTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task CreateUserInTenantAsync(Guid tenantId, string email, string password, params string[] roles)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = email,
            Email = email,
            FirstName = "First",
            LastName = email.Split('@')[0],
            EmailConfirmed = true,
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        foreach (var role in roles)
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task Instructors_list_contains_instructors_not_plain_members()
    {
        var tenantId = Guid.NewGuid();
        await CreateUserInTenantAsync(tenantId, "the-instr@demo.gym", "Test1234!", Roles.Instructor);
        await CreateUserInTenantAsync(tenantId, "the-member@demo.gym", "Test1234!", Roles.User);

        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "the-member@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var list = await client.GetFromJsonAsync<List<InstructorResponse>>("/instructors");

        Assert.Contains(list!, i => i.Name.Contains("the-instr"));
        Assert.DoesNotContain(list!, i => i.Name.Contains("the-member"));
    }
}
```

- [ ] **Step 5:** Verify ΟΛΑ → PASS: `cd backend && dotnet test`

- [ ] **Step 6: Commit** `git commit -am "feat: GET /instructors + open class-types GET to authenticated"`

### Task 37: Frontend — schedule/instructor models + API services

**Files:**
- Modify: `frontend/libs/models/src/lib/models.ts`
- Create: `frontend/libs/data-access/src/lib/schedule-api.service.ts`
- Create: `frontend/libs/data-access/src/lib/instructor-api.service.ts`
- Modify: `frontend/libs/data-access/src/index.ts`

> `nvm use 20`.

- [ ] **Step 1:** Πρόσθεσε στο `models.ts`:

```typescript
export interface ScheduleSession {
  id: string;
  classTypeId: string;
  classTypeName: string;
  instructorId: string;
  instructorName: string;
  startsAt: string; // ISO UTC
  durationMinutes: number;
  capacity: number;
  bookedCount: number;
  isBookedByMe: boolean;
  myBookingId: string | null;
}

export interface Instructor {
  id: string;
  name: string;
}

export interface ScheduleQuery {
  from: string; // ISO UTC
  to: string;   // ISO UTC
  classTypeId?: string;
  instructorId?: string;
}
```

- [ ] **Step 2:** `schedule-api.service.ts`:

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ScheduleQuery, ScheduleSession } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class ScheduleApiService {
  private readonly http = inject(HttpClient);

  getSchedule(query: ScheduleQuery): Observable<ScheduleSession[]> {
    let params = new HttpParams().set('from', query.from).set('to', query.to);
    if (query.classTypeId) {
      params = params.set('classTypeId', query.classTypeId);
    }
    if (query.instructorId) {
      params = params.set('instructorId', query.instructorId);
    }
    return this.http.get<ScheduleSession[]>('/schedule', { params });
  }
}
```

- [ ] **Step 3:** `instructor-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Instructor } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class InstructorApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<Instructor[]> {
    return this.http.get<Instructor[]>('/instructors');
  }
}
```

- [ ] **Step 4:** Πρόσθεσε στο `data-access/src/index.ts`:

```typescript
export * from './lib/schedule-api.service';
export * from './lib/instructor-api.service';
```

- [ ] **Step 5:** Verify: `cd frontend && npx nx run-many -t lint -p models data-access` → PASS.

- [ ] **Step 6: Commit** `git commit -am "feat(fe): schedule/instructor models + API services"`

### Task 38: customer app — weekly schedule σελίδα

**Files:**
- Create: `frontend/apps/customer/src/app/schedule/schedule.ts`
- Create: `frontend/apps/customer/src/app/schedule/schedule.html`
- Modify: `frontend/apps/customer/src/app/app.routes.ts`
- Delete: `frontend/apps/customer/src/app/sessions/sessions.ts` + `sessions.html` (superseded από το schedule)

- [ ] **Step 1:** `schedule.ts` (Signals· εβδομάδα ξεκινά Δευτέρα τοπικής ώρας· στέλνει UTC):

```typescript
import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatListModule } from '@angular/material/list';
import { MatSelectModule } from '@angular/material/select';
import {
  BookingApiService,
  ClassTypeApiService,
  InstructorApiService,
  ScheduleApiService,
} from '@frontend/data-access';
import { ClassType, Instructor, ScheduleSession } from '@frontend/models';

@Component({
  selector: 'app-schedule',
  standalone: true,
  imports: [DatePipe, FormsModule, MatButtonModule, MatFormFieldModule, MatListModule, MatSelectModule],
  templateUrl: './schedule.html',
})
export class Schedule {
  private readonly scheduleApi = inject(ScheduleApiService);
  private readonly bookingApi = inject(BookingApiService);
  private readonly classTypeApi = inject(ClassTypeApiService);
  private readonly instructorApi = inject(InstructorApiService);

  protected readonly weekStart = signal(this.mondayOf(new Date()));
  protected readonly sessions = signal<ScheduleSession[]>([]);
  protected readonly classTypes = signal<ClassType[]>([]);
  protected readonly instructors = signal<Instructor[]>([]);
  protected readonly classTypeId = signal<string>('');
  protected readonly instructorId = signal<string>('');
  protected readonly message = signal<string | null>(null);

  protected readonly weekEnd = computed(() => {
    const d = new Date(this.weekStart());
    d.setDate(d.getDate() + 6);
    return d;
  });

  constructor() {
    this.classTypeApi.getAll().subscribe((x) => this.classTypes.set(x));
    this.instructorApi.getAll().subscribe((x) => this.instructors.set(x));
    this.load();
  }

  private mondayOf(date: Date): Date {
    const d = new Date(date);
    d.setHours(0, 0, 0, 0);
    const dayFromMonday = (d.getDay() + 6) % 7; // Κυρ=6 ... Δευ=0
    d.setDate(d.getDate() - dayFromMonday);
    return d;
  }

  private load(): void {
    const from = this.weekStart();
    const to = new Date(from);
    to.setDate(to.getDate() + 7);
    this.scheduleApi
      .getSchedule({
        from: from.toISOString(),
        to: to.toISOString(),
        classTypeId: this.classTypeId() || undefined,
        instructorId: this.instructorId() || undefined,
      })
      .subscribe((x) => this.sessions.set(x));
  }

  changeWeek(deltaDays: number): void {
    const d = new Date(this.weekStart());
    d.setDate(d.getDate() + deltaDays);
    this.weekStart.set(d);
    this.load();
  }

  book(session: ScheduleSession): void {
    this.message.set(null);
    this.bookingApi.book({ classSessionId: session.id }).subscribe({
      next: () => this.load(),
      error: (err) => this.message.set(err?.error ?? 'Η κράτηση απέτυχε.'),
    });
  }

  cancel(session: ScheduleSession): void {
    if (!session.myBookingId) {
      return;
    }
    this.bookingApi.cancel(session.myBookingId).subscribe({ next: () => this.load() });
  }
}
```

- [ ] **Step 2:** `schedule.html` (mobile-first· `[ngModel]`+`(ngModelChange)` για signals):

```html
<div class="p-4">
  <div class="flex items-center gap-2 mb-4">
    <button mat-button (click)="changeWeek(-7)">‹ Προηγ.</button>
    <span class="font-bold">{{ weekStart() | date: 'dd/MM' }} – {{ weekEnd() | date: 'dd/MM' }}</span>
    <button mat-button (click)="changeWeek(7)">Επόμ. ›</button>
  </div>

  <div class="flex gap-4 mb-4 flex-wrap">
    <mat-form-field>
      <mat-label>Είδος</mat-label>
      <mat-select [ngModel]="classTypeId()" (ngModelChange)="classTypeId.set($event); load()">
        <mat-option value="">Όλα</mat-option>
        @for (ct of classTypes(); track ct.id) {
          <mat-option [value]="ct.id">{{ ct.name }}</mat-option>
        }
      </mat-select>
    </mat-form-field>
    <mat-form-field>
      <mat-label>Προπονητής</mat-label>
      <mat-select [ngModel]="instructorId()" (ngModelChange)="instructorId.set($event); load()">
        <mat-option value="">Όλοι</mat-option>
        @for (i of instructors(); track i.id) {
          <mat-option [value]="i.id">{{ i.name }}</mat-option>
        }
      </mat-select>
    </mat-form-field>
  </div>

  @if (message(); as m) { <p class="text-red-600 mb-4">{{ m }}</p> }

  <mat-list>
    @for (s of sessions(); track s.id) {
      <mat-list-item>
        <div class="flex items-center justify-between w-full gap-2">
          <span>{{ s.startsAt | date: 'EEE dd/MM HH:mm' }} — {{ s.classTypeName }} — {{ s.instructorName }} — {{ s.bookedCount }}/{{ s.capacity }}</span>
          @if (s.isBookedByMe) {
            <button mat-button color="warn" (click)="cancel(s)">Ακύρωση</button>
          } @else {
            <button mat-raised-button color="primary" [disabled]="s.bookedCount >= s.capacity" (click)="book(s)">Κράτηση</button>
          }
        </div>
      </mat-list-item>
    } @empty {
      <p class="text-gray-500">Δεν υπάρχουν μαθήματα αυτή την εβδομάδα.</p>
    }
  </mat-list>
</div>
```
> Το `load()` πρέπει να είναι καλέσιμο από το template — άλλαξε το `private load()` σε `protected load()` στο `schedule.ts`.

- [ ] **Step 3:** Στο customer `app.routes.ts`: αφαίρεσε το import + route του `Sessions`, πρόσθεσε το `Schedule`:

```typescript
import { Schedule } from './schedule/schedule';
```
```typescript
  { path: 'schedule', component: Schedule, canActivate: [authGuard] },
```

- [ ] **Step 4:** Διάγραψε τον φάκελο `frontend/apps/customer/src/app/sessions/` (Φ3 flat list — superseded).

- [ ] **Step 5:** Verify: `cd frontend && npx nx lint customer && npx nx build customer` → PASS.

- [ ] **Step 6: Commit** `git commit -am "feat(customer): weekly schedule with filters"`

### Task 39: customer dashboard — επερχόμενες + ιστορικό

**Files:**
- Modify: `frontend/apps/customer/src/app/dashboard/dashboard.ts`
- Modify: `frontend/apps/customer/src/app/dashboard/dashboard.html`

- [ ] **Step 1:** `dashboard.ts` (split client-side από το `/bookings/me`):

```typescript
import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '@frontend/auth';
import { BookingApiService } from '@frontend/data-access';
import { Booking } from '@frontend/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, MatButtonModule, MatListModule, RouterLink],
  templateUrl: './dashboard.html',
})
export class Dashboard {
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly bookingApi = inject(BookingApiService);

  private readonly bookings = signal<Booking[]>([]);

  protected readonly upcoming = computed(() =>
    this.bookings().filter((b) => b.status === 'Confirmed' && new Date(b.startsAt).getTime() >= Date.now()),
  );
  protected readonly history = computed(() =>
    this.bookings().filter((b) => b.status !== 'Confirmed' || new Date(b.startsAt).getTime() < Date.now()),
  );

  constructor() {
    this.bookingApi.getMine().subscribe((x) => this.bookings.set(x));
  }

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
```

- [ ] **Step 2:** `dashboard.html`:

```html
<div class="p-4">
  <div class="flex items-center justify-between mb-4">
    <a mat-raised-button color="primary" routerLink="/schedule">Πρόγραμμα μαθημάτων</a>
    <button mat-button (click)="logout()">Αποσύνδεση</button>
  </div>

  <h2 class="text-xl font-bold mb-2">Επερχόμενες κρατήσεις</h2>
  <mat-list>
    @for (b of upcoming(); track b.id) {
      <mat-list-item>{{ b.classTypeName }} — {{ b.startsAt | date: 'EEE dd/MM HH:mm' }}</mat-list-item>
    } @empty {
      <p class="text-gray-500">Καμία επερχόμενη κράτηση.</p>
    }
  </mat-list>

  <h2 class="text-xl font-bold mt-6 mb-2">Ιστορικό</h2>
  <mat-list>
    @for (b of history(); track b.id) {
      <mat-list-item>{{ b.classTypeName }} — {{ b.startsAt | date: 'dd/MM/yyyy HH:mm' }} — {{ b.status }}</mat-list-item>
    } @empty {
      <p class="text-gray-500">Κενό ιστορικό.</p>
    }
  </mat-list>
</div>
```

- [ ] **Step 3:** Verify: `cd frontend && npx nx lint customer && npx nx build customer` → PASS. (Optional e2e με backend up: login member → dashboard δείχνει επερχόμενες/ιστορικό· link → schedule → κράτηση → επιστροφή στο dashboard την δείχνει στις επερχόμενες.)

- [ ] **Step 4: Commit** `git commit -am "feat(customer): dashboard with upcoming + history"`

### Task 40: CI check + ενημέρωση progress

**Files:** Modify `CLAUDE.md`

- [ ] **Step 1:** Τρέξε ό,τι τρέχει το CI (Docker up για Testcontainers):

```bash
cd backend && dotnet build && dotnet test
cd ../frontend && npx nx run-many -t lint build
```
Expected: όλα PASS.

- [ ] **Step 2:** Στο `CLAUDE.md` → progress tracker: τσέκαρε `[x] Φ4 — Weekly schedule + φίλτρα + customer dashboard` και ενημέρωσε «Τώρα δουλεύω / Επόμενο» σε **Φ5 — Subscriptions/plans + consumption**.

- [ ] **Step 3: Commit** `git commit -am "docs: mark Phase 4 complete"`

---

## PHASE 5: Subscriptions/plans + consumption (~12h)

> Επεκτείνει τον atomic πυρήνα της Φ3: το booking **καταναλώνει** συνδρομή μέσα στο ίδιο transaction. Admin-facing (plans + ανάθεση) στο `staff`, view στο `customer`.

### Αποφάσεις Φάσης 5 (κλειδωμένες)
- **Συνδρομή υποχρεωτική για booking** _(2026-07-09)_: χωρίς χρήσιμη συνδρομή → νέο outcome `NoSubscription`. Χρήσιμη = `Status=Active` & `ValidFrom ≤ τώρα ≤ ValidTo` & (`RemainingSessions == null` (Unlimited) ή `> 0`). **Συνεπάγεται:** ενημέρωση των booking call-sites στα Φ3/Φ4 tests ώστε να σπέρνουν συνδρομή (Task 44).
- **Μία ενεργή συνδρομή/χρήστη** _(2026-07-09)_: η ανάθεση ακυρώνει (Status=Cancelled) τυχόν προηγούμενη active. Το booking διαβάζει τη μοναδική active.
- **`Booking.SubscriptionId` (nullable)** _(2026-07-09)_: το booking καταγράφει ποια συνδρομή κατανάλωσε → ακριβές refund. Νέα στήλη (migration).
- **Consumption/refund driven από `RemainingSessions` null-ness** _(2026-07-09)_: `null` ⇒ Unlimited ⇒ κανένα decrement/refund· non-null ⇒ SessionPack ⇒ `-1` στο book, `+1` στο cancel. Δεν χρειάζεται lookup του PlanType στη ροή booking.
- **Subscription row lock (`FOR UPDATE`)** _(2026-07-09)_ μέσα στο transaction: δύο ταυτόχρονες κρατήσεις του ίδιου χρήστη δεν καταναλώνουν 2× την τελευταία προπόνηση.
- **Ανάθεση by email** _(2026-07-09)_: `POST /subscriptions {email, planId}` — ο server βρίσκει τον χρήστη στο tenant. Αποφεύγει users-list endpoint τώρα (πλήρες user management = Φ7).
- **Assignment dates = server-computed** _(2026-07-09)_: `ValidFrom = τώρα`, `ValidTo = τώρα + plan.DurationDays`· `RemainingSessions = SessionPack ? plan.SessionsCount : null`.
- **Cancel refund πάντα** στη Φ5 (η χρονική cancellation-policy μπαίνει Φ6).

### Task 41: MembershipPlan + Subscription entities + Booking.SubscriptionId + migration

**Files:**
- Create: `backend/src/GymBooking.Core/Entities/Enums/PlanType.cs`
- Create: `backend/src/GymBooking.Core/Entities/Enums/SubscriptionStatus.cs`
- Create: `backend/src/GymBooking.Core/Entities/Models/MembershipPlan.cs`
- Create: `backend/src/GymBooking.Core/Entities/Models/Subscription.cs`
- Modify: `backend/src/GymBooking.Core/Entities/Models/Booking.cs`
- Modify: `backend/src/GymBooking.Data/AppDbContext.cs`
- Test: `backend/tests/GymBooking.Tests/TenantIsolationTests.cs`

- [ ] **Step 1:** Enums:

```csharp
namespace GymBooking.Core.Entities.Enums;

public enum PlanType
{
    SessionPack,   // = 0
    Unlimited,     // = 1
}
```
```csharp
namespace GymBooking.Core.Entities.Enums;

public enum SubscriptionStatus
{
    Active,      // = 0
    Cancelled,   // = 1
}
```

- [ ] **Step 2:** `MembershipPlan.cs`:

```csharp
using GymBooking.Core.Entities.Enums;

namespace GymBooking.Core.Entities.Models;

public class MembershipPlan
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlanType Type { get; set; }
    public int SessionsCount { get; set; }   // σχετικό μόνο για SessionPack
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
```

- [ ] **Step 3:** `Subscription.cs`:

```csharp
using GymBooking.Core.Entities.Enums;

namespace GymBooking.Core.Entities.Models;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid MembershipPlanId { get; set; }
    public int? RemainingSessions { get; set; }   // null για Unlimited
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
}
```

- [ ] **Step 4:** Στο `Booking.cs` πρόσθεσε: `public Guid? SubscriptionId { get; set; }`

- [ ] **Step 5:** Στο `AppDbContext.cs`: DbSets + query filters:

```csharp
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
```
```csharp
        modelBuilder.Entity<MembershipPlan>().HasQueryFilter(p => p.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<Subscription>().HasQueryFilter(s => s.TenantId == _currentTenant.TenantId);
```

- [ ] **Step 6 (test):** Πρόσθεσε στο `TenantIsolationTests.cs`:

```csharp
    [Fact]
    public void Subscriptions_query_returns_only_current_tenant_rows()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var seedContext = CreateContext(dbName, new FakeCurrentTenant(tenantA)))
        {
            seedContext.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(), TenantId = tenantA, UserId = Guid.NewGuid(),
                MembershipPlanId = Guid.NewGuid(), ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddDays(30),
            });
            seedContext.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(), TenantId = tenantB, UserId = Guid.NewGuid(),
                MembershipPlanId = Guid.NewGuid(), ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddDays(30),
            });
            seedContext.SaveChanges();
        }

        using var queryContext = CreateContext(dbName, new FakeCurrentTenant(tenantA));
        var results = queryContext.Subscriptions.ToList();

        Assert.Single(results);
        Assert.Equal(tenantA, results[0].TenantId);
    }
```

- [ ] **Step 7:** Verify: `cd backend && dotnet test --filter "FullyQualifiedName~TenantIsolationTests"` → PASS.

- [ ] **Step 8:** Migration + apply (DB up):

```bash
cd backend
dotnet ef migrations add AddSubscriptions -p src/GymBooking.Data -s src/GymBooking.Api
dotnet ef database update -p src/GymBooking.Data -s src/GymBooking.Api
```
Expected: πίνακες `MembershipPlans`, `Subscriptions` + στήλη `SubscriptionId` στο `Bookings`.

- [ ] **Step 9: Commit** `git add -A && git commit -m "feat: MembershipPlan/Subscription entities + Booking.SubscriptionId"`

### Task 42: MembershipPlan CRUD (admin)

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/MembershipPlanContracts.cs`
- Create: `backend/src/GymBooking.Api/Services/MembershipPlanService.cs`
- Create: `backend/src/GymBooking.Api/Controllers/MembershipPlansController.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs` (DI)
- Test: `backend/tests/GymBooking.Tests/MembershipPlansTests.cs`

- [ ] **Step 1:** Contracts:

```csharp
namespace GymBooking.Core.Contracts;

public record CreatePlanRequest(string Name, string Type, int SessionsCount, int DurationDays, decimal Price);
public record PlanResponse(Guid Id, string Name, string Type, int SessionsCount, int DurationDays, decimal Price, bool IsActive);
```

- [ ] **Step 2:** `MembershipPlanService.cs`:

```csharp
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class MembershipPlanService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public MembershipPlanService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    /// <summary>Επιστρέφει null αν το Type δεν είναι έγκυρο ("SessionPack"/"Unlimited").</summary>
    public async Task<MembershipPlan?> CreateAsync(string name, string type, int sessionsCount, int durationDays, decimal price)
    {
        if (!Enum.TryParse<PlanType>(type, out var planType))
        {
            return null;
        }

        var plan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            Name = name,
            Type = planType,
            SessionsCount = sessionsCount,
            DurationDays = durationDays,
            Price = price,
            IsActive = true,
        };
        _dbContext.MembershipPlans.Add(plan);
        await _dbContext.SaveChangesAsync();
        return plan;
    }

    public async Task<List<MembershipPlan>> GetAllAsync() =>
        await _dbContext.MembershipPlans.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var plan = await _dbContext.MembershipPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null)
        {
            return false;
        }
        plan.IsActive = false;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
```

- [ ] **Step 3:** `MembershipPlansController.cs` (Admin only):

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("membership-plans")]
[Authorize(Policy = Policies.RequireAdmin)]
public class MembershipPlansController : ControllerBase
{
    private readonly MembershipPlanService _service;

    public MembershipPlansController(MembershipPlanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanResponse>>> GetAll()
    {
        var plans = await _service.GetAllAsync();
        return Ok(plans.Select(ToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<PlanResponse>> Create([FromBody] CreatePlanRequest request)
    {
        var created = await _service.CreateAsync(request.Name, request.Type, request.SessionsCount, request.DurationDays, request.Price);
        if (created is null)
        {
            return BadRequest($"Type must be '{nameof(GymBooking.Core.Entities.Enums.PlanType.SessionPack)}' or '{nameof(GymBooking.Core.Entities.Enums.PlanType.Unlimited)}'.");
        }
        return CreatedAtAction(nameof(GetAll), null, ToResponse(created));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeactivateAsync(id);
        return ok ? NoContent() : NotFound();
    }

    private static PlanResponse ToResponse(MembershipPlan p) =>
        new(p.Id, p.Name, p.Type.ToString(), p.SessionsCount, p.DurationDays, p.Price, p.IsActive);
}
```

- [ ] **Step 4:** DI: `builder.Services.AddScoped<MembershipPlanService>();`

- [ ] **Step 5 (tests):** `MembershipPlansTests.cs` (InMemory `TestApiFactory` — χωρίς transactions). Αντέγραψε `CreateUserAsync`/`LoginAsync` (Φ2). Test: non-admin → 403· admin create SessionPack → appears:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class MembershipPlansTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public MembershipPlansTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    // (Αντέγραψε CreateUserAsync + LoginAsync από τα Φ2 test files.)

    [Fact]
    public async Task CreatePlan_as_non_admin_returns_403()
    {
        await CreateUserAsync("plan-instr@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "plan-instr@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/membership-plans",
            new CreatePlanRequest("10-pack", "SessionPack", 10, 60, 80m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlan_as_admin_then_appears_in_list()
    {
        await CreateUserAsync("plan-admin@demo.gym", "Test1234!", Roles.Admin);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "plan-admin@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/membership-plans",
            new CreatePlanRequest("Unlimited Monthly", "Unlimited", 0, 30, 50m));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await client.GetFromJsonAsync<List<PlanResponse>>("/membership-plans");
        Assert.Contains(list!, p => p.Name == "Unlimited Monthly" && p.Type == "Unlimited");
    }
}
```

- [ ] **Step 6:** Verify: `cd backend && dotnet test --filter "FullyQualifiedName~MembershipPlansTests"` → PASS.

- [ ] **Step 7: Commit** `git commit -am "feat: MembershipPlan CRUD (admin) + tests"`

### Task 43: SubscriptionService (assign + view)

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/SubscriptionContracts.cs`
- Create: `backend/src/GymBooking.Api/Services/SubscriptionService.cs`
- Create: `backend/src/GymBooking.Api/Controllers/SubscriptionsController.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs` (DI)
- Test: `backend/tests/GymBooking.Tests/SubscriptionsTests.cs`

- [ ] **Step 1:** Contracts:

```csharp
namespace GymBooking.Core.Contracts;

public record AssignSubscriptionRequest(string Email, Guid PlanId);
public record SubscriptionResponse(Guid Id, string PlanName, string Type, int? RemainingSessions, DateTime ValidFrom, DateTime ValidTo);
```

- [ ] **Step 2:** `SubscriptionService.cs` (η ανάθεση ακυρώνει προηγούμενη active — «μία τη φορά»):

```csharp
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class SubscriptionService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public SubscriptionService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<(bool Ok, string? Error)> AssignAsync(string email, Guid planId)
    {
        // Identity default normalizer = ToUpperInvariant· ο έλεγχος tenant γίνεται από το query filter.
        var normalizedEmail = email.ToUpperInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (user is null)
        {
            return (false, "User not found in this tenant.");
        }

        var plan = await _dbContext.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
        if (plan is null)
        {
            return (false, "Plan not found.");
        }

        // "Μία ενεργή τη φορά": ακύρωσε τυχόν προηγούμενες active.
        var active = await _dbContext.Subscriptions
            .Where(s => s.UserId == user.Id && s.Status == SubscriptionStatus.Active)
            .ToListAsync();
        foreach (var s in active)
        {
            s.Status = SubscriptionStatus.Cancelled;
        }

        var now = DateTime.UtcNow;
        _dbContext.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            UserId = user.Id,
            MembershipPlanId = plan.Id,
            RemainingSessions = plan.Type == PlanType.SessionPack ? plan.SessionsCount : (int?)null,
            ValidFrom = now,
            ValidTo = now.AddDays(plan.DurationDays),
            Status = SubscriptionStatus.Active,
        });
        await _dbContext.SaveChangesAsync();
        return (true, null);
    }

    public async Task<SubscriptionResponse?> GetActiveForUserAsync(Guid userId)
    {
        return await (
            from s in _dbContext.Subscriptions
            where s.UserId == userId && s.Status == SubscriptionStatus.Active
            join p in _dbContext.MembershipPlans on s.MembershipPlanId equals p.Id
            orderby s.ValidFrom descending
            select new SubscriptionResponse(s.Id, p.Name, p.Type.ToString(), s.RemainingSessions, s.ValidFrom, s.ValidTo))
            .FirstOrDefaultAsync();
    }
}
```

- [ ] **Step 3:** `SubscriptionsController.cs`:

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionService _service;

    public SubscriptionsController(SubscriptionService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<ActionResult<SubscriptionResponse?>> GetMine()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        return Ok(await _service.GetActiveForUserAsync(userId));
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Assign([FromBody] AssignSubscriptionRequest request)
    {
        var (ok, error) = await _service.AssignAsync(request.Email, request.PlanId);
        return ok ? NoContent() : BadRequest(error);
    }
}
```

- [ ] **Step 4:** DI: `builder.Services.AddScoped<SubscriptionService>();`

- [ ] **Step 5 (tests):** `SubscriptionsTests.cs` (InMemory). Χρειάζεται admin + member στο **ίδιο** tenant → helper με ρητό tenantId (όπως στο `InstructorsTests`). Επίσης seed ενός plan μέσω scope:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class SubscriptionsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public SubscriptionsTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    private async Task CreateUserInTenantAsync(Guid tenantId, string email, string password, params string[] roles)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserName = email, Email = email,
            FirstName = "T", LastName = "U", EmailConfirmed = true,
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        foreach (var role in roles) await userManager.AddToRoleAsync(user, role);
    }

    private async Task<Guid> SeedPlanAsync(Guid tenantId, PlanType type, int sessions, int durationDays)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = new MembershipPlan { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Plan", Type = type, SessionsCount = sessions, DurationDays = durationDays, Price = 10m, IsActive = true };
        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan.Id;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task Admin_assigns_subscription_and_member_sees_it()
    {
        var tenantId = Guid.NewGuid();
        await CreateUserInTenantAsync(tenantId, "sub-admin@demo.gym", "Test1234!", Roles.Admin);
        await CreateUserInTenantAsync(tenantId, "sub-member@demo.gym", "Test1234!", Roles.User);
        var planId = await SeedPlanAsync(tenantId, PlanType.SessionPack, sessions: 10, durationDays: 60);

        var adminClient = _factory.CreateClient();
        var adminToken = await LoginAsync(adminClient, "sub-admin@demo.gym", "Test1234!");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var assign = await adminClient.PostAsJsonAsync("/subscriptions", new AssignSubscriptionRequest("sub-member@demo.gym", planId));
        assign.EnsureSuccessStatusCode();

        var memberClient = _factory.CreateClient();
        var memberToken = await LoginAsync(memberClient, "sub-member@demo.gym", "Test1234!");
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var mine = await memberClient.GetFromJsonAsync<SubscriptionResponse>("/subscriptions/me");

        Assert.NotNull(mine);
        Assert.Equal(10, mine!.RemainingSessions);
        Assert.Equal("SessionPack", mine.Type);
    }
}
```

- [ ] **Step 6:** Verify: `cd backend && dotnet test --filter "FullyQualifiedName~SubscriptionsTests"` → PASS.

- [ ] **Step 7: Commit** `git commit -am "feat: subscription assignment (by email) + member view"`

### Task 44: Integrate consumption + refund στον BookingService

**Files:**
- Modify: `backend/src/GymBooking.Core/Entities/Enums/BookingOutcome.cs`
- Modify: `backend/src/GymBooking.Api/Services/BookingService.cs`
- Create: `backend/tests/GymBooking.Tests/TestData.cs`
- Modify: `backend/tests/GymBooking.Tests/BookingTests.cs`, `ScheduleTests.cs`, `BookingEndpointsTests.cs`
- Create: `backend/tests/GymBooking.Tests/BookingSubscriptionTests.cs`

- [ ] **Step 1:** Πρόσθεσε στο `BookingOutcome`: `NoSubscription,`

- [ ] **Step 2:** Στον `BookingService.BookAsync`, **μετά** τον capacity check και **πριν** τη δημιουργία του booking, πρόσθεσε consumption. Το πλήρες ενημερωμένο τμήμα (από τον capacity check έως το commit):

```csharp
        if (session.BookedCount >= session.Capacity)
        {
            return (BookingOutcome.SessionFull, null);
        }

        // --- Φ5: consumption συνδρομής (κλείδωμα της γραμμής subscription μέσα στο ίδιο tx) ---
        var subscription = await _dbContext.Subscriptions
            .FromSqlInterpolated($@"SELECT * FROM ""Subscriptions"" WHERE ""UserId"" = {userId} AND ""TenantId"" = {tenantId} AND ""Status"" = 0 ORDER BY ""ValidFrom"" DESC LIMIT 1 FOR UPDATE")
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync();

        var now = DateTime.UtcNow;
        var usable = subscription is not null
            && subscription.ValidFrom <= now && now <= subscription.ValidTo
            && (subscription.RemainingSessions == null || subscription.RemainingSessions > 0);
        if (!usable)
        {
            return (BookingOutcome.NoSubscription, null);
        }

        if (subscription!.RemainingSessions != null)
        {
            subscription.RemainingSessions -= 1;   // SessionPack
        }
        // --- τέλος consumption ---

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ClassSessionId = classSessionId,
            Status = BookingStatus.Confirmed,
            CreatedAt = now,
            SubscriptionId = subscription.Id,   // Φ5: για ακριβές refund
        };
        _dbContext.Bookings.Add(booking);
        session.BookedCount += 1;

        await _dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return (BookingOutcome.Success, booking);
```

- [ ] **Step 3:** Στον `BookingService.CancelAsync`, μετά το `booking.CancelledAt = DateTime.UtcNow;` και το decrement του session, πρόσθεσε refund:

```csharp
        // Φ5: refund θέσης προπόνησης στη συνδρομή που καταναλώθηκε (SessionPack μόνο).
        if (booking.SubscriptionId is Guid subId)
        {
            var subscription = await _dbContext.Subscriptions
                .FromSqlInterpolated($@"SELECT * FROM ""Subscriptions"" WHERE ""Id"" = {subId} AND ""TenantId"" = {tenantId} FOR UPDATE")
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync();
            if (subscription is not null && subscription.RemainingSessions != null)
            {
                subscription.RemainingSessions += 1;
            }
        }
```
(Πρόσεξε: αυτό μπαίνει **πριν** το `await _dbContext.SaveChangesAsync(); await tx.CommitAsync();` του `CancelAsync`.)

- [ ] **Step 4:** `TestData.cs` — κοινός helper για συνδρομές (μειώνει duplication):

```csharp
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

internal static class TestData
{
    public static async Task GiveUnlimitedAsync(IServiceProvider services, Guid tenantId, Guid userId)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var plan = new MembershipPlan { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Unlimited", Type = PlanType.Unlimited, DurationDays = 30, Price = 50m, IsActive = true };
        db.MembershipPlans.Add(plan);
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, MembershipPlanId = plan.Id,
            RemainingSessions = null, ValidFrom = now.AddDays(-1), ValidTo = now.AddDays(30), Status = SubscriptionStatus.Active,
        });
        await db.SaveChangesAsync();
    }

    public static async Task GiveSessionPackAsync(IServiceProvider services, Guid tenantId, Guid userId, int sessions)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var plan = new MembershipPlan { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Pack", Type = PlanType.SessionPack, SessionsCount = sessions, DurationDays = 30, Price = 30m, IsActive = true };
        db.MembershipPlans.Add(plan);
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, MembershipPlanId = plan.Id,
            RemainingSessions = sessions, ValidFrom = now.AddDays(-1), ValidTo = now.AddDays(30), Status = SubscriptionStatus.Active,
        });
        await db.SaveChangesAsync();
    }

    public static async Task<int?> GetRemainingAsync(IServiceProvider services, Guid tenantId, Guid userId)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sub = db.Subscriptions.Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active).OrderByDescending(s => s.ValidFrom).FirstOrDefault();
        return sub?.RemainingSessions;
    }
}
```

- [ ] **Step 5:** **Ενημέρωσε τα υπάρχοντα booking tests** ώστε ο χρήστης να έχει συνδρομή πριν το `BookAsync` (αλλιώς → `NoSubscription`):
  - `BookingTests.cs`: σε **κάθε** test που περιμένει επιτυχή κράτηση, πριν το πρώτο `BookAsync(tenantId, userId, ...)` πρόσθεσε `await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);`. Στο `Concurrent_bookings_never_exceed_capacity`, δώσε συνδρομή σε **κάθε** έναν από τους 12 χρήστες: δημιούργησε πρώτα τα `userIds`, κάνε loop `GiveUnlimitedAsync`, μετά fire τα `BookAsync`.
  - `ScheduleTests.cs`: στο `Schedule_returns_only_this_week_with_booked_flag`, πριν το `booking.BookAsync(userId, ...)` πρόσθεσε `await TestData.GiveUnlimitedAsync(_factory.Services, seeded.TenantId, userId);`.
  - `BookingEndpointsTests.cs`: στο `Member_books_session_then_it_appears_in_my_bookings` δώσε συνδρομή στον booker (`await TestData.GiveUnlimitedAsync(_factory.Services, user.TenantId, user.Id);`)· στο `Booking_full_session_returns_409` δώσε συνδρομή **και** στον booker **και** στον τυχαίο χρήστη που γεμίζει το session.

- [ ] **Step 6 (νέα tests):** `BookingSubscriptionTests.cs` (Postgres). Reuse τα helpers μοτίβα του `BookingTests` (αντέγραψε `SeedSessionAsync`, `BookAsync`, `CancelAsync`, `GetSessionAsync`):

```csharp
using GymBooking.Core.Entities.Enums;
namespace GymBooking.Tests;

[Collection("Postgres")]
public class BookingSubscriptionTests
{
    private readonly PostgresApiFactory _factory;
    public BookingSubscriptionTests(PostgresApiFactory factory) { _factory = factory; }

    // (Αντέγραψε SeedSessionAsync/BookAsync/CancelAsync/GetSessionAsync από το BookingTests.)

    [Fact]
    public async Task Booking_without_subscription_returns_NoSubscription()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));

        var (outcome, _) = await BookAsync(tenantId, Guid.NewGuid(), sessionId);

        Assert.Equal(BookingOutcome.NoSubscription, outcome);
    }

    [Fact]
    public async Task SessionPack_decrements_on_book_and_refunds_on_cancel()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var userId = Guid.NewGuid();
        await TestData.GiveSessionPackAsync(_factory.Services, tenantId, userId, sessions: 3);

        var (bookOutcome, booking) = await BookAsync(tenantId, userId, sessionId);
        Assert.Equal(BookingOutcome.Success, bookOutcome);
        Assert.Equal(2, await TestData.GetRemainingAsync(_factory.Services, tenantId, userId));

        await CancelAsync(tenantId, userId, booking!.Id);
        Assert.Equal(3, await TestData.GetRemainingAsync(_factory.Services, tenantId, userId)); // refund
    }

    [Fact]
    public async Task Unlimited_does_not_decrement()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);

        var (outcome, _) = await BookAsync(tenantId, userId, sessionId);

        Assert.Equal(BookingOutcome.Success, outcome);
        Assert.Null(await TestData.GetRemainingAsync(_factory.Services, tenantId, userId));
    }
}
```

- [ ] **Step 7:** Verify ΟΛΑ → PASS: `cd backend && dotnet test`
Expected: όλα PASS (τα ενημερωμένα Φ3/Φ4 tests + νέα Φ5).

- [ ] **Step 8: Commit** `git commit -am "feat: subscription consumption + refund inside atomic booking"`

### Task 45: Frontend — plan/subscription models + API services

**Files:**
- Modify: `frontend/libs/models/src/lib/models.ts`
- Create: `frontend/libs/data-access/src/lib/membership-plan-api.service.ts`
- Create: `frontend/libs/data-access/src/lib/subscription-api.service.ts`
- Modify: `frontend/libs/data-access/src/index.ts`

- [ ] **Step 1:** Πρόσθεσε στο `models.ts`:

```typescript
export type PlanType = 'SessionPack' | 'Unlimited';

export interface MembershipPlan {
  id: string;
  name: string;
  type: PlanType;
  sessionsCount: number;
  durationDays: number;
  price: number;
  isActive: boolean;
}

export interface CreatePlanRequest {
  name: string;
  type: PlanType;
  sessionsCount: number;
  durationDays: number;
  price: number;
}

export interface AssignSubscriptionRequest {
  email: string;
  planId: string;
}

export interface Subscription {
  id: string;
  planName: string;
  type: PlanType;
  remainingSessions: number | null;
  validFrom: string;
  validTo: string;
}
```

- [ ] **Step 2:** `membership-plan-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreatePlanRequest, MembershipPlan } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class MembershipPlanApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<MembershipPlan[]> {
    return this.http.get<MembershipPlan[]>('/membership-plans');
  }

  create(request: CreatePlanRequest): Observable<MembershipPlan> {
    return this.http.post<MembershipPlan>('/membership-plans', request);
  }
}
```

- [ ] **Step 3:** `subscription-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AssignSubscriptionRequest, Subscription } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class SubscriptionApiService {
  private readonly http = inject(HttpClient);

  getMine(): Observable<Subscription | null> {
    return this.http.get<Subscription | null>('/subscriptions/me');
  }

  assign(request: AssignSubscriptionRequest): Observable<void> {
    return this.http.post<void>('/subscriptions', request);
  }
}
```

- [ ] **Step 4:** Πρόσθεσε στο `data-access/src/index.ts`:

```typescript
export * from './lib/membership-plan-api.service';
export * from './lib/subscription-api.service';
```

- [ ] **Step 5:** Verify: `cd frontend && npx nx run-many -t lint -p models data-access` → PASS.

- [ ] **Step 6: Commit** `git commit -am "feat(fe): plan/subscription models + API services"`

### Task 46: staff app — plans + ανάθεση συνδρομής

**Files:**
- Create: `frontend/apps/staff/src/app/memberships/memberships.ts`
- Create: `frontend/apps/staff/src/app/memberships/memberships.html`
- Modify: `frontend/apps/staff/src/app/app.routes.ts`
- Modify: `frontend/apps/staff/src/app/dashboard/dashboard.html` (link)

> Route προστατευμένη με `roleGuard('Admin')`.

- [ ] **Step 1:** `memberships.ts`:

```typescript
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatSelectModule } from '@angular/material/select';
import { MembershipPlanApiService, SubscriptionApiService } from '@frontend/data-access';
import { MembershipPlan } from '@frontend/models';

@Component({
  selector: 'app-memberships',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatListModule],
  templateUrl: './memberships.html',
})
export class Memberships {
  private readonly fb = inject(FormBuilder);
  private readonly planApi = inject(MembershipPlanApiService);
  private readonly subApi = inject(SubscriptionApiService);

  protected readonly plans = signal<MembershipPlan[]>([]);
  protected readonly message = signal<string | null>(null);

  protected readonly planForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    type: ['SessionPack' as 'SessionPack' | 'Unlimited', Validators.required],
    sessionsCount: [10, [Validators.required, Validators.min(0)]],
    durationDays: [30, [Validators.required, Validators.min(1)]],
    price: [50, [Validators.required, Validators.min(0)]],
  });

  protected readonly assignForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    planId: ['', Validators.required],
  });

  constructor() {
    this.loadPlans();
  }

  private loadPlans(): void {
    this.planApi.getAll().subscribe((x) => this.plans.set(x));
  }

  createPlan(): void {
    if (this.planForm.invalid) {
      this.planForm.markAllAsTouched();
      return;
    }
    this.planApi.create(this.planForm.getRawValue()).subscribe(() => {
      this.planForm.reset({ type: 'SessionPack', sessionsCount: 10, durationDays: 30, price: 50 });
      this.loadPlans();
    });
  }

  assign(): void {
    if (this.assignForm.invalid) {
      this.assignForm.markAllAsTouched();
      return;
    }
    this.message.set(null);
    this.subApi.assign(this.assignForm.getRawValue()).subscribe({
      next: () => this.message.set('Η συνδρομή ανατέθηκε.'),
      error: (err) => this.message.set(err?.error ?? 'Η ανάθεση απέτυχε.'),
    });
  }
}
```

- [ ] **Step 2:** `memberships.html`:

```html
<div class="p-4 max-w-2xl">
  <h1 class="text-2xl font-bold mb-4">Συνδρομές</h1>

  <h2 class="text-xl font-bold mb-2">Νέο πακέτο</h2>
  <form [formGroup]="planForm" (ngSubmit)="createPlan()" class="flex flex-col gap-4 mb-6">
    <mat-form-field><mat-label>Όνομα</mat-label><input matInput formControlName="name" /></mat-form-field>
    <mat-form-field>
      <mat-label>Τύπος</mat-label>
      <mat-select formControlName="type">
        <mat-option value="SessionPack">SessionPack</mat-option>
        <mat-option value="Unlimited">Unlimited</mat-option>
      </mat-select>
    </mat-form-field>
    <mat-form-field><mat-label>Προπονήσεις (SessionPack)</mat-label><input matInput type="number" formControlName="sessionsCount" /></mat-form-field>
    <mat-form-field><mat-label>Διάρκεια (ημέρες)</mat-label><input matInput type="number" formControlName="durationDays" /></mat-form-field>
    <mat-form-field><mat-label>Τιμή (€)</mat-label><input matInput type="number" formControlName="price" /></mat-form-field>
    <button mat-raised-button color="primary" type="submit">Δημιουργία πακέτου</button>
  </form>

  <mat-list class="mb-6">
    @for (p of plans(); track p.id) {
      <mat-list-item>{{ p.name }} — {{ p.type }} — {{ p.durationDays }} ημέρες — {{ p.price }}€</mat-list-item>
    } @empty {
      <p class="text-gray-500">Δεν υπάρχουν πακέτα.</p>
    }
  </mat-list>

  <h2 class="text-xl font-bold mb-2">Ανάθεση σε χρήστη</h2>
  @if (message(); as m) { <p class="mb-2">{{ m }}</p> }
  <form [formGroup]="assignForm" (ngSubmit)="assign()" class="flex flex-col gap-4">
    <mat-form-field><mat-label>Email χρήστη</mat-label><input matInput formControlName="email" /></mat-form-field>
    <mat-form-field>
      <mat-label>Πακέτο</mat-label>
      <mat-select formControlName="planId">
        @for (p of plans(); track p.id) {
          <mat-option [value]="p.id">{{ p.name }}</mat-option>
        }
      </mat-select>
    </mat-form-field>
    <button mat-raised-button color="primary" type="submit">Ανάθεση</button>
  </form>
</div>
```

- [ ] **Step 3:** Route στο staff `app.routes.ts`:

```typescript
import { Memberships } from './memberships/memberships';
```
```typescript
  { path: 'memberships', component: Memberships, canActivate: [roleGuard('Admin')] },
```

- [ ] **Step 4:** Link στο staff `dashboard.html`: `<a mat-button routerLink="/memberships">Συνδρομές</a>`.

- [ ] **Step 5:** Verify: `cd frontend && npx nx lint staff && npx nx build staff` → PASS.

- [ ] **Step 6: Commit** `git commit -am "feat(staff): membership plans + subscription assignment UI"`

### Task 47: customer app — προβολή συνδρομής στο dashboard

**Files:**
- Modify: `frontend/apps/customer/src/app/dashboard/dashboard.ts`
- Modify: `frontend/apps/customer/src/app/dashboard/dashboard.html`

- [ ] **Step 1:** Στο `dashboard.ts` (που έγραψε το Task 39) πρόσθεσε φόρτωση συνδρομής:

```typescript
import { SubscriptionApiService } from '@frontend/data-access';
import { Booking, Subscription } from '@frontend/models';
```
Μέσα στην κλάση:
```typescript
  private readonly subscriptionApi = inject(SubscriptionApiService);
  protected readonly subscription = signal<Subscription | null>(null);
```
Στον constructor, πρόσθεσε:
```typescript
    this.subscriptionApi.getMine().subscribe((s) => this.subscription.set(s));
```

- [ ] **Step 2:** Στο `dashboard.html`, πάνω από τις «Επερχόμενες κρατήσεις», πρόσθεσε κάρτα συνδρομής:

```html
  <div class="mb-6 p-4 border rounded">
    <h2 class="text-xl font-bold mb-2">Η συνδρομή μου</h2>
    @if (subscription(); as sub) {
      <p>{{ sub.planName }} ({{ sub.type }})</p>
      @if (sub.type === 'SessionPack') {
        <p>Υπόλοιπο προπονήσεων: <strong>{{ sub.remainingSessions }}</strong></p>
      }
      <p>Ισχύει έως: {{ sub.validTo | date: 'dd/MM/yyyy' }}</p>
    } @else {
      <p class="text-gray-500">Δεν έχεις ενεργή συνδρομή. Απευθύνσου στη γραμματεία.</p>
    }
  </div>
```

- [ ] **Step 3:** Verify: `cd frontend && npx nx lint customer && npx nx build customer` → PASS. (Optional e2e: admin (staff) φτιάχνει πακέτο + ανάθεση σε member → member dashboard δείχνει υπόλοιπο· κάθε κράτηση το μειώνει.)

- [ ] **Step 4: Commit** `git commit -am "feat(customer): show active subscription on dashboard"`

### Task 48: CI check + ενημέρωση progress

**Files:** Modify `CLAUDE.md`

- [ ] **Step 1:** CI (Docker up):

```bash
cd backend && dotnet build && dotnet test
cd ../frontend && npx nx run-many -t lint build
```
Expected: όλα PASS.

- [ ] **Step 2:** Στο `CLAUDE.md` → progress: τσέκαρε `[x] Φ5 — Subscriptions/plans + consumption` και ενημέρωσε «Τώρα δουλεύω / Επόμενο» σε **Φ6 — Waitlist + cancellation policy**.

- [ ] **Step 3: Commit** `git commit -am "docs: mark Phase 5 complete"`

---

## PHASE 6–8: Outline (επέκταση σε αναλυτικά tasks όταν φτάνουμε)

> Κάθε φάση = δικό της σετ bite-sized TDD tasks, που θα γραφτούν όταν ξεκινά (τότε τα paths/DTOs υπάρχουν). Εδώ μόνο το περίγραμμα + τα tricky σημεία.

### Phase 6 — Waitlist + cancellation policy (~10h)
- [ ] `WaitlistEntry` entity + migration (μετακινήθηκε εδώ από τη Φ3 — logic & schema μαζί).
- [ ] Join waitlist όταν γεμάτο· auto-promote #1 σε cancel (μέσα στο ίδιο atomic transaction του booking service — single point).
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
- **Placeholders:** Setup+Φ1+Φ2+Φ3+Φ4+Φ5 πλήρη με εντολές/κώδικα. Φ6–8 σκόπιμα outline (progressive elaboration για μεγάλο project) — όχι placeholders προς υλοποίηση τώρα.
- **Συνέπεια ονομάτων:** `ICurrentTenant`, `AppDbContext`, `TokenService`, `InvitationService`, entities Tenant/ApplicationUser/Invitation/ClassType/ClassSession/Booking/WaitlistEntry/MembershipPlan/Subscription — σταθερά σε όλο το plan & spec.
