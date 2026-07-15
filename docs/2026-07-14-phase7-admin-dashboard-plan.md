# Φάση 7 — Admin/Staff dashboard + Responsive QA — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **Spec (single source of truth):** [2026-07-14-phase7-admin-dashboard-design.md](2026-07-14-phase7-admin-dashboard-design.md)
> **Commits:** τα κάνει **ο χρήστης** (project convention· ποτέ ο agent). Τα «Commit» steps είναι υπενθύμιση/checkpoint προς τον χρήστη.

**Goal:** Δίνει στο προσωπικό διαχείριση χρηστών/ρόλων, εποπτεία & χειρισμό κρατήσεων (roster / walk-in / cancel-on-behalf) και minimal tenant settings, με responsive QA pass.

**Architecture:** Backend layered (.NET 10, EF Core, Identity) — νέος `UserService`/`UsersController`, `TenantSettingsService`/`TenantController`, και **staff variants** στο υπάρχον `BookingService`/`BookingsController` που μοιράζονται τους ίδιους atomic helpers με τα self-service paths. Frontend Nx (Angular 21 signals, Material) — νέα staff pages `users`, `settings` και roster view μέσα στα `sessions`, με νέα data-access services.

**Tech Stack:** .NET 10 Web API · EF Core (Npgsql) · ASP.NET Core Identity + JWT · xUnit (InMemory) · Angular 21 · Angular Material · Nx.

---

## File Structure

### Backend — create
- `backend/src/GymBooking.Core/Contracts/UserContracts.cs` — DTOs για user management.
- `backend/src/GymBooking.Core/Contracts/TenantContracts.cs` — DTOs για tenant settings.
- `backend/src/GymBooking.Core/Contracts/RosterContracts.cs` — DTOs για roster + staff walk-in.
- `backend/src/GymBooking.Api/Services/UserService.cs` — λίστα/ρόλος/activate-deactivate.
- `backend/src/GymBooking.Api/Services/TenantSettingsService.cs` — get/update settings.
- `backend/src/GymBooking.Api/Controllers/UsersController.cs`
- `backend/src/GymBooking.Api/Controllers/TenantController.cs`
- `backend/tests/GymBooking.Tests/UserServiceTests.cs`
- `backend/tests/GymBooking.Tests/StaffBookingTests.cs`
- `backend/tests/GymBooking.Tests/TenantSettingsTests.cs`

### Backend — modify
- `backend/src/GymBooking.Api/Services/BookingService.cs` — staff variants `BookForAsync`, `CancelByStaffAsync`, `GetRosterAsync`.
- `backend/src/GymBooking.Api/Controllers/BookingsController.cs` — staff endpoints.
- `backend/src/GymBooking.Api/Controllers/AuthController.cs` — reject login αν `!IsActive`.
- `backend/src/GymBooking.Api/Program.cs` — DI για νέα services.

### Frontend — create
- `frontend/libs/data-access/src/lib/user-api.service.ts`
- `frontend/libs/data-access/src/lib/tenant-api.service.ts`
- `frontend/libs/data-access/src/lib/roster-api.service.ts`
- `frontend/apps/staff/src/app/users/users.ts` + `users.html`
- `frontend/apps/staff/src/app/settings/settings.ts` + `settings.html`
- `frontend/apps/staff/src/app/sessions/roster.ts` + `roster.html`

### Frontend — modify
- `frontend/libs/models/src/lib/models.ts` — νέα interfaces.
- `frontend/libs/data-access/src/index.ts` — exports.
- `frontend/apps/staff/src/app/app.routes.ts` — νέα routes.
- `frontend/apps/staff/src/app/shell/staff-shell.html` — nav items.

### Decisions locked
- **Deactivate source of truth:** `ApplicationUser.IsActive`. Το `Status` enum μένει ως έχει (δεν το πειράζουμε στη Φ7)· ο deactivate γράφει `IsActive`.
- **Roles:** customer = `Roles.User` ("User"). Role change μεταξύ User/Instructor/Admin.
- **"self" detection:** ο acting userId περνιέται στα service methods από το `sub` claim του controller.

---

## GROUP A — User management (Admin only)

### Task 1: User DTOs

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/UserContracts.cs`

- [ ] **Step 1: Create the contracts file**

```csharp
namespace GymBooking.Core.Contracts;

public record UserListItem(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTime CreatedAt);

public record ChangeRoleRequest(string Role);
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build backend/src/GymBooking.Core`
Expected: Build succeeded.

- [ ] **Step 3: Commit** (ο χρήστης) — `git add ... && git commit -m "feat(users): add user management DTOs"`

> **Σημείωση `CreatedAt`:** το `ApplicationUser` (IdentityUser) δεν έχει `CreatedAt`. Στο Task 2 θα το αντλήσουμε από το `Id` **μόνο αν** είναι sequential — δεν είναι. Απόφαση: **προσθήκη `CreatedAt` στο `ApplicationUser`** (migration) — δες Task 2, Step 0.

---

### Task 2: `ApplicationUser.CreatedAt` + migration

**Files:**
- Modify: `backend/src/GymBooking.Core/Entities/Models/ApplicationUser.cs`

- [ ] **Step 1: Add the field**

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Set `CreatedAt` at registration** — `AuthController.Register`, στο `new ApplicationUser { ... }` πρόσθεσε `CreatedAt = DateTime.UtcNow,`.

- [ ] **Step 3: Create migration**

Run (από `/backend`, με Postgres up):
`dotnet ef migrations add AddUserCreatedAt -p src/GymBooking.Data -s src/GymBooking.Api`
Expected: migration file δημιουργήθηκε, adds `CreatedAt` column.

- [ ] **Step 4: Apply**

Run: `dotnet ef database update -p src/GymBooking.Data -s src/GymBooking.Api`
Expected: Done.

- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -m "feat(users): add ApplicationUser.CreatedAt + migration"`

---

### Task 3: `UserService` — list / change role / toggle active

**Files:**
- Create: `backend/src/GymBooking.Api/Services/UserService.cs`
- Test: `backend/tests/GymBooking.Tests/UserServiceTests.cs`

> **Outcome enum** (define at top of UserService.cs, ίδιο μοτίβο με `BookingOutcome`):

- [ ] **Step 1: Write the failing test (self-demote guard)**

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Tests;

public class UserServiceTests
{
    private static AppDbContext Ctx(string db, Guid tenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(db).Options;
        return new AppDbContext(options, new FakeTenant(tenant));
    }

    private sealed class FakeTenant : ICurrentTenant
    {
        public FakeTenant(Guid id) { TenantId = id; }
        public Guid TenantId { get; }
    }

