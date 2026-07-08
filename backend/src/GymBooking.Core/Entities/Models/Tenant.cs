namespace GymBooking.Core.Entities.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int CancellationHours { get; set; }
    public bool IsActive { get; set; } = true;
}
