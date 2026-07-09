namespace GymBooking.Core.Contracts;

public record CreateClassTypeRequest(string Name, string Description, int DefaultDurationMinutes, int DefaultCapacity);
public record UpdateClassTypeRequest(string Name, string Description, int DefaultDurationMinutes, int DefaultCapacity);
public record ClassTypeResponse(Guid Id, string Name, string Description, int DefaultDurationMinutes, int DefaultCapacity, bool IsActive);
