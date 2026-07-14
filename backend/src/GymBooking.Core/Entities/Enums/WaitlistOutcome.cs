namespace GymBooking.Core.Entities.Enums;

public enum WaitlistOutcome
{
    Joined,
    SessionNotFound,
    SessionNotFull,      // υπάρχει θέση → κάνε κανονική κράτηση, όχι waitlist
    AlreadyBooked,
    AlreadyOnWaitlist,
    Left,
    NotOnWaitlist,
}