    [Fact]
    public async Task ChangeRole_blocks_admin_from_demoting_self()
    {
        var tenant = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();

        using (var seed = Ctx(db, tenant))
        {
            seed.Users.Add(new ApplicationUser
            {
                Id = adminId, TenantId = tenant, UserName = "admin@demo.gym",
                Email = "admin@demo.gym", FirstName = "Ad", LastName = "Min",
            });
            seed.SaveChanges();
        }

        using var ctx = Ctx(db, tenant);
        var roles = new FakeRoleReader(new() { [adminId] = new() { Roles.Admin } });
        var service = new UserService(ctx, new FakeTenant(tenant), roles);

        var outcome = await service.ChangeRoleAsync(actingUserId: adminId, targetUserId: adminId, Roles.User);

        Assert.Equal(UserOutcome.CannotModifySelf, outcome);
    }
}
```

> **`FakeRoleReader`** υλοποιεί ένα λεπτό interface `IRoleReaderWriter` (abstraction πάνω από `UserManager`, ώστε τα tests να μη στήνουν ολόκληρο Identity). Ορίζεται στο Step 3 μαζί με το service. Πρόσθεσε στο test file:

```csharp
    private sealed class FakeRoleReader : GymBooking.Api.Services.IRoleReaderWriter
    {
        private readonly Dictionary<Guid, List<string>> _roles;
        public FakeRoleReader(Dictionary<Guid, List<string>> roles) { _roles = roles; }
        public Task<IList<string>> GetRolesAsync(ApplicationUser u) =>
            Task.FromResult<IList<string>>(_roles.TryGetValue(u.Id, out var r) ? r : new List<string>());
        public Task SetSingleRoleAsync(ApplicationUser u, string role)
        {
            _roles[u.Id] = new List<string> { role };
            return Task.CompletedTask;
        }
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/tests/GymBooking.Tests --filter ChangeRole_blocks_admin_from_demoting_self`
Expected: FAIL — `UserService` / `IRoleReaderWriter` / `UserOutcome` δεν υπάρχουν.

- [ ] **Step 3: Implement `UserService` + `IRoleReaderWriter` + `UserOutcome`**

```csharp
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public enum UserOutcome
{
    Success,
    NotFound,
    CannotModifySelf,
    InvalidRole,
}

// Λεπτό abstraction πάνω από το UserManager ώστε το UserService να είναι unit-testable χωρίς Identity stack.
public interface IRoleReaderWriter
{
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task SetSingleRoleAsync(ApplicationUser user, string role);
}

public class IdentityRoleReaderWriter : IRoleReaderWriter
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityRoleReaderWriter(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    public async Task SetSingleRoleAsync(ApplicationUser user, string role)
    {
        var current = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, current);
        await _userManager.AddToRoleAsync(user, role);
    }
}

public class UserService
{
    private static readonly string[] AllowedRoles = { Roles.User, Roles.Instructor, Roles.Admin };

    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly IRoleReaderWriter _roles;

