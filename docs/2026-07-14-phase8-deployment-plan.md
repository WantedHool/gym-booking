# Φάση 8 (Μέρος A) — Deployment για demo/tests — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Να τρέχει η εφαρμογή σε σταθερό δημόσιο URL (δωρεάν, Render + Neon), προσβάσιμο από κινητά, με έτοιμα demo δεδομένα — μόνο για παρουσίαση/tests.

**Architecture:** 2 Render Static Sites (customer, staff) + 1 Render Docker Web Service (.NET API) + Neon Postgres. Τα Angular apps μιλάνε στο API με relative paths μέσω same-origin rewrite-proxy (μηδέν CORS αλλαγές). Migrations + seed τρέχουν αυτόματα στο startup του API.

**Tech Stack:** Angular (Nx) · .NET 10 Web API · EF Core · PostgreSQL (Neon) · Docker · Render.

**Design doc (single source of truth):** [2026-07-14-phase8-deployment-design.md](2026-07-14-phase8-deployment-design.md)

> **Σύμβαση project:** Ο **Claude γράφει** τον κώδικα ανά task, ο **χρήστης κάνει commit + review**. Τα «Commit» βήματα παρακάτω είναι markers — τα εκτελεί ο χρήστης.
> **Frontend:** `nvm use 20` πριν οτιδήποτε. **Backend tests:** `cd backend && dotnet test`.

---

## File Structure

**Backend — κώδικας (TDD):**
- Modify `backend/src/GymBooking.Api/Services/InvitationService.cs` — `CreateAsync` επιστρέφει το register link.
- Create `backend/src/GymBooking.Core/Contracts/InvitationResponse.cs` — response record με το link.
- Modify `backend/src/GymBooking.Api/Controllers/InvitationsController.cs` — επιστρέφει `200 OK { registerLink }`.
- Modify `backend/tests/GymBooking.Tests/InvitationsTests.cs` — ενημέρωση του test που περίμενε 204.
- Create `backend/src/GymBooking.Api/Utilities/LoggingEmailSender.cs` — production-safe email sender.
- Create `backend/tests/GymBooking.Tests/LoggingEmailSenderTests.cs`.
- Create `backend/src/GymBooking.Api/Utilities/DemoDataSeeder.cs` — πλουσιότερο demo dataset.
- Create `backend/tests/GymBooking.Tests/DemoDataSeederTests.cs`.
- Modify `backend/src/GymBooking.Api/Utilities/DbSeeder.cs` — προσθήκη additional-tenant seed (config-driven, idempotent).
- Create `backend/tests/GymBooking.Tests/AdditionalTenantSeedTests.cs`.
- Modify `backend/src/GymBooking.Core/Options/SeedOptions.cs` — additional tenants config.
- Modify `backend/src/GymBooking.Api/Program.cs` — auto-migrate, forwarded headers, HTTPS-redirect guard, conditional email sender, demo seed call.

**Backend — deployment config (χωρίς tests):**
- Create `backend/src/GymBooking.Api/Dockerfile`.
- Create `backend/.dockerignore`.
- Create `backend/src/GymBooking.Api/appsettings.Production.json` (σκελετός, χωρίς secrets).

**Frontend:**
- Modify `frontend/libs/models/src/lib/models.ts` — `InvitationResponse`.
- Modify `frontend/libs/data-access/src/lib/invitation-api.service.ts` — `send()` επιστρέφει `InvitationResponse`.
- Modify `frontend/apps/staff/src/app/invitations/invitations.ts` — εμφάνιση link.
- Modify `frontend/apps/staff/src/app/invitations/invitations.html` — link + κουμπί αντιγραφής.

**Infra (χειροκίνητα από τον χρήστη, με οδηγίες):**
- Neon project, Render API service, Render static sites + rewrites, mobile smoke test.
- Create `docs/deployment-runbook.md` — βήμα-βήμα οδηγίες + demo credentials.

---

## Task 1: Invitation endpoint επιστρέφει το register link (TDD)

**Files:**
- Create: `backend/src/GymBooking.Core/Contracts/InvitationResponse.cs`
- Modify: `backend/src/GymBooking.Api/Services/InvitationService.cs:39-66`
- Modify: `backend/src/GymBooking.Api/Controllers/InvitationsController.cs:20-32`
- Test: `backend/tests/GymBooking.Tests/InvitationsTests.cs:117-130`

- [ ] **Step 1: Ενημέρωσε το failing test** — άλλαξε το `CreateInvitation_as_admin_sends_email_with_token` (γραμμές 117-130) ώστε να περιμένει `200 OK` + body με `registerLink` που περιέχει `token=`:

