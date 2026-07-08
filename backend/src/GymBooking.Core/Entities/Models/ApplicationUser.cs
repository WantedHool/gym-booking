using GymBooking.Core.Entities.Enums;
using Microsoft.AspNetCore.Identity;

namespace GymBooking.Core.Entities.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsActive { get; set; } = true;
}