    public UserService(AppDbContext dbContext, ICurrentTenant currentTenant, IRoleReaderWriter roles)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _roles = roles;
    }

    public async Task<List<UserListItem>> GetAllAsync()
    {
        var users = await _dbContext.Users.OrderBy(u => u.Email).ToListAsync();
        var result = new List<UserListItem>();
        foreach (var u in users)
        {
            var roles = await _roles.GetRolesAsync(u);
            result.Add(new UserListItem(
                u.Id, u.Email ?? string.Empty, u.FirstName, u.LastName,
                roles.ToList(), u.IsActive, u.CreatedAt));
        }
        return result;
    }

    public async Task<UserOutcome> ChangeRoleAsync(Guid actingUserId, Guid targetUserId, string role)
    {
        if (!AllowedRoles.Contains(role))
        {
            return UserOutcome.InvalidRole;
        }
        if (actingUserId == targetUserId)
        {
            return UserOutcome.CannotModifySelf; // no self-demote (μη μείνει tenant χωρίς admin)
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
        if (user is null)
        {
            return UserOutcome.NotFound;
        }

        await _roles.SetSingleRoleAsync(user, role);
        return UserOutcome.Success;
    }

    public async Task<UserOutcome> SetActiveAsync(Guid actingUserId, Guid targetUserId, bool isActive)
    {
        if (actingUserId == targetUserId)
        {
            return UserOutcome.CannotModifySelf; // no self-deactivate
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
        if (user is null)
        {
            return UserOutcome.NotFound;
        }

        user.IsActive = isActive;
        await _dbContext.SaveChangesAsync();
        return UserOutcome.Success;
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test backend/tests/GymBooking.Tests --filter ChangeRole_blocks_admin_from_demoting_self`
Expected: PASS.

- [ ] **Step 5: Add coverage tests** (same file):

```csharp
    [Fact]
    public async Task SetActive_blocks_self_deactivation()
    {
        var tenant = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.Users.Add(new ApplicationUser { Id = adminId, TenantId = tenant, UserName = "a", Email = "a", FirstName = "A", LastName = "A" });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new UserService(ctx, new FakeTenant(tenant), new FakeRoleReader(new()));
        var outcome = await service.SetActiveAsync(adminId, adminId, false);
        Assert.Equal(UserOutcome.CannotModifySelf, outcome);
    }

    [Fact]
    public async Task ChangeRole_rejects_unknown_role()
    {
        var tenant = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using var ctx = Ctx(db, tenant);
        var service = new UserService(ctx, new FakeTenant(tenant), new FakeRoleReader(new()));
        var outcome = await service.ChangeRoleAsync(Guid.NewGuid(), Guid.NewGuid(), "Wizard");
        Assert.Equal(UserOutcome.InvalidRole, outcome);
    }

    [Fact]
    public async Task SetActive_deactivates_other_user()
    {
        var tenant = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.Users.Add(new ApplicationUser { Id = targetId, TenantId = tenant, UserName = "t", Email = "t", FirstName = "T", LastName = "T", IsActive = true });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new UserService(ctx, new FakeTenant(tenant), new FakeRoleReader(new()));
        var outcome = await service.SetActiveAsync(adminId, targetId, false);
        Assert.Equal(UserOutcome.Success, outcome);
        Assert.False((await ctx.Users.FirstAsync(u => u.Id == targetId)).IsActive);
    }
```

- [ ] **Step 6: Run all UserService tests**

Run: `dotnet test backend/tests/GymBooking.Tests --filter UserServiceTests`
Expected: 4 passed.

- [ ] **Step 7: Commit** (ο χρήστης) — `git commit -m "feat(users): UserService list/role/active + tests"`

---

### Task 4: `UsersController` + DI + reject inactive login

**Files:**
- Create: `backend/src/GymBooking.Api/Controllers/UsersController.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs`, `backend/src/GymBooking.Api/Controllers/AuthController.cs`

- [ ] **Step 1: Register services in `Program.cs`**

Βρες όπου γίνονται τα `builder.Services.AddScoped<...Service>()` και πρόσθεσε:

```csharp
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IRoleReaderWriter, IdentityRoleReaderWriter>();
builder.Services.AddScoped<TenantSettingsService>();
```
(το `TenantSettingsService` προστίθεται εδώ αλλά υλοποιείται στο Group C.)

- [ ] **Step 2: Create `UsersController`**

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize(Policy = Policies.RequireAdmin)]
public class UsersController : ControllerBase
{
    private readonly UserService _service;

    public UsersController(UserService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserListItem>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        var actingUserId = Guid.Parse(User.FindFirst("sub")!.Value);
        var outcome = await _service.ChangeRoleAsync(actingUserId, id, request.Role);
        return Map(outcome);
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var actingUserId = Guid.Parse(User.FindFirst("sub")!.Value);
        return Map(await _service.SetActiveAsync(actingUserId, id, false));
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var actingUserId = Guid.Parse(User.FindFirst("sub")!.Value);
        return Map(await _service.SetActiveAsync(actingUserId, id, true));
    }

    private IActionResult Map(UserOutcome outcome)
    {
        return outcome switch
        {
            UserOutcome.Success => NoContent(),
            UserOutcome.NotFound => NotFound(),
            UserOutcome.CannotModifySelf => Conflict("Δεν μπορείς να τροποποιήσεις τον δικό σου λογαριασμό."),
            UserOutcome.InvalidRole => BadRequest("Άκυρος ρόλος."),
            _ => BadRequest(),
        };
    }
}
```

- [ ] **Step 3: Reject inactive login** — στο `AuthController.Login`, μετά το `if (user is null) return Unauthorized();` και **πριν** το password check:

```csharp
        if (!user.IsActive)
        {
            return Unauthorized();
        }
```

- [ ] **Step 4: Build**

Run: `dotnet build backend/src/GymBooking.Api`
Expected: Build succeeded.

- [ ] **Step 5: Manual smoke** — `dotnet run --project src/GymBooking.Api`, στο Scalar (`/scalar/v1`): `GET /users` με Admin JWT → 200 + λίστα· χωρίς Admin → 403.

- [ ] **Step 6: Commit** (ο χρήστης) — `git commit -m "feat(users): UsersController + DI + reject inactive login"`

---

## GROUP B — Booking oversight (Admin + Instructor)

### Task 5: Roster + staff-booking DTOs

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/RosterContracts.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace GymBooking.Core.Contracts;

public record RosterBooking(Guid BookingId, Guid UserId, string Name, string Email, DateTime CreatedAt);
public record RosterWaiting(Guid UserId, string Name, int Position);
public record RosterResponse(
    Guid ClassSessionId,
    string ClassTypeName,
    DateTime StartsAt,
    int Capacity,
    int BookedCount,
    IReadOnlyList<RosterBooking> Confirmed,
    IReadOnlyList<RosterWaiting> Waitlist);

public record StaffBookRequest(Guid UserId);
```

- [ ] **Step 2: Build** — `dotnet build backend/src/GymBooking.Core` → succeeded.
- [ ] **Step 3: Commit** (ο χρήστης) — `git commit -m "feat(roster): roster + staff-booking DTOs"`

---

### Task 6: `BookingService.GetRosterAsync`

**Files:**
- Modify: `backend/src/GymBooking.Api/Services/BookingService.cs`
- Test: `backend/tests/GymBooking.Tests/StaffBookingTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Tests;

public class StaffBookingTests
{
    private static AppDbContext Ctx(string db, Guid tenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(db).Options;
        return new AppDbContext(options, new FakeTenant(tenant));
    }
    private sealed class FakeTenant : ICurrentTenant
    {
        public FakeTenant(Guid id) { TenantId = id; }
        public Guid TenantId { get; }
    }

    [Fact]
    public async Task GetRoster_returns_confirmed_bookings_with_user_names()
    {
        var tenant = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var classTypeId = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();

        using (var seed = Ctx(db, tenant))
        {
            seed.ClassTypes.Add(new ClassType { Id = classTypeId, TenantId = tenant, Name = "Yoga" });
            seed.ClassSessions.Add(new ClassSession { Id = sessionId, TenantId = tenant, ClassTypeId = classTypeId, InstructorId = Guid.NewGuid(), StartsAt = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Capacity = 10, BookedCount = 1 });
            seed.Users.Add(new ApplicationUser { Id = userId, TenantId = tenant, UserName = "m@demo.gym", Email = "m@demo.gym", FirstName = "Mel", LastName = "Loi" });
            seed.Bookings.Add(new Booking { Id = Guid.NewGuid(), TenantId = tenant, UserId = userId, ClassSessionId = sessionId, Status = BookingStatus.Confirmed, CreatedAt = DateTime.UtcNow });
            seed.SaveChanges();
        }

        using var ctx = Ctx(db, tenant);
        var service = new BookingService(ctx, new FakeTenant(tenant));
        var roster = await service.GetRosterAsync(sessionId);

        Assert.NotNull(roster);
        Assert.Single(roster!.Confirmed);
        Assert.Equal("Mel Loi", roster.Confirmed[0].Name);
        Assert.Equal("Yoga", roster.ClassTypeName);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test backend/tests/GymBooking.Tests --filter GetRoster_returns_confirmed_bookings_with_user_names`
Expected: FAIL — `GetRosterAsync` δεν υπάρχει.

- [ ] **Step 3: Add `GetRosterAsync` to `BookingService`**

```csharp
    public async Task<RosterResponse?> GetRosterAsync(Guid classSessionId)
    {
        var session = await (
            from s in _dbContext.ClassSessions
            where s.Id == classSessionId
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            select new { s.Id, ClassTypeName = ct.Name, s.StartsAt, s.Capacity, s.BookedCount }).FirstOrDefaultAsync();
        if (session is null)
        {
            return null;
        }

        var confirmed = await (
            from b in _dbContext.Bookings
            where b.ClassSessionId == classSessionId && b.Status == BookingStatus.Confirmed
            join u in _dbContext.Users on b.UserId equals u.Id
            orderby b.CreatedAt
            select new RosterBooking(b.Id, u.Id, u.FirstName + " " + u.LastName, u.Email ?? string.Empty, b.CreatedAt))
            .ToListAsync();

        var waitingRaw = await (
            from w in _dbContext.WaitlistEntries
            where w.ClassSessionId == classSessionId && w.Status == WaitlistStatus.Waiting
            join u in _dbContext.Users on w.UserId equals u.Id
            orderby w.CreatedAt
            select new { u.Id, Name = u.FirstName + " " + u.LastName }).ToListAsync();

        var waitlist = waitingRaw
            .Select((w, i) => new RosterWaiting(w.Id, w.Name, i + 1))
            .ToList();

        return new RosterResponse(
            session.Id, session.ClassTypeName, session.StartsAt,
            session.Capacity, session.BookedCount, confirmed, waitlist);
    }
```

(Πρόσθεσε `using GymBooking.Core.Contracts;` — ήδη υπάρχει.)

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test backend/tests/GymBooking.Tests --filter GetRoster_returns_confirmed_bookings_with_user_names`
Expected: PASS.

- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -m "feat(roster): BookingService.GetRosterAsync + test"`

---

### Task 7: `BookingService.BookForAsync` (walk-in)

**Files:**
- Modify: `backend/src/GymBooking.Api/Services/BookingService.cs`
- Test: `backend/tests/GymBooking.Tests/StaffBookingTests.cs`

> **Reuse:** το `BookForAsync(userId, sessionId)` είναι **ταυτόσημο** με το `BookAsync` — η μόνη διαφορά είναι σημασιολογική (ο `userId` προέρχεται από τον staff, όχι από το JWT). Για να μείνει DRY: κάνε το `BookAsync(userId, sessionId)` public ήδη (είναι), και πρόσθεσε λεπτό alias `BookForAsync` που καλεί το ίδιο. Έτσι το controller-intent είναι σαφές χωρίς διπλό tx logic.

- [ ] **Step 1: Write the failing test (walk-in respects capacity)**

```csharp
    [Fact]
    public async Task BookFor_returns_SessionFull_when_capacity_reached()
    {
        var tenant = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.ClassSessions.Add(new ClassSession { Id = sessionId, TenantId = tenant, ClassTypeId = Guid.NewGuid(), InstructorId = Guid.NewGuid(), StartsAt = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Capacity = 1, BookedCount = 1 });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new BookingService(ctx, new FakeTenant(tenant));
        var (outcome, _) = await service.BookForAsync(userId, sessionId);
        Assert.Equal(BookingOutcome.SessionFull, outcome);
    }
```

> **Σημείωση harness:** το `BookAsync` χρησιμοποιεί `CreateExecutionStrategy()` + `BeginTransactionAsync()` + raw `FOR UPDATE` SQL. Στον InMemory provider τα raw-SQL `FromSqlInterpolated` **δεν** τρέχουν. Επιβεβαίωσε πώς τα υπάρχοντα Φ3/Φ6 tests χειρίζονται αυτό: αν τα booking-path tests τρέχουν σε **Postgres test DB** (όχι InMemory), γράψε αυτό το test με το ίδιο pattern που ήδη υπάρχει στα Φ3 tests. **Πριν το Step 3, διάβασε το υπάρχον booking test file** (`ls backend/tests/GymBooking.Tests`) και ακολούθησε το ίδιο DB harness. Αν δεν υπάρχει booking-path test με Postgres, μετέτρεψε αυτό το test ώστε να χτυπά μόνο τον κλάδο capacity χωρίς να φτάσει σε `FOR UPDATE` (η capacity-check είναι πριν το subscription/commit, αλλά μετά το `FOR UPDATE` select — άρα χρειάζεται Postgres). **Decision:** αυτό το test τρέχει στο ίδιο harness με τα υπάρχοντα atomic-booking tests.

- [ ] **Step 2: Run to verify it fails** — `dotnet test ... --filter BookFor_returns_SessionFull_when_capacity_reached` → FAIL (`BookForAsync` missing).

- [ ] **Step 3: Add the alias**

```csharp
    // Staff walk-in: ίδια atomic ροή με το self-service BookAsync· ο userId έρχεται από τον staff.
    public async Task<(BookingOutcome Outcome, Booking? Booking)> BookForAsync(Guid userId, Guid classSessionId)
    {
        return await BookAsync(userId, classSessionId);
    }
```

- [ ] **Step 4: Run to verify it passes** — PASS.
- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -m "feat(roster): BookForAsync walk-in alias + test"`

---

### Task 8: `BookingService.CancelByStaffAsync` (cancel-on-behalf + window override)

**Files:**
- Modify: `backend/src/GymBooking.Api/Services/BookingService.cs`
- Test: `backend/tests/GymBooking.Tests/StaffBookingTests.cs`

> **Reuse:** το self-service `CancelAsync(userId, bookingId)` κάνει (α) ownership check (`booking.UserId != userId`), (β) cancellation-window check. Ο staff χρειάζεται **ούτε** το ένα **ούτε** το άλλο. Refactor: εξαγωγή του πυρήνα σε `private CancelCoreAsync(Guid bookingId, Guid? ownerUserId, bool enforceWindow)`, και τα δύο public methods καλούν τον πυρήνα.

- [ ] **Step 1: Refactor `CancelAsync` → `CancelCoreAsync`**

Αντικατέστησε το σώμα του `CancelAsync` ώστε:

```csharp
    public async Task<(BookingOutcome Outcome, Booking? Booking)> CancelAsync(Guid userId, Guid bookingId)
    {
        return await CancelCoreAsync(bookingId, ownerUserId: userId, enforceWindow: true);
    }

    // Staff: καμία ιδιοκτησία, καμία πολιτική παραθύρου.
    public async Task<(BookingOutcome Outcome, Booking? Booking)> CancelByStaffAsync(Guid bookingId)
    {
        return await CancelCoreAsync(bookingId, ownerUserId: null, enforceWindow: false);
    }

    private async Task<(BookingOutcome Outcome, Booking? Booking)> CancelCoreAsync(Guid bookingId, Guid? ownerUserId, bool enforceWindow)
    {
        // ... (το ΥΠΑΡΧΟΝ σώμα του CancelAsync, με ΔΥΟ αλλαγές: )
        //   1) ownership: αντί για `if (booking is null || booking.UserId != userId)`
        //      γράψε:
        //        if (booking is null) return (BookingOutcome.SessionNotFound, null);
        //        if (ownerUserId is Guid oid && booking.UserId != oid) return (BookingOutcome.SessionNotFound, null);
        //   2) window: τύλιξε το cancellation-window block σε `if (enforceWindow) { ... }`
        // Ο υπόλοιπος κώδικας (refund, auto-promote, commit) μένει ΑΚΡΙΒΩΣ ίδιος.
    }
```

> Ο implementer αντιγράφει το υπάρχον σώμα (γραμμές 100–206 του τρέχοντος `CancelAsync`) μέσα στο `CancelCoreAsync` και εφαρμόζει τις 2 αλλαγές.

- [ ] **Step 2: Verify existing self-service cancel tests still pass**

Run: `dotnet test backend/tests/GymBooking.Tests --filter Cancel`
Expected: όλα τα υπάρχοντα Φ3/Φ6 cancel tests PASS (regression guard του refactor).

- [ ] **Step 3: Write the failing staff-cancel test**

```csharp
    [Fact]
    public async Task CancelByStaff_cancels_without_ownership_check()
    {
        var tenant = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.ClassSessions.Add(new ClassSession { Id = sessionId, TenantId = tenant, ClassTypeId = Guid.NewGuid(), InstructorId = Guid.NewGuid(), StartsAt = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Capacity = 10, BookedCount = 1 });
            seed.Bookings.Add(new Booking { Id = bookingId, TenantId = tenant, UserId = ownerId, ClassSessionId = sessionId, Status = BookingStatus.Confirmed, CreatedAt = DateTime.UtcNow });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new BookingService(ctx, new FakeTenant(tenant));
        var (outcome, _) = await service.CancelByStaffAsync(bookingId);
        Assert.Equal(BookingOutcome.Success, outcome);
    }
```

(Ίδιο DB-harness caveat με το Task 7 — ακολούθησε το υπάρχον booking-test harness.)

- [ ] **Step 4: Run to verify it passes** — PASS.
- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -m "feat(roster): CancelByStaffAsync + refactor CancelCore + tests"`

---

### Task 9: Staff booking endpoints

**Files:**
- Modify: `backend/src/GymBooking.Api/Controllers/BookingsController.cs`

- [ ] **Step 1: Add staff endpoints** (append μέσα στην κλάση):

```csharp
    [HttpGet("/sessions/{sessionId:guid}/roster")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Instructor}")]
    public async Task<ActionResult<RosterResponse>> GetRoster(Guid sessionId)
    {
        var roster = await _service.GetRosterAsync(sessionId);
        return roster is null ? NotFound() : Ok(roster);
    }

    [HttpPost("/sessions/{sessionId:guid}/bookings")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Instructor}")]
    public async Task<IActionResult> StaffBook(Guid sessionId, [FromBody] StaffBookRequest request)
    {
        var (outcome, _) = await _service.BookForAsync(request.UserId, sessionId);
        return outcome switch
        {
            BookingOutcome.Success => NoContent(),
            BookingOutcome.SessionNotFound => NotFound(),
            BookingOutcome.SessionCancelled => Conflict("Το session έχει ακυρωθεί."),
            BookingOutcome.SessionFull => Conflict("Το session είναι πλήρες."),
            BookingOutcome.AlreadyBooked => Conflict("Ο πελάτης έχει ήδη κράτηση."),
            BookingOutcome.TimeConflict => Conflict("Ο πελάτης έχει επικαλυπτόμενη κράτηση."),
            BookingOutcome.NoSubscription => Conflict("Ο πελάτης δεν έχει ενεργή συνδρομή."),
            _ => BadRequest(),
        };
    }

    [HttpDelete("/bookings/{id:guid}/staff")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Instructor}")]
    public async Task<IActionResult> StaffCancel(Guid id)
    {
        var (outcome, _) = await _service.CancelByStaffAsync(id);
        return outcome switch
        {
            BookingOutcome.SessionNotFound => NotFound(),
            _ => NoContent(),
        };
    }
```

Πρόσθεσε στα usings: `using GymBooking.Core.Entities.Constants;` (για `Roles`).

> **Route note:** τα leading-`/` templates κάνουν override το `[Route("bookings")]` του controller, ώστε τα URLs να είναι `/sessions/{id}/roster` κ.λπ. όπως στο spec.

- [ ] **Step 2: Build** — `dotnet build backend/src/GymBooking.Api` → succeeded.
- [ ] **Step 3: Manual smoke** στο Scalar: `GET /sessions/{id}/roster` με Instructor JWT → 200· με User JWT → 403.
- [ ] **Step 4: Commit** (ο χρήστης) — `git commit -m "feat(roster): staff booking endpoints (roster/walk-in/cancel)"`

---

## GROUP C — Tenant settings (Admin only, minimal)

### Task 10: `TenantSettingsService` + DTOs + tests

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/TenantContracts.cs`, `backend/src/GymBooking.Api/Services/TenantSettingsService.cs`
- Test: `backend/tests/GymBooking.Tests/TenantSettingsTests.cs`

- [ ] **Step 1: DTOs**

```csharp
namespace GymBooking.Core.Contracts;

public record TenantSettingsResponse(string Name, int CancellationHours);
public record UpdateTenantSettingsRequest(string Name, int CancellationHours);
```

- [ ] **Step 2: Write the failing test**

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Tests;

public class TenantSettingsTests
{
    private static AppDbContext Ctx(string db, Guid tenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(db).Options;
        return new AppDbContext(options, new FakeTenant(tenant));
    }
    private sealed class FakeTenant : ICurrentTenant
    {
        public FakeTenant(Guid id) { TenantId = id; }
        public Guid TenantId { get; }
    }

    [Fact]
    public async Task Update_persists_name_and_cancellation_hours()
    {
        var tenant = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.Tenants.Add(new Tenant { Id = tenant, Name = "Old", Slug = "old", CancellationHours = 2 });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new TenantSettingsService(ctx, new FakeTenant(tenant));
        var outcome = await service.UpdateAsync(new UpdateTenantSettingsRequest("New Gym", 6));
        Assert.True(outcome);
        var settings = await service.GetAsync();
        Assert.Equal("New Gym", settings!.Name);
        Assert.Equal(6, settings.CancellationHours);
    }

    [Fact]
    public async Task Update_rejects_negative_hours()
    {
        var tenant = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.Tenants.Add(new Tenant { Id = tenant, Name = "Old", Slug = "old", CancellationHours = 2 });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new TenantSettingsService(ctx, new FakeTenant(tenant));
        var outcome = await service.UpdateAsync(new UpdateTenantSettingsRequest("X", -1));
        Assert.False(outcome);
    }
}
```

- [ ] **Step 3: Run to verify it fails** — FAIL (`TenantSettingsService` missing).

- [ ] **Step 4: Implement the service**

```csharp
using GymBooking.Core.Contracts;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class TenantSettingsService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public TenantSettingsService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<TenantSettingsResponse?> GetAsync()
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == _currentTenant.TenantId);
        if (tenant is null)
        {
            return null;
        }
        return new TenantSettingsResponse(tenant.Name, tenant.CancellationHours);
    }

    public async Task<bool> UpdateAsync(UpdateTenantSettingsRequest request)
    {
        if (request.CancellationHours < 0 || string.IsNullOrWhiteSpace(request.Name))
        {
            return false;
        }
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == _currentTenant.TenantId);
        if (tenant is null)
        {
            return false;
        }
        tenant.Name = request.Name.Trim();
        tenant.CancellationHours = request.CancellationHours;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
```

> **Σημείωση:** το `Tenants` DbSet έχει global query filter; επιβεβαίωσε ότι δεν φιλτράρεται εκτός tenant. Αν το `AppDbContext` εφαρμόζει filter στο `Tenant` που κρύβει το ίδιο το tenant, χρησιμοποίησε `.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == _currentTenant.TenantId)`. **Έλεγξε το `AppDbContext.cs` πριν το Step 4** και προσάρμοσε.

- [ ] **Step 5: Run to verify it passes** — 2 passed.
- [ ] **Step 6: Commit** (ο χρήστης) — `git commit -m "feat(settings): TenantSettingsService + DTOs + tests"`

---

### Task 11: `TenantController`

**Files:**
- Create: `backend/src/GymBooking.Api/Controllers/TenantController.cs`
(DI έγινε ήδη στο Task 4 Step 1.)

- [ ] **Step 1: Create the controller**

```csharp
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("tenant/settings")]
[Authorize(Policy = Policies.RequireAdmin)]
public class TenantController : ControllerBase
{
    private readonly TenantSettingsService _service;

    public TenantController(TenantSettingsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<TenantSettingsResponse>> Get()
    {
        var settings = await _service.GetAsync();
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTenantSettingsRequest request)
    {
        var ok = await _service.UpdateAsync(request);
        return ok ? NoContent() : BadRequest("Άκυρες ρυθμίσεις.");
    }
}
```

- [ ] **Step 2: Build** → succeeded.
- [ ] **Step 3: Manual smoke** στο Scalar: `GET /tenant/settings` (Admin) → 200· `PUT` με `cancellationHours: -1` → 400.
- [ ] **Step 4: Commit** (ο χρήστης) — `git commit -m "feat(settings): TenantController"`

---

## GROUP D — Frontend

### Task 12: Models + data-access services

**Files:**
- Modify: `frontend/libs/models/src/lib/models.ts`, `frontend/libs/data-access/src/index.ts`
- Create: `user-api.service.ts`, `tenant-api.service.ts`, `roster-api.service.ts` (στο `frontend/libs/data-access/src/lib/`)

- [ ] **Step 1: Add model interfaces** (append στο `models.ts`)

```typescript
export interface AppUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

export interface ChangeRoleRequest {
  role: 'User' | 'Instructor' | 'Admin';
}

export interface TenantSettings {
  name: string;
  cancellationHours: number;
}

export interface RosterBooking {
  bookingId: string;
  userId: string;
  name: string;
  email: string;
  createdAt: string;
}

export interface RosterWaiting {
  userId: string;
  name: string;
  position: number;
}

export interface Roster {
  classSessionId: string;
  classTypeName: string;
  startsAt: string;
  capacity: number;
  bookedCount: number;
  confirmed: RosterBooking[];
  waitlist: RosterWaiting[];
}

export interface StaffBookRequest {
  userId: string;
}
```

- [ ] **Step 2: Create `user-api.service.ts`**

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AppUser, ChangeRoleRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class UserApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>('/users');
  }

  changeRole(id: string, request: ChangeRoleRequest): Observable<void> {
    return this.http.put<void>(`/users/${id}/role`, request);
  }

  deactivate(id: string): Observable<void> {
    return this.http.put<void>(`/users/${id}/deactivate`, {});
  }

  activate(id: string): Observable<void> {
    return this.http.put<void>(`/users/${id}/activate`, {});
  }
}
```

- [ ] **Step 3: Create `tenant-api.service.ts`**

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantSettings } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class TenantApiService {
  private readonly http = inject(HttpClient);

  get(): Observable<TenantSettings> {
    return this.http.get<TenantSettings>('/tenant/settings');
  }

  update(settings: TenantSettings): Observable<void> {
    return this.http.put<void>('/tenant/settings', settings);
  }
}
```

- [ ] **Step 4: Create `roster-api.service.ts`**

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Roster, StaffBookRequest } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class RosterApiService {
  private readonly http = inject(HttpClient);

  get(sessionId: string): Observable<Roster> {
    return this.http.get<Roster>(`/sessions/${sessionId}/roster`);
  }

  walkIn(sessionId: string, request: StaffBookRequest): Observable<void> {
    return this.http.post<void>(`/sessions/${sessionId}/bookings`, request);
  }

  cancel(bookingId: string): Observable<void> {
    return this.http.delete<void>(`/bookings/${bookingId}/staff`);
  }
}
```

- [ ] **Step 5: Export them** — πρόσθεσε στο `frontend/libs/data-access/src/index.ts`:

```typescript
export * from './lib/user-api.service';
export * from './lib/tenant-api.service';
export * from './lib/roster-api.service';
```

- [ ] **Step 6: Lint + build** — `npx nx run-many -t lint build --projects=data-access,models`
Expected: pass.

- [ ] **Step 7: Commit** (ο χρήστης) — `git commit -m "feat(fe): models + user/tenant/roster api services"`

---

### Task 13: Users page

**Files:**
- Create: `frontend/apps/staff/src/app/users/users.ts`, `users.html`
- Modify: `frontend/apps/staff/src/app/app.routes.ts`

- [ ] **Step 1: Component**

```typescript
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { UserApiService } from '@frontend/data-access';
import { AppUser } from '@frontend/models';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatSelectModule, MatTableModule, MatSlideToggleModule, PageHeader],
  templateUrl: './users.html',
})
export class Users {
  private readonly api = inject(UserApiService);

  protected readonly users = signal<AppUser[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly roles = ['User', 'Instructor', 'Admin'] as const;
  protected readonly columns = ['email', 'name', 'role', 'active', 'createdAt'];

  constructor() {
    this.load();
  }

  private load(): void {
    this.api.getAll().subscribe({
      next: (u) => this.users.set(u),
      error: () => this.error.set('Αποτυχία φόρτωσης χρηστών.'),
    });
  }

  changeRole(user: AppUser, role: 'User' | 'Instructor' | 'Admin'): void {
    this.error.set(null);
    this.api.changeRole(user.id, { role }).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(err?.error ?? 'Η αλλαγή ρόλου απέτυχε.'),
    });
  }

  toggleActive(user: AppUser): void {
    this.error.set(null);
    const op = user.isActive ? this.api.deactivate(user.id) : this.api.activate(user.id);
    op.subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(err?.error ?? 'Η ενέργεια απέτυχε.'),
    });
  }
}
```

- [ ] **Step 2: Template `users.html`**

```html
<lib-page-header title="Χρήστες" subtitle="Διαχείριση ρόλων & πρόσβασης"></lib-page-header>

@if (error()) {
  <p class="text-red-600 mb-4">{{ error() }}</p>
}

<div class="overflow-x-auto">
  <table mat-table [dataSource]="users()" class="w-full">
    <ng-container matColumnDef="email">
      <th mat-header-cell *matHeaderCellDef>Email</th>
      <td mat-cell *matCellDef="let u">{{ u.email }}</td>
    </ng-container>
    <ng-container matColumnDef="name">
      <th mat-header-cell *matHeaderCellDef>Όνομα</th>
      <td mat-cell *matCellDef="let u">{{ u.firstName }} {{ u.lastName }}</td>
    </ng-container>
    <ng-container matColumnDef="role">
      <th mat-header-cell *matHeaderCellDef>Ρόλος</th>
      <td mat-cell *matCellDef="let u">
        <mat-select [value]="u.roles[0]" (selectionChange)="changeRole(u, $event.value)">
          @for (r of roles; track r) {
            <mat-option [value]="r">{{ r }}</mat-option>
          }
        </mat-select>
      </td>
    </ng-container>
    <ng-container matColumnDef="active">
      <th mat-header-cell *matHeaderCellDef>Ενεργός</th>
      <td mat-cell *matCellDef="let u">
        <mat-slide-toggle [checked]="u.isActive" (change)="toggleActive(u)"></mat-slide-toggle>
      </td>
    </ng-container>
    <ng-container matColumnDef="createdAt">
      <th mat-header-cell *matHeaderCellDef>Εγγραφή</th>
      <td mat-cell *matCellDef="let u">{{ u.createdAt | date: 'dd/MM/yyyy' }}</td>
    </ng-container>
    <tr mat-header-row *matHeaderRowDef="columns"></tr>
    <tr mat-row *matRowDef="let row; columns: columns"></tr>
  </table>
</div>
```

> `DatePipe`: πρόσθεσε `DatePipe` στα imports του component (ή `CommonModule`). Χρησιμοποίησε το ίδιο μοτίβο με άλλα staff templates — **έλεγξε ένα υπάρχον** (π.χ. `sessions.html`) για το πώς γίνεται date formatting εκεί και ακολούθησέ το.

- [ ] **Step 3: Route** — στο `app.routes.ts` πρόσθεσε import + entry κάτω από τα Admin routes:

```typescript
import { Users } from './users/users';
// ...
      { path: 'users', component: Users, canActivate: [roleGuard('Admin')] },
```

- [ ] **Step 4: Serve & manual check** — `npx nx serve staff`, login ως Admin, `/users` → βλέπεις λίστα· αλλαγή ρόλου/toggle δουλεύει· ως Instructor → redirect login.

- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -m "feat(fe): staff users page"`

---

### Task 14: Settings page

**Files:**
- Create: `frontend/apps/staff/src/app/settings/settings.ts`, `settings.html`
- Modify: `frontend/apps/staff/src/app/app.routes.ts`

- [ ] **Step 1: Component**

```typescript
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TenantApiService } from '@frontend/data-access';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, PageHeader],
  templateUrl: './settings.html',
})
export class Settings {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(TenantApiService);