```csharp
    [Fact]
    public async Task CreateInvitation_as_admin_returns_register_link_with_token()
    {
        await CreateUserAsync("invite-admin-ok@demo.gym", "Test1234!", Roles.Admin);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "invite-admin-ok@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/invitations", new CreateInvitationRequest("invitee3@demo.gym", Roles.Instructor));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InvitationResponse>();
        Assert.NotNull(body);
        Assert.Contains("token=", body!.RegisterLink);

        // Το email εξακολουθεί να «στέλνεται» (dev: Papercut) — ο FakeEmailSender το καταγράφει.
        var emailSender = _factory.Services.GetRequiredService<FakeEmailSender>();
        Assert.Contains(emailSender.SentEmails, e => e.To == "invitee3@demo.gym" && e.Body.Contains("token="));
    }
```

- [ ] **Step 2: Τρέξε το test — πρέπει να ΑΠΟΤΥΧΕΙ (compile error: `InvitationResponse` δεν υπάρχει)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~CreateInvitation_as_admin_returns_register_link"`
Expected: FAIL (build error — `InvitationResponse` not found).

- [ ] **Step 3: Δημιούργησε το `InvitationResponse.cs`** (ίδιο style με `CreateInvitationRequest.cs`):

```csharp
namespace GymBooking.Core.Contracts;

public record InvitationResponse(string RegisterLink);
```

- [ ] **Step 4: Άλλαξε το `InvitationService.CreateAsync` να επιστρέφει το link** — άλλαξε την υπογραφή σε `Task<string>` και πρόσθεσε `return registerLink;` στο τέλος. Το σώμα (γραμμές 39-66) γίνεται:

```csharp
    public async Task<string> CreateAsync(string email, string role)
    {
        var rawToken = GenerateRawToken();
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            Email = email,
            Role = role,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        _dbContext.Invitations.Add(invitation);
        await _dbContext.SaveChangesAsync();

        var registerLink = $"{_options.RegisterUrlBase}?token={rawToken}";
        var roleLabel = role switch
        {
            Roles.User => "μέλος (πελάτης)",
            Roles.Instructor => "προπονητής/τρια",
            _ => role,
        };
        await _emailSender.SendAsync(
            email,
            "Πρόσκληση εγγραφής",
            $"Προσκλήθηκες να εγγραφείς ως {roleLabel} στο Demo Gym. Κάνε εγγραφή εδώ: {registerLink}");

        return registerLink;
    }
```

- [ ] **Step 5: Άλλαξε τον `InvitationsController.Create` να επιστρέφει το link** (γραμμές 20-32):

```csharp
    [HttpPost]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<InvitationResponse>> Create([FromBody] CreateInvitationRequest request)
    {
        if (request.Role != Roles.User && request.Role != Roles.Instructor)
        {
            return BadRequest($"Role must be '{Roles.User}' or '{Roles.Instructor}'.");
        }

        var registerLink = await _invitationService.CreateAsync(request.Email, request.Role);

        return Ok(new InvitationResponse(registerLink));
    }
```

- [ ] **Step 6: Τρέξε ΟΛΑ τα invitation tests — πρέπει να ΠΕΡΑΣΟΥΝ**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~InvitationsTests"`
Expected: PASS (όλα — τα register tests διαβάζουν ακόμα token από το email body, που εξακολουθεί να στέλνεται).

- [ ] **Step 7: Commit** (ο χρήστης) — `git commit -am "feat: invitation endpoint returns register link"`

---

## Task 2: Production-safe email sender (TDD)

**Files:**
- Create: `backend/src/GymBooking.Api/Utilities/LoggingEmailSender.cs`
- Test: `backend/tests/GymBooking.Tests/LoggingEmailSenderTests.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs:106-110`

- [ ] **Step 1: Γράψε το failing test** — ο `LoggingEmailSender` δεν πρέπει να ρίχνει exception (σε αντίθεση με το `SmtpEmailSender` που θα προσπαθούσε να συνδεθεί σε ανύπαρκτο SMTP):

```csharp
using GymBooking.Api.Utilities;
using Microsoft.Extensions.Logging.Abstractions;

namespace GymBooking.Tests;

public class LoggingEmailSenderTests
{
    [Fact]
    public async Task SendAsync_does_not_throw()
    {
        var sender = new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance);

        // Δεν πρέπει να ρίξει — απλώς κάνει log (καμία δικτυακή σύνδεση).
        await sender.SendAsync("to@demo.gym", "Subject", "Body with token=abc123");
    }
}
```

- [ ] **Step 2: Τρέξε — ΑΠΟΤΥΧΙΑ (compile error: `LoggingEmailSender` δεν υπάρχει)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~LoggingEmailSenderTests"`
Expected: FAIL (build error).

- [ ] **Step 3: Δημιούργησε το `LoggingEmailSender.cs`** (block body — όχι expression-bodied, βάσει σύμβασης project):

```csharp
using GymBooking.Api.Interfaces;

namespace GymBooking.Api.Utilities;

// Production-safe: δεν στέλνει πραγματικό email — καταγράφει το μήνυμα στο log. Χρησιμοποιείται
// στο deployed demo (δεν υπάρχει SMTP· ο admin παίρνει το invite link μέσα από το staff UI).
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Email (not sent) → {To} · {Subject} · {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 4: Τρέξε — ΠΕΡΝΑΕΙ**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~LoggingEmailSenderTests"`
Expected: PASS.

