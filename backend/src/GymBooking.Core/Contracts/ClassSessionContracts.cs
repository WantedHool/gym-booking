namespace GymBooking.Core.Contracts;

public record CreateClassSessionRequest(Guid ClassTypeId, Guid? InstructorId, DateTime StartsAt, int DurationMinutes, int Capacity);
public record ClassSessionResponse(
    Guid Id,
    Guid ClassTypeId,
    string ClassTypeName,
    Guid InstructorId,
    string InstructorName,
    DateTime StartsAt,
    int DurationMinutes,
    int Capacity,
    int BookedCount,
    bool IsCancelled);
