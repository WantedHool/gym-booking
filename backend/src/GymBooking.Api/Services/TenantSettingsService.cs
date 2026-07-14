using GymBooking.Core.Contracts;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class TenantSettingsService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public TenantSettingsService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<TenantSettingsResponse?> GetAsync()
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == _currentTenant.TenantId);
        if (tenant is null)
        {
            return null;
        }
        return new TenantSettingsResponse(tenant.Name, tenant.CancellationHours);
    }

    public async Task<bool> UpdateAsync(UpdateTenantSettingsRequest request)
    {
        if (request.CancellationHours < 0 || string.IsNullOrWhiteSpace(request.Name))
        {
            return false;
        }
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == _currentTenant.TenantId);
        if (tenant is null)
        {
            return false;
        }
        tenant.Name = request.Name.Trim();
        tenant.CancellationHours = request.CancellationHours;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