- [ ] **Step 5: Κάνε το email sender registration conditional στο `Program.cs`** — αντικατέστησε τη γραμμή 110 (`builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();`) με:

```csharp
// Dev → SmtpEmailSender (Papercut). Αλλού (Production/demo) → LoggingEmailSender (δεν υπάρχει SMTP).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
}
```

> Σημ.: το `Testing` environment αντικαθιστά ούτως ή άλλως τον `IEmailSender` με `FakeEmailSender` (last-registration-wins στο `TestApiFactory`) — άρα τα tests δεν επηρεάζονται.

- [ ] **Step 6: Τρέξε ΟΛΑ τα tests — τίποτα δεν σπάει**

Run: `cd backend && dotnet test`
Expected: PASS (όλα).

- [ ] **Step 7: Commit** (ο χρήστης) — `git commit -am "feat: production-safe LoggingEmailSender + conditional wiring"`

---

## Task 3: API deployment readiness στο Program.cs (auto-migrate, forwarded headers, PORT)

> Αυτά δεν είναι εύκολα unit-testable (env-conditional startup wiring). Επαληθεύονται με local build + smoke test στο Task 10-13. Κάθε βήμα δείχνει ακριβώς τον κώδικα.

**Files:**
- Modify: `backend/src/GymBooking.Api/Program.cs`

- [ ] **Step 1: Auto-apply migrations στο startup** — στο block του seeding (γραμμές 139-144), πρόσθεσε migration πριν το seed. Αντικατέστησε το υπάρχον block με:

```csharp
// Εφάρμοσε τυχόν pending migrations (deployed container: δεν τρέχει χειροκίνητα το `dotnet ef`)
// και μετά κάνε seed. Στο Testing (InMemory) δεν υπάρχουν migrations — παρακάμπτεται.
using (var startupScope = app.Services.CreateScope())
{
    if (!app.Environment.IsEnvironment("Testing"))
    {
        var dbContext = startupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    var seeder = startupScope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}
```

Πρόσθεσε στα usings (αν λείπει): `using Microsoft.EntityFrameworkCore;` (ήδη υπάρχει στη γραμμή 12).

- [ ] **Step 2: Forwarded headers + HTTPS-redirect guard** — το Render τερματίζει TLS στον proxy. Αντικατέστησε τη γραμμή 156 (`app.UseHttpsRedirection();`) με:

```csharp
// Πίσω από τον reverse proxy του Render το TLS τερματίζεται στον edge· ο container δέχεται HTTP.
// Τα forwarded headers δίνουν στο app το σωστό scheme/host. Το HTTPS redirect μένει μόνο σε dev
// (σε production θα προκαλούσε redirect loop — το HTTPS το εγγυάται ήδη ο Render edge).
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

Πρόσθεσε στα usings του `Program.cs`: `using Microsoft.AspNetCore.HttpOverrides;`

- [ ] **Step 3: Επιβεβαίωσε ότι χτίζει**

Run: `cd backend && dotnet build`
Expected: build succeeded.

- [ ] **Step 4: Τρέξε ΟΛΑ τα tests — τίποτα δεν σπάει** (το Testing env παρακάμπτει το migrate)

Run: `cd backend && dotnet test`
Expected: PASS (όλα).

- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -am "feat: API deployment readiness (auto-migrate, forwarded headers)"`

---

## Task 4: Dockerfile για το API

**Files:**
- Create: `backend/src/GymBooking.Api/Dockerfile`
- Create: `backend/.dockerignore`

- [ ] **Step 1: Δημιούργησε το `backend/.dockerignore`:**

```
**/bin/
**/obj/
**/logs/
.git/
```

- [ ] **Step 2: Δημιούργησε το `backend/src/GymBooking.Api/Dockerfile`** (multi-stage· το build context θα είναι το `backend/`). Το API ακούει στο `PORT` που δίνει το Render:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files + restore (layer caching)
COPY src/GymBooking.Api/GymBooking.Api.csproj src/GymBooking.Api/
COPY src/GymBooking.Core/GymBooking.Core.csproj src/GymBooking.Core/
COPY src/GymBooking.Data/GymBooking.Data.csproj src/GymBooking.Data/
RUN dotnet restore src/GymBooking.Api/GymBooking.Api.csproj

