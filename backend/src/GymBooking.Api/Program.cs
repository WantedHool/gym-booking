using System.Text;
using GymBooking.Api.Auth;
using GymBooking.Api.Email;
using GymBooking.Api.Invitations;
using GymBooking.Api.Multitenancy;
using GymBooking.Api.Seeding;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: structured logging στην κονσόλα
builder.Services.AddSerilog(config => config.WriteTo.Console());

// Add services to the container.
builder.Services.AddControllers();

// OpenAPI spec (built-in) — το διαδραστικό UI το σερβίρει το Scalar παρακάτω
builder.Services.AddOpenApi();

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
        options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
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

builder.Services.AddAuthorization();

// Email (dev: SmtpEmailSender → Papercut) + Invitations
var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>()
    ?? throw new InvalidOperationException("Missing 'Email' configuration section.");
builder.Services.AddSingleton(emailOptions);
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var invitationOptions = builder.Configuration.GetSection("Invitations").Get<InvitationOptions>()
    ?? throw new InvalidOperationException("Missing 'Invitations' configuration section.");
builder.Services.AddSingleton(invitationOptions);
builder.Services.AddScoped<InvitationService>();

// Seed: 1 tenant + roles + 1 admin, μόνο σε άδεια βάση (βλ. κλήση seeder.SeedAsync() παρακάτω).
var seedOptions = builder.Configuration.GetSection("Seed").Get<SeedOptions>()
    ?? throw new InvalidOperationException("Missing 'Seed' configuration section.");
builder.Services.AddSingleton(seedOptions);
builder.Services.AddScoped<DbSeeder>();

// Health checks: ελέγχει και τη σύνδεση με τη βάση μέσω του AppDbContext
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Seed: τρέχει μόνο σε άδεια βάση (βλ. DbSeeder.SeedAsync).
using (var seedScope = app.Services.CreateScope())
{
    var seeder = seedScope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

// Serilog request logging (ένα δομημένο log ανά HTTP request)
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // διαδραστικό API docs UI στο /scalar/v1
}

app.UseHttpsRedirection();

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
