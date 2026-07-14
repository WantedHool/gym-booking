using GymBooking.Core.Entities.Enums;

namespace GymBooking.Core.Entities.Models;

public class WaitlistEntry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid ClassSessionId { get; set; }
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;
    public DateTime CreatedAt { get; set; }
}