# Copy the rest + publish
COPY src/ src/
RUN dotnet publish src/GymBooking.Api/GymBooking.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Το Render περνάει το port μέσω env var PORT· το ASP.NET ακούει εκεί.
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymBooking.Api.dll"]
```

> **Verification (local, προαιρετικό αλλά συνιστάται):**
> Run: `cd backend && docker build -f src/GymBooking.Api/Dockerfile -t gymbooking-api .`
> Expected: build succeeded.

- [ ] **Step 3: Commit** (ο χρήστης) — `git commit -am "chore: add API Dockerfile + dockerignore"`

---

## Task 5: appsettings.Production.json skeleton

> Οι πραγματικές τιμές έρχονται από env vars στο Render (convention `Section__Key`). Αυτό το αρχείο δίνει μόνο μη-μυστικά defaults + δομή. **Κανένα secret εδώ.**

**Files:**
- Create: `backend/src/GymBooking.Api/appsettings.Production.json`

- [ ] **Step 1: Δημιούργησε το αρχείο:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Issuer": "GymBooking",
    "Audience": "GymBooking",
    "AccessTokenExpiryMinutes": 120
  },
  "Email": {
    "FromAddress": "noreply@demo.gym"
  },
  "Seed": {
    "TenantName": "Demo Gym",
    "TenantSlug": "demo-gym",
    "CancellationHours": 24,
    "AdminEmail": "admin@demo.gym"
  }
}
```

> **Env vars που ΠΡΕΠΕΙ να οριστούν στο Render** (τεκμηριώνονται στο runbook του Task 13):
> `ConnectionStrings__Default`, `Jwt__SigningKey`, `Seed__AdminPassword`, `Invitations__RegisterUrlBase`, `ASPNETCORE_ENVIRONMENT=Production`.

- [ ] **Step 2: Επιβεβαίωσε ότι το `.gitignore` δεν αγνοεί το `appsettings.Production.json`** (πρέπει να μπει στο git — δεν έχει secrets). Έλεγξε ότι ΔΕΝ υπάρχει pattern που να το κόβει.

Run: `cd backend && git check-ignore src/GymBooking.Api/appsettings.Production.json; echo "exit=$?"`
Expected: `exit=1` (δηλαδή ΔΕΝ αγνοείται).

- [ ] **Step 3: Commit** (ο χρήστης) — `git commit -am "chore: add appsettings.Production.json skeleton"`

---

## Task 6: Πλουσιότερο demo dataset (TDD)

> Νέος `DemoDataSeeder` που τρέχει **μετά** τον `DbSeeder` (χρειάζεται το seeded tenant + admin). Γεμίζει class types, instructors, sessions, members, subscriptions, 1 booking, 1 waitlist entry — **μόνο** αν δεν υπάρχουν ήδη (idempotent guard). Πριν γράψεις κώδικα, **διάβασε** τα entities/services που θα χρησιμοποιήσεις για ακριβείς properties: `ClassType`, `ClassSession`, `MembershipPlan`, `Subscription`, `Booking`, `WaitlistEntry` στο `backend/src/GymBooking.Core/Entities/Models/`, και τους αντίστοιχους services στο `backend/src/GymBooking.Api/Services/`.

**Files:**
- Create: `backend/src/GymBooking.Api/Utilities/DemoDataSeeder.cs`
- Test: `backend/tests/GymBooking.Tests/DemoDataSeederTests.cs`
- Modify: `backend/src/GymBooking.Api/Program.cs` (DI + κλήση)

- [ ] **Step 1: Διάβασε τα entities** για να ξέρεις τα ακριβή property names (ο executor ΔΕΝ πρέπει να μαντέψει):

Run: `cat backend/src/GymBooking.Core/Entities/Models/ClassSession.cs backend/src/GymBooking.Core/Entities/Models/MembershipPlan.cs backend/src/GymBooking.Core/Entities/Models/Subscription.cs backend/src/GymBooking.Core/Entities/Models/Booking.cs backend/src/GymBooking.Core/Entities/Models/WaitlistEntry.cs`
Expected: εμφανίζονται τα properties — χρησιμοποίησέ τα ακριβώς στα επόμενα βήματα.

- [ ] **Step 2: Γράψε το failing test** — μετά από `DemoDataSeeder.SeedAsync()` σε βάση που έχει ήδη tenant+admin, υπάρχουν class types, sessions (μελλοντικά), και επιπλέον users. Idempotent: δεύτερη κλήση δεν διπλασιάζει:

```csharp
using GymBooking.Api.Utilities;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class DemoDataSeederTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public DemoDataSeederTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedAsync_populates_demo_data_and_is_idempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();

        await seeder.SeedAsync();
        await seeder.SeedAsync(); // δεύτερη φορά — δεν πρέπει να διπλασιάσει

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var classTypeCount = await dbContext.ClassTypes.IgnoreQueryFilters().CountAsync();
        var sessionCount = await dbContext.ClassSessions.IgnoreQueryFilters().CountAsync();

        Assert.True(classTypeCount >= 2, $"Expected >= 2 class types, got {classTypeCount}");
        Assert.True(sessionCount >= 3, $"Expected >= 3 sessions, got {sessionCount}");

        // Idempotent: όλα τα sessions είναι μελλοντικά (ώστε να φαίνονται στο πρόγραμμα).
        var allFuture = await dbContext.ClassSessions.IgnoreQueryFilters()
            .AllAsync(s => s.StartsAt > DateTime.UtcNow);
        Assert.True(allFuture);
    }
}
```