  protected readonly saving = signal(false);
  protected readonly success = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    cancellationHours: [0, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    this.api.get().subscribe({
      next: (s) => this.form.setValue({ name: s.name, cancellationHours: s.cancellationHours }),
      error: () => this.error.set('Αποτυχία φόρτωσης ρυθμίσεων.'),
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.success.set(null);
    this.error.set(null);
    this.saving.set(true);
    this.api.update(this.form.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.success.set('Οι ρυθμίσεις αποθηκεύτηκαν.');
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error ?? 'Η αποθήκευση απέτυχε.');
      },
    });
  }
}
```

- [ ] **Step 2: Template `settings.html`**

```html
<lib-page-header title="Ρυθμίσεις" subtitle="Βασικές ρυθμίσεις γυμναστηρίου"></lib-page-header>

<form [formGroup]="form" (ngSubmit)="save()" class="flex flex-col gap-4 max-w-md">
  <mat-form-field>
    <mat-label>Όνομα γυμναστηρίου</mat-label>
    <input matInput formControlName="name" />
  </mat-form-field>

  <mat-form-field>
    <mat-label>Ώρες πριν το μάθημα για ακύρωση</mat-label>
    <input matInput type="number" formControlName="cancellationHours" min="0" />
  </mat-form-field>

  @if (success()) { <p class="text-green-600">{{ success() }}</p> }
  @if (error()) { <p class="text-red-600">{{ error() }}</p> }

  <button mat-flat-button color="primary" type="submit" [disabled]="saving()">Αποθήκευση</button>
