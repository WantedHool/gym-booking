namespace GymBooking.Core.Entities.Models;

public class ClassSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClassTypeId { get; set; }
    public Guid InstructorId { get; set; }
    public DateTime StartsAt { get; set; }        // UTC
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }          // αρχικά 0· θα το αυξάνει το booking της Φ3
    public bool IsCancelled { get; set; }
    public bool IsActive { get; set; } = true;
}
