using System.Text;
using System.Threading.RateLimiting;
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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

builder.Services.AddControllers();

builder.Services.AddOpenApi();

const string AppCorsPolicy = "AppCors";
var corsAllowedOrigins = builder.Environment.IsDevelopment()
    ? new[] { "http://localhost:4200", "http://localhost:4201" }
    : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AppCorsPolicy, policy =>
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

const string LoginRateLimitPolicy = "login";
var isTestingEnv = builder.Environment.IsEnvironment("Testing");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(LoginRateLimitPolicy, httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (isTestingEnv)
        {
            return RateLimitPartition.GetNoLimiter(clientIp);
        }

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsync(
            "Πάρα πολλές προσπάθειες. Δοκιμάστε ξανά σε λίγο.", cancellationToken);
    };
});

builder.Services.AddScoped<CurrentTenant>();
builder.Services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("Default"),
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));
}

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

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
    options.AddPolicy(Policies.RequireAdmin, policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.RequireInstructor, policy => policy.RequireRole(Roles.Instructor, Roles.Admin));
});

var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>()
    ?? throw new InvalidOperationException("Missing 'Email' configuration section.");
builder.Services.AddSingleton(emailOptions);

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

var seedOptions = builder.Configuration.GetSection("Seed").Get<SeedOptions>()
    ?? throw new InvalidOperationException("Missing 'Seed' configuration section.");
builder.Services.AddSingleton(seedOptions);
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<DemoDataSeeder>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

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

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(AppCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TenantMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
