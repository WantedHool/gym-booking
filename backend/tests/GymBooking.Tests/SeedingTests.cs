using GymBooking.Api.Seeding;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class SeedingTests
{
    [Fact]
    public async Task SeedAsync_creates_tenant_roles_and_admin_on_empty_database()
    {
        using var factory = new TestApiFactory();
        using var scope = factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await seeder.SeedAsync();

        var tenant = await dbContext.Tenants.SingleAsync();
        Assert.Equal("Demo Gym", tenant.Name);
        Assert.Equal("demo-gym", tenant.Slug);
        Assert.Equal(24, tenant.CancellationHours);

        var admin = await userManager.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.NormalizedEmail == "ADMIN@DEMO.GYM");
        Assert.Equal(tenant.Id, admin.TenantId);
        Assert.Contains(Roles.Admin, await userManager.GetRolesAsync(admin));
    }

    [Fact]
    public async Task SeedAsync_does_not_duplicate_when_called_twice()
    {
        using var factory = new TestApiFactory();
        using var scope = factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(1, await dbContext.Tenants.CountAsync());
    }
}
