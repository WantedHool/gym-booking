using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Tests;

public class TenantIsolationTests
{
    private static AppDbContext CreateContext(string dbName, ICurrentTenant currentTenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options, currentTenant);
    }

    [Fact]
    public void Invitations_query_returns_only_current_tenant_rows()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var seedContext = CreateContext(dbName, new FakeCurrentTenant(tenantA)))
        {
            seedContext.Invitations.Add(new Invitation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                Email = "a@demo.gym",
                Role = Roles.User,
                TokenHash = "hashA",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            });
            seedContext.Invitations.Add(new Invitation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantB,
                Email = "b@demo.gym",
                Role = Roles.User,
                TokenHash = "hashB",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            });
            seedContext.SaveChanges();
        }

        using var queryContext = CreateContext(dbName, new FakeCurrentTenant(tenantA));
        var results = queryContext.Invitations.ToList();

        Assert.Single(results);
        Assert.Equal(tenantA, results[0].TenantId);
    }

    [Fact]
    public void Users_query_returns_only_current_tenant_rows()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var seedContext = CreateContext(dbName, new FakeCurrentTenant(tenantA)))
        {
            seedContext.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                UserName = "a@demo.gym",
                Email = "a@demo.gym",
                FirstName = "A",
                LastName = "One",
            });
            seedContext.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenantB,
                UserName = "b@demo.gym",
                Email = "b@demo.gym",
                FirstName = "B",
                LastName = "Two",
            });
            seedContext.SaveChanges();
        }

        using var queryContext = CreateContext(dbName, new FakeCurrentTenant(tenantA));
        var results = queryContext.Users.ToList();

        Assert.Single(results);
        Assert.Equal(tenantA, results[0].TenantId);
    }

    private class FakeCurrentTenant : ICurrentTenant
    {
        public FakeCurrentTenant(Guid tenantId)
        {
            TenantId = tenantId;
        }

        public Guid TenantId { get; }
    }
}
