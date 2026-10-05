namespace GymBooking.Core.Entities.Enums;

public enum WaitlistOutcome
{
    Joined,
    SessionNotFound,
    SessionNotFull,
    AlreadyBooked,
    AlreadyOnWaitlist,
    NoSubscription,
    Left,
    NotOnWaitlist,
}
