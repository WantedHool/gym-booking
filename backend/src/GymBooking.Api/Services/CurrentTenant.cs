using GymBooking.Core.Multitenancy;

namespace GymBooking.Api.Services;

public class CurrentTenant : ICurrentTenant
{
    public Guid TenantId { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
