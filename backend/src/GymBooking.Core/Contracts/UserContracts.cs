namespace GymBooking.Core.Contracts;

public record UserListItem(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTime CreatedAt);

public record ChangeRoleRequest(string Role);
