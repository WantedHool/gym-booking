using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Tests;

public class TenantSettingsTests
{
    private static AppDbContext Ctx(string db, Guid tenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(db).Options;
        return new AppDbContext(options, new FakeTenant(tenant));
    }
    private sealed class FakeTenant : ICurrentTenant
    {
        public FakeTenant(Guid id) { TenantId = id; }
        public Guid TenantId { get; }
    }

    [Fact]
    public async Task Update_persists_name_and_cancellation_hours()
    {
        var tenant = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.Tenants.Add(new Tenant { Id = tenant, Name = "Old", Slug = "old", CancellationHours = 2 });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new TenantSettingsService(ctx, new FakeTenant(tenant));
        var outcome = await service.UpdateAsync(new UpdateTenantSettingsRequest("New Gym", 6));
        Assert.True(outcome);
        var settings = await service.GetAsync();
        Assert.Equal("New Gym", settings!.Name);
        Assert.Equal(6, settings.CancellationHours);
    }

    [Fact]
    public async Task Update_rejects_negative_hours()
    {
        var tenant = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.Tenants.Add(new Tenant { Id = tenant, Name = "Old", Slug = "old", CancellationHours = 2 });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new TenantSettingsService(ctx, new FakeTenant(tenant));
        var outcome = await service.UpdateAsync(new UpdateTenantSettingsRequest("X", -1));
        Assert.False(outcome);
    }
}
