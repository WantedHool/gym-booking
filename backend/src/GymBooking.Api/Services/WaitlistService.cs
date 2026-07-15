using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class WaitlistService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public WaitlistService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<WaitlistOutcome> JoinAsync(Guid userId, Guid classSessionId)
    {
        var session = await _dbContext.ClassSessions.FirstOrDefaultAsync(s => s.Id == classSessionId);
        if (session is null || !session.IsActive || session.IsCancelled)
        {
            return WaitlistOutcome.SessionNotFound;
        }

        if (session.BookedCount < session.Capacity)
        {
            return WaitlistOutcome.SessionNotFull;
        }

        var alreadyBooked = await _dbContext.Bookings
            .AnyAsync(b => b.UserId == userId && b.ClassSessionId == classSessionId && b.Status == BookingStatus.Confirmed);
        if (alreadyBooked)
        {
            return WaitlistOutcome.AlreadyBooked;
        }

        var alreadyWaiting = await _dbContext.WaitlistEntries
            .AnyAsync(w => w.UserId == userId && w.ClassSessionId == classSessionId && w.Status == WaitlistStatus.Waiting);
        if (alreadyWaiting)
        {
            return WaitlistOutcome.AlreadyOnWaitlist;
        }

        // Ίδια απαίτηση με την κράτηση: χωρίς ενεργή/χρήσιμη συνδρομή δεν προωθείσαι ποτέ από τη λίστα
        // (το auto-promote στο BookingService τον κόβει), άρα μπλοκάρουμε ήδη στην είσοδο.
        if (!await HasUsableSubscriptionAsync(userId))
        {
            return WaitlistOutcome.NoSubscription;
        }

        _dbContext.WaitlistEntries.Add(new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            UserId = userId,
            ClassSessionId = classSessionId,
            Status = WaitlistStatus.Waiting,
            CreatedAt = DateTime.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return WaitlistOutcome.Joined;
    }

    // Read-only έλεγχος (ΔΕΝ καταναλώνει θέση) — ίδια κριτήρια χρησιμότητας με το
    // BookingService.TryConsumeSubscriptionAsync: active + εντός ισχύος + διαθέσιμες θέσεις.
    // TenantId φιλτράρεται αυτόματα από το global query filter.
    private async Task<bool> HasUsableSubscriptionAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.Subscriptions
            .AnyAsync(s => s.UserId == userId
                && s.Status == SubscriptionStatus.Active
                && s.ValidFrom <= now
                && now <= s.ValidTo
                && (s.RemainingSessions == null || s.RemainingSessions > 0));
    }

    public async Task<WaitlistOutcome> LeaveAsync(Guid userId, Guid classSessionId)
    {
        var entry = await _dbContext.WaitlistEntries
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ClassSessionId == classSessionId && w.Status == WaitlistStatus.Waiting);
        if (entry is null)
        {
            return WaitlistOutcome.NotOnWaitlist;
        }

        entry.Status = WaitlistStatus.Left;
        await _dbContext.SaveChangesAsync();
        return WaitlistOutcome.Left;
    }

    public async Task<List<WaitlistEntryResponse>> GetMineAsync(Guid userId)
    {
        var mine = await (
            from w in _dbContext.WaitlistEntries
            where w.UserId == userId && w.Status == WaitlistStatus.Waiting
            join s in _dbContext.ClassSessions on w.ClassSessionId equals s.Id
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            select new { w.ClassSessionId, ClassTypeName = ct.Name, s.StartsAt, w.CreatedAt }).ToListAsync();

        var result = new List<WaitlistEntryResponse>();
        foreach (var m in mine)
        {
            // Θέση = πόσοι Waiting μπήκαν νωρίτερα στο ίδιο session, +1 (computed — κανένα stored Position).
            var earlier = await _dbContext.WaitlistEntries
                .CountAsync(w => w.ClassSessionId == m.ClassSessionId
                    && w.Status == WaitlistStatus.Waiting
                    && w.CreatedAt < m.CreatedAt);
            result.Add(new WaitlistEntryResponse(m.ClassSessionId, m.ClassTypeName, m.StartsAt, earlier + 1));
        }

        return result.OrderBy(r => r.StartsAt).ToList();
    }
}
