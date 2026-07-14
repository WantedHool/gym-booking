namespace GymBooking.Core.Contracts;

public record RosterBooking(Guid BookingId, Guid UserId, string Name, string Email, DateTime CreatedAt);
public record RosterWaiting(Guid UserId, string Name, int Position);
public record RosterResponse(
    Guid ClassSessionId,
    string ClassTypeName,
    DateTime StartsAt,
    int Capacity,
    int BookedCount,
    IReadOnlyList<RosterBooking> Confirmed,
    IReadOnlyList<RosterWaiting> Waitlist);

public record StaffBookRequest(Guid UserId);
