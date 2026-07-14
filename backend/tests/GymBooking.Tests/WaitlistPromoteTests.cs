using GymBooking.Api.Services;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class WaitlistPromoteTests
{
    private readonly PostgresApiFactory _factory;

    public WaitlistPromoteTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid TenantId, Guid SessionId)> SeedSessionAsync(int capacity, DateTime startsAtUtc)
    {
        var tenantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ct = new ClassType { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Yoga", DefaultDurationMinutes = 60, DefaultCapacity = capacity, IsActive = true };
        var s = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = Guid.NewGuid(), StartsAt = startsAtUtc, DurationMinutes = 60, Capacity = capacity, BookedCount = 0, IsActive = true };
        db.ClassTypes.Add(ct);
        db.ClassSessions.Add(s);
        await db.SaveChangesAsync();
        return (tenantId, s.Id);
    }

    private BookingService Booking(IServiceScope scope, Guid tenantId)
    {
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        return scope.ServiceProvider.GetRequiredService<BookingService>();
    }

    private WaitlistService Waitlist(IServiceScope scope, Guid tenantId)
    {
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        return scope.ServiceProvider.GetRequiredService<WaitlistService>();
    }

    private async Task<bool> HasConfirmedBookingAsync(Guid tenantId, Guid userId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Bookings.AnyAsync(b => b.UserId == userId && b.ClassSessionId == sessionId && b.Status == BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Cancel_auto_promotes_first_waiting()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var holder = Guid.NewGuid();
        var waiter = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, holder);
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, waiter);

        Booking booking;
        using (var scope = _factory.Services.CreateScope())
        {
            var (_, b) = await Booking(scope, tenantId).BookAsync(holder, sessionId); // γεμίζει
            booking = b!;
        }
        using (var scope = _factory.Services.CreateScope())
        {
            await Waitlist(scope, tenantId).JoinAsync(waiter, sessionId);
        }
        using (var scope = _factory.Services.CreateScope())
        {
            await Booking(scope, tenantId).CancelAsync(holder, booking.Id); // ελευθερώνει → promote waiter
        }

        Assert.True(await HasConfirmedBookingAsync(tenantId, waiter, sessionId));
    }

    [Fact]
    public async Task Promote_skips_waiter_without_subscription_and_takes_next()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var holder = Guid.NewGuid();
        var noSub = Guid.NewGuid();      // 1ος στη λίστα, ΧΩΡΙΣ συνδρομή → skip
        var withSub = Guid.NewGuid();    // 2ος, με συνδρομή → προωθείται
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, holder);
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, withSub);

        Booking booking;
        using (var scope = _factory.Services.CreateScope())
        {
            var (_, b) = await Booking(scope, tenantId).BookAsync(holder, sessionId);
            booking = b!;
        }
        using (var scope = _factory.Services.CreateScope())
        {
            await Waitlist(scope, tenantId).JoinAsync(noSub, sessionId);   // μπαίνει 1ος
        }
        using (var scope = _factory.Services.CreateScope())
        {
            await Waitlist(scope, tenantId).JoinAsync(withSub, sessionId); // μπαίνει 2ος
        }
        using (var scope = _factory.Services.CreateScope())
        {
            await Booking(scope, tenantId).CancelAsync(holder, booking.Id);
        }

        Assert.False(await HasConfirmedBookingAsync(tenantId, noSub, sessionId));   // skipped
        Assert.True(await HasConfirmedBookingAsync(tenantId, withSub, sessionId));  // promoted
    }

    [Fact]
    public async Task Cancel_with_empty_waitlist_leaves_spot_open()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var holder = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, holder);

        Booking booking;
        using (var scope = _factory.Services.CreateScope())
        {
            var (_, b) = await Booking(scope, tenantId).BookAsync(holder, sessionId);
            booking = b!;
        }
        using (var scope = _factory.Services.CreateScope())
        {
            await Booking(scope, tenantId).CancelAsync(holder, booking.Id);
        }

        using var check = _factory.Services.CreateScope();
        check.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await db.ClassSessions.FirstAsync(s => s.Id == sessionId);
        Assert.Equal(0, session.BookedCount);
    }
}
