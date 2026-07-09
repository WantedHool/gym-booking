namespace GymBooking.Core.Entities.Models;

public class ClassType
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DefaultDurationMinutes { get; set; }
    public int DefaultCapacity { get; set; }
    public bool IsActive { get; set; } = true;
}
