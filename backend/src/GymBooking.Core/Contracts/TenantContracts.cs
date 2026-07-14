namespace GymBooking.Core.Contracts;

public record TenantSettingsResponse(string Name, int CancellationHours);
public record UpdateTenantSettingsRequest(string Name, int CancellationHours);
