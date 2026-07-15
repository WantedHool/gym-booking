namespace GymBooking.Core.Entities.Enums;

public enum WaitlistOutcome
{
    Joined,
    SessionNotFound,
    SessionNotFull,      // υπάρχει θέση → κάνε κανονική κράτηση, όχι waitlist
    AlreadyBooked,
    AlreadyOnWaitlist,
    NoSubscription,      // δεν έχεις ενεργή συνδρομή → δεν έχει νόημα να μπεις στη λίστα (δεν θα προωθηθείς)
    Left,
    NotOnWaitlist,
}