- [ ] **Step 3: Τρέξε — ΑΠΟΤΥΧΙΑ (`DemoDataSeeder` δεν υπάρχει)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~DemoDataSeederTests"`
Expected: FAIL (build error).

- [ ] **Step 4: Δημιούργησε το `DemoDataSeeder.cs`** — χρησιμοποίησε τα **ακριβή** property names από το Step 1. Σκελετός (συμπλήρωσε τα properties βάσει των entities· idempotent guard σε ClassTypes· TenantId = του seeded tenant· sessions με `StartsAt = DateTime.UtcNow.AddDays(n)`):

```csharp
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Utilities;

// Τρέχει μετά τον DbSeeder. Γεμίζει το demo tenant με ρεαλιστικά δεδομένα ώστε ένας tester
// να βλέπει αμέσως γεμάτο app. Idempotent: guard στα ClassTypes.
public class DemoDataSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public DemoDataSeeder(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        var tenant = await _dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync();
        if (tenant is null)
        {
            return; // δεν υπάρχει tenant — ο DbSeeder δεν έχει τρέξει
        }

        // Idempotent guard: αν υπάρχουν ήδη class types, μην ξανασπείρεις.
        if (await _dbContext.ClassTypes.IgnoreQueryFilters().AnyAsync())
        {
            return;
        }

        // 1) Class types (συμπλήρωσε τα properties από ClassType.cs — Step 1)
        // 2) 1-2 instructors μέσω _userManager.CreateAsync(...) + AddToRoleAsync(Roles.Instructor)
        // 3) Class sessions με StartsAt = DateTime.UtcNow.AddDays(1..7), TenantId = tenant.Id
        // 4) 2-3 members + subscriptions
        // 5) 1 booking + 1 waitlist entry
        // Χρησιμοποίησε ΑΚΡΙΒΩΣ τα property names από το Step 1.

        await _dbContext.SaveChangesAsync();
    }
}
```

> **ΣΗΜΑΝΤΙΚΟ για τον executor:** Τα σχόλια 1-5 είναι obligations — γράψε πραγματικό κώδικα με τα ακριβή properties από το Step 1, όχι placeholders. Τα credentials των demo instructors/members κράτα τα σταθερά & τεκμηρίωσέ τα στο runbook (Task 13): π.χ. `instructor@demo.gym / Demo1234!`, `member@demo.gym / Demo1234!`.

- [ ] **Step 5: Register στο `Program.cs`** — πρόσθεσε DI (κοντά στο `AddScoped<DbSeeder>()`, γραμμή 131):

```csharp
builder.Services.AddScoped<DemoDataSeeder>();
```

Και στο startup block (Task 3 Step 1), μετά το `seeder.SeedAsync()`:

```csharp
    var demoSeeder = startupScope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    await demoSeeder.SeedAsync();
```

- [ ] **Step 6: Τρέξε — ΠΕΡΝΑΕΙ**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~DemoDataSeederTests"`
Expected: PASS. Μετά τρέξε ΟΛΑ (`dotnet test`) → όλα PASS.

- [ ] **Step 7: Commit** (ο χρήστης) — `git commit -am "feat: demo data seeder for populated demo instance"`

---

## Task 7: Config-driven additional-tenant seed (TDD)

> Δημιουργεί επιπλέον tenant + admin (για τα προσωπικά σου tests) μέσω `UserManager` (έγκυρο Identity hash), idempotent σε slug, config-driven. Ο πυρήνας μπαίνει στον `DbSeeder`.

**Files:**
- Modify: `backend/src/GymBooking.Core/Options/SeedOptions.cs`
- Modify: `backend/src/GymBooking.Api/Utilities/DbSeeder.cs`
- Test: `backend/tests/GymBooking.Tests/AdditionalTenantSeedTests.cs`

- [ ] **Step 1: Διάβασε το `SeedOptions.cs`** για να ξέρεις το υπάρχον σχήμα:

Run: `cat backend/src/GymBooking.Core/Options/SeedOptions.cs`
Expected: εμφανίζονται τα properties (TenantName, TenantSlug, CancellationHours, AdminEmail, AdminPassword).

- [ ] **Step 2: Πρόσθεσε το additional-tenants σχήμα στο `SeedOptions.cs`** — πρόσθεσε record + property:

```csharp
public class AdditionalTenantSeed
{
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public int CancellationHours { get; set; } = 24;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
```

Και μέσα στο `SeedOptions`, πρόσθεσε:

```csharp
    public List<AdditionalTenantSeed> AdditionalTenants { get; set; } = new();
```

- [ ] **Step 3: Γράψε το failing test** — δημιούργησε `AdditionalTenantSeedTests.cs`. Τεστάρει τη μέθοδο `SeedAdditionalTenantAsync` απευθείας (unit, με πραγματικό `UserManager` από το factory scope). Idempotent σε slug:

