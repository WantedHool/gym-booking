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

            if (await HasTimeConflictAsync(userId, session))
            {
                return (BookingOutcome.TimeConflict, null);
            }

            if (session.BookedCount >= session.Capacity)
            {
                return (BookingOutcome.SessionFull, null);
            }

            var (usable, subscriptionId) = await TryConsumeSubscriptionAsync(userId, tenantId);
            if (!usable)
            {
                return (BookingOutcome.NoSubscription, null);
            }

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                ClassSessionId = classSessionId,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                SubscriptionId = subscriptionId,
            };
            _dbContext.Bookings.Add(booking);
            session.BookedCount += 1; // ΤΟ ΜΟΝΑΔΙΚΟ σημείο αλλαγής του BookedCount (book)

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return (BookingOutcome.Success, booking);
        });
    }

    // Staff walk-in: ίδια atomic ροή με το self-service BookAsync· ο userId έρχεται από τον staff.
    public async Task<(BookingOutcome Outcome, Booking? Booking)> BookForAsync(Guid userId, Guid classSessionId)
    {
        return await BookAsync(userId, classSessionId);
    }

    public async Task<(BookingOutcome Outcome, Booking? Booking)> CancelAsync(Guid userId, Guid bookingId)
    {
        return await CancelCoreAsync(bookingId, ownerUserId: userId, enforceWindow: true);
    }

    // Staff: καμία ιδιοκτησία, καμία πολιτική παραθύρου.
    public async Task<(BookingOutcome Outcome, Booking? Booking)> CancelByStaffAsync(Guid bookingId)
    {
        return await CancelCoreAsync(bookingId, ownerUserId: null, enforceWindow: false);
    }

    private async Task<(BookingOutcome Outcome, Booking? Booking)> CancelCoreAsync(Guid bookingId, Guid? ownerUserId, bool enforceWindow)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<(BookingOutcome Outcome, Booking? Booking)>(async () =>
        {
            var tenantId = _currentTenant.TenantId;

            await using var tx = await _dbContext.Database.BeginTransactionAsync();

            var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking is null)
            {
                return (BookingOutcome.SessionNotFound, null);
            }
            if (ownerUserId is Guid oid && booking.UserId != oid)
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

            // Φ6: πολιτική ακύρωσης — block αν είμαστε πολύ κοντά στην ώρα του μαθήματος.
            if (enforceWindow && session is not null)
            {
                var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
                var cancellationHours = tenant?.CancellationHours ?? 0;
                if (cancellationHours > 0 && DateTime.UtcNow > session.StartsAt.AddHours(-cancellationHours))
                {
                    return (BookingOutcome.CancellationTooLate, booking);
                }
            }

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

            // --- Φ6: auto-promote επόμενου έγκυρου της λίστας αναμονής (μέσα στο ίδιο tx) ---
            if (session is not null && session.BookedCount < session.Capacity)
            {
                var waiting = await _dbContext.WaitlistEntries
                    .Where(w => w.ClassSessionId == booking.ClassSessionId && w.Status == WaitlistStatus.Waiting)
                    .OrderBy(w => w.CreatedAt)
                    .ToListAsync();

                foreach (var entry in waiting)
                {
                    var entryAlreadyBooked = await _dbContext.Bookings
                        .AnyAsync(b => b.UserId == entry.UserId && b.ClassSessionId == booking.ClassSessionId && b.Status == BookingStatus.Confirmed);
                    if (entryAlreadyBooked)
                    {
                        entry.Status = WaitlistStatus.Left;
                        continue;
                    }

                    if (await HasTimeConflictAsync(entry.UserId, session))
                    {
                        entry.Status = WaitlistStatus.Left;
                        continue;
                    }

                    var (entryUsable, entrySubscriptionId) = await TryConsumeSubscriptionAsync(entry.UserId, tenantId);
                    if (!entryUsable)
                    {
                        entry.Status = WaitlistStatus.Left;
                        continue;
                    }

                    _dbContext.Bookings.Add(new Booking
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        UserId = entry.UserId,
                        ClassSessionId = booking.ClassSessionId,
                        Status = BookingStatus.Confirmed,
                        CreatedAt = DateTime.UtcNow,
                        SubscriptionId = entrySubscriptionId,
                    });
                    session.BookedCount += 1; // επιστροφή θέσης → προωθημένος
                    entry.Status = WaitlistStatus.Promoted;
                    break;
                }
            }
            // --- τέλος auto-promote ---

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return (BookingOutcome.Success, booking);
        });
    }

    public async Task<RosterResponse?> GetRosterAsync(Guid classSessionId)
    {
        var session = await (
            from s in _dbContext.ClassSessions
            where s.Id == classSessionId
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            select new { s.Id, ClassTypeName = ct.Name, s.StartsAt, s.Capacity, s.BookedCount }).FirstOrDefaultAsync();
        if (session is null)
        {
            return null;
        }

        var confirmed = await (
            from b in _dbContext.Bookings
            where b.ClassSessionId == classSessionId && b.Status == BookingStatus.Confirmed
            join u in _dbContext.Users on b.UserId equals u.Id
            orderby b.CreatedAt
            select new RosterBooking(b.Id, u.Id, u.FirstName + " " + u.LastName, u.Email ?? string.Empty, b.CreatedAt))
            .ToListAsync();

        var waitingRaw = await (
            from w in _dbContext.WaitlistEntries
            where w.ClassSessionId == classSessionId && w.Status == WaitlistStatus.Waiting
            join u in _dbContext.Users on w.UserId equals u.Id
            orderby w.CreatedAt
            select new { u.Id, Name = u.FirstName + " " + u.LastName }).ToListAsync();

        var waitlist = waitingRaw
            .Select((w, i) => new RosterWaiting(w.Id, w.Name, i + 1))
            .ToList();

        return new RosterResponse(
            session.Id, session.ClassTypeName, session.StartsAt,
            session.Capacity, session.BookedCount, confirmed, waitlist);
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

    // Χρονική επικάλυψη με άλλη confirmed κράτηση του χρήστη (τα confirmed sessions είναι λίγα → in-memory).
    private async Task<bool> HasTimeConflictAsync(Guid userId, ClassSession session)
    {
        var newStart = session.StartsAt;
        var newEnd = session.StartsAt.AddMinutes(session.DurationMinutes);
        var userSessions = await (
            from b in _dbContext.Bookings
            where b.UserId == userId && b.Status == BookingStatus.Confirmed
            join s in _dbContext.ClassSessions on b.ClassSessionId equals s.Id
            select new { s.StartsAt, s.DurationMinutes }).ToListAsync();

        return userSessions.Any(x => x.StartsAt < newEnd && newStart < x.StartsAt.AddMinutes(x.DurationMinutes));
    }

    // Κλειδώνει (FOR UPDATE) & καταναλώνει τη μοναδική active συνδρομή· επιστρέφει αν είναι χρήσιμη + το Id της.
    private async Task<(bool Usable, Guid? SubscriptionId)> TryConsumeSubscriptionAsync(Guid userId, Guid tenantId)
    {
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
            return (false, null);
        }

        if (subscription!.RemainingSessions != null)
        {
            subscription.RemainingSessions -= 1;
        }

        return (true, subscription.Id);
    }
}
