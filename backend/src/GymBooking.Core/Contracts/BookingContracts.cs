namespace GymBooking.Core.Contracts;

public record CreateBookingRequest(Guid ClassSessionId);
public record BookingResponse(Guid Id, Guid ClassSessionId, string ClassTypeName, DateTime StartsAt, string Status, DateTime CreatedAt);
public record WaitlistEntryResponse(Guid ClassSessionId, string ClassTypeName, DateTime StartsAt, int Position);
