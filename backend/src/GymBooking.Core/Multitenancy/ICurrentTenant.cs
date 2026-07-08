namespace GymBooking.Core.Multitenancy;

public interface ICurrentTenant
{
    Guid TenantId { get; }
}
