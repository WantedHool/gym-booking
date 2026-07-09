namespace GymBooking.Core.Entities.Enums;

public enum BookingOutcome
{
    Success,
    SessionNotFound,
    SessionCancelled,
    SessionFull,
    AlreadyBooked,
    TimeConflict,
}
