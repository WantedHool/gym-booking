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
