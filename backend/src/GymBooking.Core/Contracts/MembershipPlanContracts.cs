namespace GymBooking.Core.Contracts;

public record CreatePlanRequest(string Name, string Type, int SessionsCount, int DurationDays, decimal Price);
public record PlanResponse(Guid Id, string Name, string Type, int SessionsCount, int DurationDays, decimal Price, bool IsActive);
