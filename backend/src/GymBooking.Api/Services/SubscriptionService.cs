using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class SubscriptionService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public SubscriptionService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<(bool Ok, string? Error)> AssignAsync(string email, Guid planId)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (user is null)
        {
            return (false, "User not found in this tenant.");
        }

        var plan = await _dbContext.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
        if (plan is null)
        {
            return (false, "Plan not found.");
        }

        var active = await _dbContext.Subscriptions
            .Where(s => s.UserId == user.Id && s.Status == SubscriptionStatus.Active)
            .ToListAsync();
        foreach (var s in active)
        {
            s.Status = SubscriptionStatus.Cancelled;
        }

        var now = DateTime.UtcNow;
        _dbContext.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            UserId = user.Id,
            MembershipPlanId = plan.Id,
            RemainingSessions = plan.Type == PlanType.SessionPack ? plan.SessionsCount : (int?)null,
            ValidFrom = now,
            ValidTo = now.AddDays(plan.DurationDays),
            Status = SubscriptionStatus.Active,
        });
        await _dbContext.SaveChangesAsync();
        return (true, null);
    }

    public async Task<SubscriptionResponse?> GetActiveForUserAsync(Guid userId)
    {
        return await (
            from s in _dbContext.Subscriptions
            where s.UserId == userId && s.Status == SubscriptionStatus.Active
            join p in _dbContext.MembershipPlans on s.MembershipPlanId equals p.Id
            orderby s.ValidFrom descending
            select new SubscriptionResponse(
                s.Id,
                p.Name,
                p.Type.ToString(),
                s.RemainingSessions,
                p.Type == PlanType.SessionPack ? p.SessionsCount : (int?)null,
                s.ValidFrom,
                s.ValidTo))
            .FirstOrDefaultAsync();
    }
}
