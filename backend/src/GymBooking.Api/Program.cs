using System.Text;
using GymBooking.Api.Interfaces;
using GymBooking.Api.Services;
using GymBooking.Api.Utilities;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Core.Options;
using GymBooking.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: structured logging στην κονσόλα + αρχείο (ένα ανά μέρα, κρατάει τις τελευταίες 14 μέρες)
builder.Services.AddSerilog(config => config
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

// Add services to the container.
builder.Services.AddControllers();

// OpenAPI spec (built-in) — το διαδραστικό UI το σερβίρει το Scalar παρακάτω
builder.Services.AddOpenApi();

// CORS (dev only): τα Angular apps (customer:4200, staff:4201) τρέχουν σε δικό τους dev server
// origin, άρα το browser τα θεωρεί cross-origin. Bearer token στο header, όχι cookies — δεν
// χρειάζεται AllowCredentials.
const string DevCorsPolicy = "DevCors";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(DevCorsPolicy, policy =>
            policy.WithOrigins("http://localhost:4200", "http://localhost:4201")
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

// Tenant context: CurrentTenant + ICurrentTenant πρέπει να resolve στο ΙΔΙΟ scoped instance
// (ICurrentTenant το διαβάζει ο AppDbContext, CurrentTenant.SetTenant το γεμίζει το middleware).
builder.Services.AddScoped<CurrentTenant>();
builder.Services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());

// EF Core + PostgreSQL (connection string από appsettings). Στο "Testing" environment
// (integration tests μέσω WebApplicationFactory) το TestApiFactory καταχωρεί δικό του
// InMemory AppDbContext — αν καταχωρούσαμε και το Npgsql εδώ, θα συγκρούονταν δύο providers.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("Default"),
            // Retry σε παροδικά connection failures — π.χ. αν το API ξεκινήσει λίγο πριν προλάβει
            // να είναι πλήρως έτοιμη η Postgres (docker compose healthcheck ~ έως 50s).
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));
}

// ASP.NET Core Identity: user/role management, password hashing, sign-in checks.
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT: config-driven signing key/issuer/audience + TokenService (stateless, άρα singleton).
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<TokenService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Χωρίς το inbound claim map της Microsoft — ό,τι claim type εκδίδουμε στο TokenService
        // (πεζά, σύντομα: "sub", "role", "tenantId"), το ίδιο ακριβώς διαβάζουμε παντού.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };
    });

builder.Services.AddAuthorization(options =>
{
    // RequireInstructor = Instructor Ή Admin (ο Admin μπορεί ό,τι κι ο instructor).
    options.AddPolicy(Policies.RequireAdmin, policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.RequireInstructor, policy => policy.RequireRole(Roles.Instructor, Roles.Admin));
});

// Email (dev: SmtpEmailSender → Papercut) + Invitations
var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>()
    ?? throw new InvalidOperationException("Missing 'Email' configuration section.");
builder.Services.AddSingleton(emailOptions);

// Dev → SmtpEmailSender (Papercut). Αλλού (Production/demo) → LoggingEmailSender (δεν υπάρχει SMTP).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
}

var invitationOptions = builder.Configuration.GetSection("Invitations").Get<InvitationOptions>()
    ?? throw new InvalidOperationException("Missing 'Invitations' configuration section.");
builder.Services.AddSingleton(invitationOptions);
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<ClassTypeService>();
builder.Services.AddScoped<ClassSessionService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<WaitlistService>();
builder.Services.AddScoped<ScheduleService>();
builder.Services.AddScoped<MembershipPlanService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IRoleReaderWriter, IdentityRoleReaderWriter>();
builder.Services.AddScoped<TenantSettingsService>();

// Seed: 1 tenant + roles + 1 admin, μόνο σε άδεια βάση (βλ. κλήση seeder.SeedAsync() παρακάτω).
var seedOptions = builder.Configuration.GetSection("Seed").Get<SeedOptions>()
    ?? throw new InvalidOperationException("Missing 'Seed' configuration section.");
builder.Services.AddSingleton(seedOptions);
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<DemoDataSeeder>();

// Health checks: ελέγχει και τη σύνδεση με τη βάση μέσω του AppDbContext
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

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

    var demoSeeder = startupScope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    await demoSeeder.SeedAsync();

    foreach (var additionalTenant in seedOptions.AdditionalTenants)
    {
        await seeder.SeedAdditionalTenantAsync(additionalTenant);
    }
}

// Serilog request logging (ένα δομημένο log ανά HTTP request)
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // διαδραστικό API docs UI στο /scalar/v1
}

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

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

// Γεμίζει το CurrentTenant από το JWT claim "tenantId" (μόλις το UseAuthentication παραπάνω
// έχει ήδη επικυρώσει το token και γεμίσει το HttpContext.User).
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Public partial ώστε το WebApplicationFactory<Program> (integration tests) να μπορεί να το δει.
public partial class Program
{
}
