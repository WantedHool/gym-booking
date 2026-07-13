namespace GymBooking.Core.Contracts;

public record AssignSubscriptionRequest(string Email, Guid PlanId);
public record SubscriptionResponse(Guid Id, string PlanName, string Type, int? RemainingSessions, int? SessionsTotal, DateTime ValidFrom, DateTime ValidTo);
