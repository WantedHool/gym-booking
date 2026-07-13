namespace GymBooking.Core.Contracts;

public record ScheduleSessionResponse(
    Guid Id,
    Guid ClassTypeId,
    string ClassTypeName,
    Guid InstructorId,
    string InstructorName,
    DateTime StartsAt,
    int DurationMinutes,
    int Capacity,
    int BookedCount,
    bool IsBookedByMe,
    Guid? MyBookingId);