```csharp
using GymBooking.Api.Utilities;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Options;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class AdditionalTenantSeedTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AdditionalTenantSeedTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedAdditionalTenantAsync_creates_tenant_and_admin_and_is_idempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        var seed = new AdditionalTenantSeed
        {
            TenantName = "Test Gym 2",
            TenantSlug = "test-gym-2",
            CancellationHours = 12,
            AdminEmail = "admin2@test.gym",
            AdminPassword = "Admin1234!",
        };

        await seeder.SeedAdditionalTenantAsync(seed);
        await seeder.SeedAdditionalTenantAsync(seed); // idempotent

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenants = await dbContext.Tenants.IgnoreQueryFilters()
            .Where(t => t.Slug == "test-gym-2").ToListAsync();
        Assert.Single(tenants);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.Users.IgnoreQueryFilters()
            .SingleAsync(u => u.NormalizedEmail == "ADMIN2@TEST.GYM");
        Assert.Equal(tenants[0].Id, admin.TenantId);
        Assert.Contains(Roles.Admin, await userManager.GetRolesAsync(admin));
        // Έγκυρο Identity hash → το password επαληθεύεται.
        Assert.True(await userManager.CheckPasswordAsync(admin, "Admin1234!"));
    }
}
```

- [ ] **Step 4: Τρέξε — ΑΠΟΤΥΧΙΑ (`SeedAdditionalTenantAsync` δεν υπάρχει)**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~AdditionalTenantSeedTests"`
Expected: FAIL (build error).

- [ ] **Step 5: Πρόσθεσε τη `SeedAdditionalTenantAsync` στον `DbSeeder`** — ίδιο pattern με το υπάρχον `SeedAsync` (tenant → roles → admin μέσω `UserManager`, με rollback σε αποτυχία). Guard idempotency σε slug:

```csharp
    // On-demand: δημιουργεί επιπλέον tenant + admin (για προσωπικά tests). Idempotent σε slug.
    // Ο admin δημιουργείται μέσω UserManager (έγκυρο Identity hash), όπως ο πρώτος.
    public async Task SeedAdditionalTenantAsync(AdditionalTenantSeed seed)
    {
        if (string.IsNullOrWhiteSpace(seed.TenantSlug))
        {
            return;
        }

        var exists = await _dbContext.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Slug == seed.TenantSlug);
        if (exists)
        {
            return;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = seed.TenantName,
            Slug = seed.TenantSlug,
            CancellationHours = seed.CancellationHours,
            IsActive = true,
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        foreach (var role in new[] { Roles.User, Roles.Instructor, Roles.Admin })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserName = seed.AdminEmail,
            Email = seed.AdminEmail,
            FirstName = "Admin",
            LastName = tenant.Name,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(admin, seed.AdminPassword);
        if (!result.Succeeded)
        {
            _dbContext.Tenants.Remove(tenant);
            await _dbContext.SaveChangesAsync();
            throw new InvalidOperationException(
                "Seeding additional tenant admin failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(admin);
            _dbContext.Tenants.Remove(tenant);
            await _dbContext.SaveChangesAsync();
            throw new InvalidOperationException(
                "Seeding additional tenant admin role failed: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }
```

Πρόσθεσε στα usings του `DbSeeder.cs` (αν λείπει): `using GymBooking.Core.Options;` (ήδη υπάρχει, γραμμή 3).

- [ ] **Step 6: Κάλεσε το από το startup για κάθε configured tenant** — στο `Program.cs` startup block (Task 6 Step 5), μετά το `demoSeeder.SeedAsync()`:

```csharp
    foreach (var additionalTenant in seedOptions.AdditionalTenants)
    {
        await seeder.SeedAdditionalTenantAsync(additionalTenant);
    }
```

> Έτσι, όποτε θέλεις νέο tenant, το προσθέτεις στο `Seed:AdditionalTenants` (env var / config) και restart — idempotent, δεν αγγίζει το demo tenant.

- [ ] **Step 7: Τρέξε — ΠΕΡΝΑΕΙ**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~AdditionalTenantSeedTests"`
Expected: PASS. Μετά ΟΛΑ (`dotnet test`) → όλα PASS.

- [ ] **Step 8: Commit** (ο χρήστης) — `git commit -am "feat: config-driven additional-tenant seed (idempotent, UserManager)"`

---

## Task 8: Frontend — invitation service επιστρέφει το link

**Files:**
- Modify: `frontend/libs/models/src/lib/models.ts:74-77`
- Modify: `frontend/libs/data-access/src/lib/invitation-api.service.ts:10-12`

> `nvm use 20` πρώτα.

- [ ] **Step 1: Πρόσθεσε το `InvitationResponse` interface** στο `models.ts` (μετά το `CreateInvitationRequest`, γραμμή 77):

```typescript
export interface InvitationResponse {
  registerLink: string;
}
```

- [ ] **Step 2: Άλλαξε το `send()` να επιστρέφει `InvitationResponse`** στο `invitation-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateInvitationRequest, InvitationResponse } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class InvitationApiService {
  private readonly http = inject(HttpClient);

  send(request: CreateInvitationRequest): Observable<InvitationResponse> {
    return this.http.post<InvitationResponse>('/invitations', request);
  }
}
```

