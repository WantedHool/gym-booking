namespace GymBooking.Core.Entities.Models;

public class ClassSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClassTypeId { get; set; }
    public Guid InstructorId { get; set; }
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsActive { get; set; } = true;
}
