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