- [ ] **Step 3: Επιβεβαίωσε ότι χτίζουν οι libs**

Run: `cd frontend && npx nx run-many -t lint -p models data-access`
Expected: PASS.

- [ ] **Step 4: Commit** (ο χρήστης) — `git commit -am "feat(fe): invitation service returns register link"`

---

## Task 9: Frontend — staff εμφανίζει το invite link με κουμπί αντιγραφής

**Files:**
- Modify: `frontend/apps/staff/src/app/invitations/invitations.ts`
- Modify: `frontend/apps/staff/src/app/invitations/invitations.html`

- [ ] **Step 1: Διάβασε το υπάρχον `invitations.html`** για να ταιριάξεις το styling:

Run: `cat frontend/apps/staff/src/app/invitations/invitations.html`
Expected: βλέπεις το form + success/error μηνύματα.

- [ ] **Step 2: Άλλαξε το `invitations.ts`** — αποθήκευσε το link σε signal, πρόσθεσε copy. Αντικατέστησε το `submit()` + πρόσθεσε signal + copy method:

```typescript
  protected readonly registerLink = signal<string | null>(null);
  protected readonly copied = signal(false);

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.registerLink.set(null);
    this.copied.set(false);
    this.submitting.set(true);
    const { email, role } = this.form.getRawValue();
    this.api.send({ email, role }).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.successMessage.set(`Η πρόσκληση για ${email} δημιουργήθηκε. Στείλε το παρακάτω link:`);
        this.registerLink.set(response.registerLink);
        this.form.reset({ email: '', role: 'User' });
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Αποτυχία δημιουργίας πρόσκλησης.');
      },
    });
  }

  async copyLink(): Promise<void> {
    const link = this.registerLink();
    if (!link) {
      return;
    }
    await navigator.clipboard.writeText(link);
    this.copied.set(true);
  }
```

- [ ] **Step 3: Πρόσθεσε στο `invitations.html`** (μετά το `successMessage` block) το link + κουμπί αντιγραφής:

```html
@if (registerLink()) {
  <div class="mt-4 rounded border p-3">
    <p class="mb-2 break-all font-mono text-sm">{{ registerLink() }}</p>
    <button mat-stroked-button type="button" (click)="copyLink()">
      {{ copied() ? 'Αντιγράφηκε ✓' : 'Αντιγραφή link' }}
    </button>
  </div>
}
```

- [ ] **Step 4: Επιβεβαίωσε ότι χτίζει το staff app**

Run: `cd frontend && npx nx build staff`
Expected: build succeeded.

- [ ] **Step 5: Commit** (ο χρήστης) — `git commit -am "feat(fe): show invite link with copy button in staff"`

---

## Task 10: Neon Postgres setup (χειροκίνητα, ο χρήστης)

> Δεν είναι κώδικας — οδηγίες. Ο Claude καθοδηγεί, ο χρήστης εκτελεί (χρειάζεται λογαριασμός).

