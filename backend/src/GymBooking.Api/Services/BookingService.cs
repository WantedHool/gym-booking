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
        // Το Program.cs ενεργοποιεί EnableRetryOnFailure στο Npgsql provider (retry σε παροδικά
        // connection errors). Όταν υπάρχει retrying execution strategy, η EF Core ΑΠΑΓΟΡΕΥΕΙ
        // χειροκίνητο Database.BeginTransactionAsync() εκτός αν όλο το transaction τρέχει μέσα
        // στο strategy.ExecuteAsync — αλλιώς ρίχνει InvalidOperationException at runtime
        // (δεν το πιάνει το InMemory/no-retry test harness, μόνο πραγματικό Postgres+retry config).
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<(BookingOutcome Outcome, Booking? Booking)>(async () =>
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

            // --- Φ5: consumption συνδρομής (κλείδωμα της γραμμής subscription μέσα στο ίδιο tx) ---
            var subscription = await _dbContext.Subscriptions
                .FromSqlInterpolated($@"SELECT * FROM ""Subscriptions"" WHERE ""UserId"" = {userId} AND ""TenantId"" = {tenantId} AND ""Status"" = 0 ORDER BY ""ValidFrom"" DESC LIMIT 1 FOR UPDATE")
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync();

            var now = DateTime.UtcNow;
            var usable = subscription is not null
                && subscription.ValidFrom <= now && now <= subscription.ValidTo
                && (subscription.RemainingSessions == null || subscription.RemainingSessions > 0);
            if (!usable)
            {
                return (BookingOutcome.NoSubscription, null);
            }

            if (subscription!.RemainingSessions != null)
            {
                subscription.RemainingSessions -= 1; // SessionPack
            }
            // --- τέλος consumption ---

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                ClassSessionId = classSessionId,
                Status = BookingStatus.Confirmed,
                CreatedAt = now,
                SubscriptionId = subscription.Id, // Φ5: για ακριβές refund
            };
            _dbContext.Bookings.Add(booking);
            session.BookedCount += 1; // ΤΟ ΜΟΝΑΔΙΚΟ σημείο αλλαγής του BookedCount

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return (BookingOutcome.Success, booking);
        });
    }

    public async Task<(BookingOutcome Outcome, Booking? Booking)> CancelAsync(Guid userId, Guid bookingId)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<(BookingOutcome Outcome, Booking? Booking)>(async () =>
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

            // Φ5: refund θέσης προπόνησης στη συνδρομή που καταναλώθηκε (SessionPack μόνο).
            if (booking.SubscriptionId is Guid subId)
            {
                var subscription = await _dbContext.Subscriptions
                    .FromSqlInterpolated($@"SELECT * FROM ""Subscriptions"" WHERE ""Id"" = {subId} AND ""TenantId"" = {tenantId} FOR UPDATE")
                    .IgnoreQueryFilters()
                    .AsTracking()
                    .FirstOrDefaultAsync();
                if (subscription is not null && subscription.RemainingSessions != null)
                {
                    subscription.RemainingSessions += 1;
                }
            }

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return (BookingOutcome.Success, booking);
        });
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
