using GymBooking.Core.Multitenancy;

namespace GymBooking.Api.Multitenancy;

public class CurrentTenant : ICurrentTenant
{
    public Guid TenantId { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