- [ ] **Step 1:** Λογαριασμός στο [neon.tech](https://neon.tech) (δωρεάν) → New Project → περιοχή EU (π.χ. Frankfurt) → κράτα το connection string (μορφή Npgsql).
- [ ] **Step 2:** Μετέτρεψε το connection string σε Npgsql format αν χρειάζεται: `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true`.
- [ ] **Step 3:** Κράτα το — θα μπει ως `ConnectionStrings__Default` env var στο Render (Task 11).

---

## Task 11: Render API Web Service (χειροκίνητα, ο χρήστης)

- [ ] **Step 1:** Push όλα τα προηγούμενα tasks σε GitHub `main` (ο χρήστης — προϋποθέτει ότι το repo είναι στο GitHub· αν όχι, πρώτα `git remote add` + push).
- [ ] **Step 2:** [render.com](https://render.com) → New → Web Service → σύνδεση με το GitHub repo → Runtime: **Docker** → Dockerfile path: `backend/src/GymBooking.Api/Dockerfile` → Docker build context: `backend`.
- [ ] **Step 3:** Environment variables (Render dashboard):
  - `ASPNETCORE_ENVIRONMENT` = `Production`
  - `ConnectionStrings__Default` = (το Neon string από Task 10)
  - `Jwt__SigningKey` = (τυχαίο ≥32 χαρακτήρες — π.χ. `openssl rand -base64 48`)
  - `Seed__AdminPassword` = (δικό σου demo admin password)
  - `Invitations__RegisterUrlBase` = (θα το συμπληρώσεις μετά το Task 12 — προσωρινά placeholder)
- [ ] **Step 4:** Health check path: `/health`. Deploy.
- [ ] **Step 5:** Verify: άνοιξε `https://<api>.onrender.com/health` → `Healthy`. Έλεγξε τα logs ότι έτρεξαν migrations + seed χωρίς σφάλμα.

---

## Task 12: Render Static Sites (customer + staff) + rewrites (χειροκίνητα)

> Το `@angular/build:application` παράγει output στο `dist/apps/<app>/browser`. **Επιβεβαίωσε** το ακριβές path με ένα local build (`npx nx build staff` → κοίτα τι δημιουργήθηκε κάτω από `frontend/dist/apps/staff/`).

- [ ] **Step 1 (staff):** Render → New → Static Site → repo →
  - Build command: `cd frontend && npm ci && npx nx build staff`
  - Publish directory: `frontend/dist/apps/staff/browser` (επιβεβαίωσε το `browser` subfolder)
- [ ] **Step 2 (staff rewrites):** στο Static Site → Redirects/Rewrites, πρόσθεσε **rewrite** rules (τύπος: Rewrite, όχι Redirect) για κάθε API prefix προς το API origin. Από το [`proxy.conf.js`](../frontend/apps/staff/proxy.conf.js) τα prefixes είναι:
  `/auth`, `/invitations`, `/class-types`, `/class-sessions`, `/bookings`, `/schedule`, `/instructors`, `/membership-plans`, `/subscriptions`, `/users`, `/tenant`, `/sessions`, `/health`.
  Κανόνας ανά prefix: Source `/auth/*` → Destination `https://<api>.onrender.com/auth/:splat` (Rewrite). **Και** SPA fallback: Source `/*` → Destination `/index.html` (Rewrite) — **τελευταίο**, ώστε να μην υπερκαλύπτει τα API rewrites.
- [ ] **Step 3 (customer):** Ίδια διαδικασία — build `npx nx build customer`, publish `frontend/dist/apps/customer/browser`. Rewrites: μόνο τα prefixes που χρησιμοποιεί το customer (κοίτα `frontend/apps/customer/proxy.conf.js`) + SPA fallback.
- [ ] **Step 4:** Πάρε το customer URL → γύρνα στο Render API service → όρισε `Invitations__RegisterUrlBase` = `https://<customer>.onrender.com/register` → redeploy API.
- [ ] **Step 5:** Verify: άνοιξε το staff URL → login με τον seeded admin → πρέπει να δουλεύει (same-origin proxy → API).

---

## Task 13: Deployment runbook + mobile smoke test

**Files:**
- Create: `docs/deployment-runbook.md`

- [ ] **Step 1: Γράψε το `docs/deployment-runbook.md`** — περιέχει: URLs (customer/staff/API), demo credentials (admin, demo instructor, demo member από Task 6), env vars που χρειάζεται το Render, πώς προσθέτεις δεύτερο tenant (`Seed:AdditionalTenants` env var + restart, από Task 7), και σημείωση για το cold start (~30s στο πρώτο request).

- [ ] **Step 2: End-to-end smoke test από κινητό:**
  - Άνοιξε το staff URL σε κινητό → login admin.
  - Δημιούργησε invitation (role=User) → αντίγραψε το link.
  - Άνοιξε το link (customer register) σε άλλο κινητό/tab → κάνε register.
  - Login ως ο νέος user στο customer app → δες το πρόγραμμα (demo sessions) → κάνε μια κράτηση.
  - Επιβεβαίωσε ότι όλα δουλεύουν από κινητό.

- [ ] **Step 3: Ενημέρωσε το CLAUDE.md** — τσέκαρε την πρόοδο (Φ8 Μέρος A: deployment ✅) και πρόσθεσε τα deployed URLs στις σημειώσεις.

- [ ] **Step 4: Commit** (ο χρήστης) — `git commit -am "docs: deployment runbook + phase 8 deployment done"`

---

## Self-Review (συντάκτη)

- **Spec coverage:** A (backend readiness) → Tasks 3,4,5 · B (invite link UI) → Tasks 1,8,9 + LoggingEmailSender Task 2 · C (demo data) → Task 6 · D (2ος tenant) → Task 7 · E (secrets/config) → Tasks 5,11. Rewrite-proxy → Task 12. Runbook/smoke → Task 13. ✅ Όλα καλυμμένα.
- **Placeholders:** Το μόνο «σκελετό» σημείο είναι το `DemoDataSeeder` σώμα (Task 6 Step 4) — σκόπιμα, γιατί εξαρτάται από τα ακριβή entity properties που ο executor διαβάζει στο Step 1· περιγράφεται ρητά ως obligation με τα ακριβή βήματα.
- **Type consistency:** `InvitationResponse` (BE record `RegisterLink` / FE interface `registerLink`) συνεπές με JSON camelCase. `SeedAdditionalTenantAsync(AdditionalTenantSeed)` ίδιο σε Task 7 & Program.cs κλήση. `DemoDataSeeder.SeedAsync()` ίδιο σε Task 6 test & Program.cs.
- **Γνωστά ρίσκα (μη-code):** το ακριβές Render publish path (`browser` subfolder) & τα Static Site rewrites επιβεβαιώνονται live στο Task 12· καταγεγραμμένα ως verification βήματα.
