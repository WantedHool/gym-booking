using GymBooking.Api.Multitenancy;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;
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

// EF Core + PostgreSQL (connection string από appsettings)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Health checks: ελέγχει και τη σύνδεση με τη βάση μέσω του AppDbContext
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Serilog request logging (ένα δομημένο log ανά HTTP request)
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // διαδραστικό API docs UI στο /scalar/v1
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Γεμίζει το CurrentTenant από το JWT claim "tenantId". Μέχρι το Task 12 (JwtBearer +
// UseAuthentication) δεν υπάρχει authenticated user, άρα δεν κάνει τίποτα ακόμα — έτοιμο καλώδιο.
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
