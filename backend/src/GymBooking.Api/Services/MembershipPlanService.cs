using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class MembershipPlanService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public MembershipPlanService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<MembershipPlan?> CreateAsync(string name, string type, int sessionsCount, int durationDays, decimal price)
    {
        if (!Enum.TryParse<PlanType>(type, out var planType))
        {
            return null;
        }

        var plan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            Name = name,
            Type = planType,
            SessionsCount = sessionsCount,
            DurationDays = durationDays,
            Price = price,
            IsActive = true,
        };
        _dbContext.MembershipPlans.Add(plan);
        await _dbContext.SaveChangesAsync();
        return plan;
    }

    public async Task<List<MembershipPlan>> GetAllAsync()
    {
        return await _dbContext.MembershipPlans.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var plan = await _dbContext.MembershipPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null)
        {
            return false;
        }

        plan.IsActive = false;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