</form>
```

- [ ] **Step 3: Route**

```typescript
import { Settings } from './settings/settings';
// ...
      { path: 'settings', component: Settings, canActivate: [roleGuard('Admin')] },
```

- [ ] **Step 4: Serve & check** — `/settings` φορτώνει τιμές, αποθηκεύει, negative hours → error.
- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -m "feat(fe): staff settings page"`

---

### Task 15: Roster view (roster + walk-in + cancel)

**Files:**
- Create: `frontend/apps/staff/src/app/sessions/roster.ts`, `roster.html`
- Modify: `frontend/apps/staff/src/app/app.routes.ts`, `frontend/apps/staff/src/app/sessions/sessions.html`

- [ ] **Step 1: Component**

```typescript
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { RosterApiService, UserApiService } from '@frontend/data-access';
import { AppUser, Roster } from '@frontend/models';
import { PageHeader } from '@frontend/ui';

@Component({
  selector: 'app-roster',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatListModule, PageHeader],
  templateUrl: './roster.html',
})
export class RosterView {
  private readonly route = inject(ActivatedRoute);
  private readonly rosterApi = inject(RosterApiService);
  private readonly userApi = inject(UserApiService);
  private readonly fb = inject(FormBuilder);

  private readonly sessionId = this.route.snapshot.paramMap.get('id')!;

  protected readonly roster = signal<Roster | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly customers = signal<AppUser[]>([]);

  protected readonly walkInForm = this.fb.nonNullable.group({
    userId: ['', Validators.required],
  });

  constructor() {
    this.load();
    // Για walk-in: μόνο πελάτες (role 'User') του tenant.
    this.userApi.getAll().subscribe({
      next: (u) => this.customers.set(u.filter((x) => x.roles.includes('User') && x.isActive)),
      error: () => void 0,
    });
  }

  private load(): void {
    this.rosterApi.get(this.sessionId).subscribe({
      next: (r) => this.roster.set(r),
      error: () => this.error.set('Αποτυχία φόρτωσης roster.'),
    });
  }

  walkIn(): void {
    if (this.walkInForm.invalid) {
      return;
    }
    this.error.set(null);
    this.rosterApi.walkIn(this.sessionId, this.walkInForm.getRawValue()).subscribe({
      next: () => {
        this.walkInForm.reset({ userId: '' });
        this.load();
      },
      error: (err) => this.error.set(err?.error ?? 'Η κράτηση απέτυχε.'),
    });
  }

  cancel(bookingId: string): void {
    this.error.set(null);
    this.rosterApi.cancel(bookingId).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(err?.error ?? 'Η ακύρωση απέτυχε.'),
    });
  }
}
```

