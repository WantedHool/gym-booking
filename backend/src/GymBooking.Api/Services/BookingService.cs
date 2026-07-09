using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class BookingService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public BookingService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<(BookingOutcome Outcome, Booking? Booking)> BookAsync(Guid userId, Guid classSessionId)
    {
        var tenantId = _currentTenant.TenantId;

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        // Κλειδώνει τη ΣΕΙΡΑ του session μέχρι το COMMIT — no overbooking σε ταυτόχρονες κρατήσεις.
        var session = await _dbContext.ClassSessions
            .FromSqlInterpolated($@"SELECT * FROM ""ClassSessions"" WHERE ""Id"" = {classSessionId} AND ""TenantId"" = {tenantId} FOR UPDATE")
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync();

        if (session is null || !session.IsActive)
        {
            return (BookingOutcome.SessionNotFound, null);
        }

        if (session.IsCancelled)
        {
            return (BookingOutcome.SessionCancelled, null);
        }

        var alreadyBooked = await _dbContext.Bookings
            .AnyAsync(b => b.UserId == userId && b.ClassSessionId == classSessionId && b.Status == BookingStatus.Confirmed);
        if (alreadyBooked)
        {
            return (BookingOutcome.AlreadyBooked, null);
        }

        // Overlap: φέρνουμε τα confirmed sessions του χρήστη στη μνήμη (λίγα) — το AddMinutes γίνεται σε C#.
        var newStart = session.StartsAt;
        var newEnd = session.StartsAt.AddMinutes(session.DurationMinutes);
        var userSessions = await (
            from b in _dbContext.Bookings
            where b.UserId == userId && b.Status == BookingStatus.Confirmed
            join s in _dbContext.ClassSessions on b.ClassSessionId equals s.Id
            select new { s.StartsAt, s.DurationMinutes }).ToListAsync();

        var overlaps = userSessions.Any(x => x.StartsAt < newEnd && newStart < x.StartsAt.AddMinutes(x.DurationMinutes));
        if (overlaps)
        {
            return (BookingOutcome.TimeConflict, null);
        }

        if (session.BookedCount >= session.Capacity)
        {
            return (BookingOutcome.SessionFull, null);
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ClassSessionId = classSessionId,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
        };
        _dbContext.Bookings.Add(booking);
        session.BookedCount += 1; // ΤΟ ΜΟΝΑΔΙΚΟ σημείο αλλαγής του BookedCount

        await _dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return (BookingOutcome.Success, booking);
    }

    public async Task<(BookingOutcome Outcome, Booking? Booking)> CancelAsync(Guid userId, Guid bookingId)
    {
        var tenantId = _currentTenant.TenantId;

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null || booking.UserId != userId)
        {
            return (BookingOutcome.SessionNotFound, null); // ownership: μη-δική-σου → σαν να μην υπάρχει
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return (BookingOutcome.Success, booking); // idempotent
        }

        var session = await _dbContext.ClassSessions
            .FromSqlInterpolated($@"SELECT * FROM ""ClassSessions"" WHERE ""Id"" = {booking.ClassSessionId} AND ""TenantId"" = {tenantId} FOR UPDATE")
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync();

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;

        if (session is not null && session.BookedCount > 0)
        {
            session.BookedCount -= 1; // επιστροφή θέσης
        }

        await _dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return (BookingOutcome.Success, booking);
    }

    public async Task<List<BookingResponse>> GetMineAsync(Guid userId)
    {
        return await (
            from b in _dbContext.Bookings
            where b.UserId == userId
            join s in _dbContext.ClassSessions on b.ClassSessionId equals s.Id
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            orderby s.StartsAt
            select new BookingResponse(
                b.Id,
                b.ClassSessionId,
                ct.Name,
                s.StartsAt,
                b.Status.ToString(),
                b.CreatedAt))
            .ToListAsync();
    }
}
