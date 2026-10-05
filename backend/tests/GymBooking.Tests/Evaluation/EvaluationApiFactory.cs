using GymBooking.Api.Interfaces;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GymBooking.Tests.Evaluation;

public class EvaluationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithCommand("-c", "max_connections=300")
        .Build();

    public string ConnectionString
    {
        get
        {
            return _db.GetConnectionString() + ";Maximum Pool Size=250";
        }
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
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
            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(
                ConnectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<FakeEmailSender>());
        });
    }

    private sealed class NoTenant : ICurrentTenant
    {
        public Guid TenantId
        {
            get
            {
                return Guid.Empty;
            }
        }
    }
}

[CollectionDefinition("Evaluation")]
public class EvaluationCollection : ICollectionFixture<EvaluationApiFactory>
{
}
