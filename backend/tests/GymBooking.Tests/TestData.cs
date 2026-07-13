using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

internal static class TestData
{
    public static async Task GiveUnlimitedAsync(IServiceProvider services, Guid tenantId, Guid userId)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var plan = new MembershipPlan { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Unlimited", Type = PlanType.Unlimited, DurationDays = 30, Price = 50m, IsActive = true };
        db.MembershipPlans.Add(plan);
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            MembershipPlanId = plan.Id,
            RemainingSessions = null,
            ValidFrom = now.AddDays(-1),
            ValidTo = now.AddDays(30),
            Status = SubscriptionStatus.Active,
        });
        await db.SaveChangesAsync();
    }

    public static async Task GiveSessionPackAsync(IServiceProvider services, Guid tenantId, Guid userId, int sessions)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var plan = new MembershipPlan { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Pack", Type = PlanType.SessionPack, SessionsCount = sessions, DurationDays = 30, Price = 30m, IsActive = true };
        db.MembershipPlans.Add(plan);
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            MembershipPlanId = plan.Id,
            RemainingSessions = sessions,
            ValidFrom = now.AddDays(-1),
            ValidTo = now.AddDays(30),
            Status = SubscriptionStatus.Active,
        });
        await db.SaveChangesAsync();
    }

    public static async Task<int?> GetRemainingAsync(IServiceProvider services, Guid tenantId, Guid userId)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sub = db.Subscriptions
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.ValidFrom)
            .FirstOrDefault();
        return sub?.RemainingSessions;
    }
}
