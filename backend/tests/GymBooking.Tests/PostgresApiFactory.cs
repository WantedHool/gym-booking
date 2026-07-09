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
            // EnableRetryOnFailure matches το production Program.cs — χωρίς αυτό, tests δεν
            // εντοπίζουν bugs που αφορούν το EF Core execution-strategy (π.χ. χειροκίνητο
            // BeginTransactionAsync χωρίς CreateExecutionStrategy().ExecuteAsync wrapper).
            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(
                _db.GetConnectionString(),
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

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
