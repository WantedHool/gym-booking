using GymBooking.Core.Entities.Enums;

namespace GymBooking.Core.Entities.Models;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid MembershipPlanId { get; set; }
    public int? RemainingSessions { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
}