- [ ] **Step 2: Template `roster.html`**

```html
@if (roster(); as r) {
  <lib-page-header [title]="r.classTypeName" [subtitle]="(r.startsAt | date: 'dd/MM HH:mm') + ' · ' + r.bookedCount + '/' + r.capacity"></lib-page-header>

  @if (error()) { <p class="text-red-600 mb-4">{{ error() }}</p> }

  <h3 class="font-semibold mt-4 mb-2">Κρατήσεις</h3>
  <mat-list>
    @for (b of r.confirmed; track b.bookingId) {
      <mat-list-item>
        <span matListItemTitle>{{ b.name }}</span>
        <span matListItemLine>{{ b.email }}</span>
        <button mat-icon-button matListItemMeta (click)="cancel(b.bookingId)" aria-label="Ακύρωση">
          <mat-icon>close</mat-icon>
        </button>
      </mat-list-item>
    } @empty {
      <p class="text-gray-500">Καμία κράτηση.</p>
    }
  </mat-list>

  <h3 class="font-semibold mt-6 mb-2">Λίστα αναμονής</h3>
  <mat-list>
    @for (w of r.waitlist; track w.userId) {
      <mat-list-item>
        <span matListItemTitle>{{ w.position }}. {{ w.name }}</span>
      </mat-list-item>
    } @empty {
      <p class="text-gray-500">Κενή.</p>
    }
  </mat-list>

  <form [formGroup]="walkInForm" (ngSubmit)="walkIn()" class="flex items-end gap-3 mt-6">
    <mat-form-field class="flex-1">
      <mat-label>Προσθήκη πελάτη (walk-in)</mat-label>
      <select matNativeControl formControlName="userId">
        <option value="">— επίλεξε —</option>
        @for (c of customers(); track c.id) {
          <option [value]="c.id">{{ c.firstName }} {{ c.lastName }} ({{ c.email }})</option>
        }
      </select>
    </mat-form-field>
    <button mat-flat-button color="primary" type="submit">Προσθήκη</button>
  </form>
} @else if (error()) {
  <p class="text-red-600">{{ error() }}</p>
}
```

