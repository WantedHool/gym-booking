namespace GymBooking.Core.Entities.Enums;

public enum WaitlistStatus
{
    Waiting,     // = 0
    Promoted,    // = 1
    Left,        // = 2  (αποχώρησε ή skipped στο promote)
}
