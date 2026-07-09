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

## PHASE 3–8: Outline (επέκταση σε αναλυτικά tasks όταν φτάνουμε)

> Κάθε φάση = δικό της σετ bite-sized TDD tasks, που θα γραφτούν όταν ξεκινά (τότε τα paths/DTOs υπάρχουν). Εδώ μόνο το περίγραμμα + τα tricky σημεία.

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
- **Placeholders:** Setup+Φ1+Φ2 πλήρη με εντολές/κώδικα. Φ3–8 σκόπιμα outline (progressive elaboration για μεγάλο project) — όχι placeholders προς υλοποίηση τώρα.
- **Συνέπεια ονομάτων:** `ICurrentTenant`, `AppDbContext`, `TokenService`, `InvitationService`, entities Tenant/ApplicationUser/Invitation/ClassType/ClassSession/Booking/WaitlistEntry/MembershipPlan/Subscription — σταθερά σε όλο το plan & spec.
