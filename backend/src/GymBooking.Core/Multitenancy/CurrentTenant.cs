namespace GymBooking.Core.Multitenancy;

public class CurrentTenant : ICurrentTenant
{
    public Guid TenantId { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
