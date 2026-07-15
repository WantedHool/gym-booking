namespace GymBooking.Core.Options;

public class SeedOptions
{
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public int CancellationHours { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public List<AdditionalTenantSeed> AdditionalTenants { get; set; } = new();
}

public class AdditionalTenantSeed
{
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public int CancellationHours { get; set; } = 24;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
