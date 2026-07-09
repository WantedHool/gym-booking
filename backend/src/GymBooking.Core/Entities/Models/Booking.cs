using GymBooking.Core.Entities.Enums;

namespace GymBooking.Core.Entities.Models;

public class Booking
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid ClassSessionId { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