- [ ] **Step 3: Route**

```typescript
import { RosterView } from './sessions/roster';
// ...
      { path: 'sessions/:id/roster', component: RosterView, canActivate: [roleGuard('Instructor', 'Admin')] },
```

- [ ] **Step 4: Link from sessions list** — στο `sessions.html`, σε κάθε session row πρόσθεσε κουμπί που πλοηγεί στο roster:

```html
<a mat-button [routerLink]="['/sessions', session.id, 'roster']">Roster</a>
```
(Βεβαιώσου ότι `RouterLink` είναι στα imports του `Sessions` component· αν το `session` variable έχει άλλο όνομα στο υπάρχον template, χρησιμοποίησε αυτό. **Έλεγξε το υπάρχον `sessions.html` πρώτα.**)

- [ ] **Step 5: Serve & manual check** — άνοιξε ένα session roster, δες κρατήσεις/waitlist, κάνε walk-in (χρειάζεσαι πελάτη με ενεργή συνδρομή), κάνε cancel.

- [ ] **Step 6: Commit** (ο χρήστης) — `git commit -m "feat(fe): session roster view with walk-in + cancel"`

---

### Task 16: Navigation wiring

**Files:**
- Modify: `frontend/apps/staff/src/app/shell/staff-shell.html`

- [ ] **Step 1: Add nav items** — μέσα στο sidenav, στην περιοχή που εμφανίζεται όταν `isAdmin()`, πρόσθεσε links για Χρήστες & Ρυθμίσεις:

