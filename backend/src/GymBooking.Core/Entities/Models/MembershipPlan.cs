using GymBooking.Core.Entities.Enums;

namespace GymBooking.Core.Entities.Models;

public class MembershipPlan
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlanType Type { get; set; }
    public int SessionsCount { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