```html
@if (isAdmin()) {
  <a mat-list-item routerLink="/users" routerLinkActive="active">
    <mat-icon matListItemIcon>group</mat-icon>
    <span matListItemTitle>Χρήστες</span>
  </a>
  <a mat-list-item routerLink="/settings" routerLinkActive="active">
    <mat-icon matListItemIcon>settings</mat-icon>
    <span matListItemTitle>Ρυθμίσεις</span>
  </a>
}
```
(Ακολούθησε το **ακριβές markup pattern** των υπαρχόντων nav items στο ίδιο αρχείο — έλεγξέ το πρώτα και μιμήσου το.)

- [ ] **Step 2: Serve & check** — ως Admin βλέπεις τα νέα items· ως Instructor όχι.
- [ ] **Step 3: Lint+build** — `npx nx run-many -t lint build` → pass.
- [ ] **Step 4: Commit** (ο χρήστης) — `git commit -m "feat(fe): staff nav for users + settings"`

---

## GROUP E — Responsive QA

### Task 17: Responsive QA pass + fixes

**Files:** ό,τι σπάει (staff templates κυρίως· customer αν χρειαστεί).

> Χρησιμοποίησε το browser preview (μόνο εδώ — ο χρήστης το επιτρέπει ρητά για QA). Ξεκίνα dev servers μέσω `.claude/launch.json` (customer + staff).

- [ ] **Step 1: Customer @ mobile** — resize σε 390×844. Πέρασε: login, register, schedule, session cards, bookings, waitlist join/leave. Κατάγραψε overflow / κομμένα κουμπιά / οριζόντιο scroll.

- [ ] **Step 2: Staff @ desktop (1280) & tablet (768)** — πέρασε: dashboard, sessions + **roster** (νέος πίνακας/λίστες), **users** (νέος πίνακας — ύποπτο για overflow), **settings**, memberships, invitations, sidenav (drawer σε tablet).

- [ ] **Step 3: Fix issues** — για κάθε καταγεγραμμένο πρόβλημα: διόρθωσε στο αντίστοιχο template/scss (π.χ. `overflow-x-auto` wrapper σε πίνακες — ήδη μπήκε στο users, εφάρμοσε παντού που χρειάζεται· stacking σε mobile με flex-col). Re-check μετά από κάθε fix.

- [ ] **Step 4: Proof** — screenshot customer mobile + staff desktop για επιβεβαίωση στον χρήστη.

- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -m "fix(fe): responsive QA pass Phase 7"`

---

### Task 18: Close-out

- [ ] **Step 1: Full backend test run** — `dotnet test backend` → all pass.
- [ ] **Step 2: Full FE gate** — `npx nx run-many -t lint build` → pass.
- [ ] **Step 3: Update `CLAUDE.md` progress** — τσέκαρε `[x] Φ7 — Admin/staff dashboard + responsive QA`· ενημέρωσε «Τώρα δουλεύω / Επόμενο» → **Φ8 — Hardening + security + deployment**.
- [ ] **Step 4: Update spec/plan status** (προαιρετικό σχόλιο ολοκλήρωσης).
- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -m "docs: mark Phase 7 complete"`

---

## Self-review

**Spec coverage:**
- User management (list + role + activate/deactivate) → Tasks 1–4 ✅
- Booking oversight (roster + walk-in + cancel-on-behalf + window override) → Tasks 5–9 ✅
- Tenant settings minimal → Tasks 10–11 ✅
- Roles: users→Admin, oversight→Admin+Instructor → guards σε Tasks 4/9/13/15 ✅
- Deactivated user can't login → Task 4 Step 3 ✅
- Responsive QA (customer mobile / staff desktop) → Task 17 ✅
- Nav wiring → Task 16 ✅

**Type consistency:** `UserOutcome`, `IRoleReaderWriter`, `BookForAsync`, `CancelByStaffAsync`, `CancelCoreAsync`, `GetRosterAsync`, `RosterResponse`/`RosterBooking`/`RosterWaiting`, `AppUser`/`Roster`/`TenantSettings` — σταθερά ονόματα σε backend & FE. FE role literal `'User'|'Instructor'|'Admin'` ταιριάζει με `Roles` constants.

**Open items flagged for implementer (μη-placeholders — απαιτούν έλεγχο υπάρχοντος κώδικα):**
1. **Booking-test DB harness** (Tasks 7–8): το atomic `BookAsync`/`CancelAsync` έχει raw `FOR UPDATE` που δεν τρέχει σε InMemory. Ο implementer ακολουθεί το ΥΠΑΡΧΟΝ harness των Φ3/Φ6 booking tests (Postgres test DB ή ισοδύναμο). Δες `ls backend/tests/GymBooking.Tests`.
2. **`Tenants` query filter** (Task 10): έλεγξε αν χρειάζεται `IgnoreQueryFilters` για self-tenant read.
3. **Date formatting & nav/sessions markup** (Tasks 13/15/16): μιμήσου τα υπάρχοντα staff templates.

**Placeholder scan:** κανένα «TODO/TBD»· τα «έλεγξε το υπάρχον X» είναι σκόπιμες οδηγίες ακρίβειας, όχι κενά — ο κώδικας κάθε βήματος είναι πλήρης.
