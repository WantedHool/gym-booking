using GymBooking.Api.Services;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class BookingTests
{
    private readonly PostgresApiFactory _factory;

    public BookingTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid TenantId, Guid SessionId)> SeedSessionAsync(int capacity, DateTime startsAtUtc, int durationMinutes = 60)
    {
        var tenantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var classType = new ClassType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Yoga",
            DefaultDurationMinutes = durationMinutes,
            DefaultCapacity = capacity,
            IsActive = true,
        };
        var session = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClassTypeId = classType.Id,
            InstructorId = Guid.NewGuid(),
            StartsAt = startsAtUtc,
            DurationMinutes = durationMinutes,
            Capacity = capacity,
            BookedCount = 0,
            IsActive = true,
        };
        db.ClassTypes.Add(classType);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return (tenantId, session.Id);
    }

    private async Task<(BookingOutcome Outcome, Booking? Booking)> BookAsync(Guid tenantId, Guid userId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var service = scope.ServiceProvider.GetRequiredService<BookingService>();
        return await service.BookAsync(userId, sessionId);
    }

    private async Task<ClassSession> GetSessionAsync(Guid tenantId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ClassSessions.FirstAsync(s => s.Id == sessionId);
    }

    [Fact]
    public async Task Book_available_session_succeeds_and_increments_bookedCount()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);

        var (outcome, booking) = await BookAsync(tenantId, userId, sessionId);

        Assert.Equal(BookingOutcome.Success, outcome);
        Assert.NotNull(booking);
        Assert.Equal(1, (await GetSessionAsync(tenantId, sessionId)).BookedCount);
    }

    [Fact]
    public async Task Book_full_session_returns_SessionFull()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var fillerId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, fillerId);
        await BookAsync(tenantId, fillerId, sessionId);

        var (outcome, _) = await BookAsync(tenantId, Guid.NewGuid(), sessionId);

        Assert.Equal(BookingOutcome.SessionFull, outcome);
        Assert.Equal(1, (await GetSessionAsync(tenantId, sessionId)).BookedCount);
    }

    private async Task<Guid> SeedAnotherSessionAsync(Guid tenantId, DateTime startsAtUtc, int durationMinutes = 60, int capacity = 5)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var classType = new ClassType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Spin",
            DefaultDurationMinutes = durationMinutes,
            DefaultCapacity = capacity,
            IsActive = true,
        };
        var session = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClassTypeId = classType.Id,
            InstructorId = Guid.NewGuid(),
            StartsAt = startsAtUtc,
            DurationMinutes = durationMinutes,
            Capacity = capacity,
            BookedCount = 0,
            IsActive = true,
        };
        db.ClassTypes.Add(classType);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    [Fact]
    public async Task Booking_same_session_twice_returns_AlreadyBooked()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);
        await BookAsync(tenantId, userId, sessionId);

        var (outcome, _) = await BookAsync(tenantId, userId, sessionId);

        Assert.Equal(BookingOutcome.AlreadyBooked, outcome);
    }

    [Fact]
    public async Task Booking_time_overlapping_session_returns_TimeConflict()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var (tenantId, sessionA) = await SeedSessionAsync(capacity: 5, startsAtUtc: start, durationMinutes: 60);
        var sessionB = await SeedAnotherSessionAsync(tenantId, start.AddMinutes(30), durationMinutes: 60);
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);
        await BookAsync(tenantId, userId, sessionA);

        var (outcome, _) = await BookAsync(tenantId, userId, sessionB);

        Assert.Equal(BookingOutcome.TimeConflict, outcome);
    }

    private async Task<(BookingOutcome Outcome, Booking? Booking)> CancelAsync(Guid tenantId, Guid userId, Guid bookingId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var service = scope.ServiceProvider.GetRequiredService<BookingService>();
        return await service.CancelAsync(userId, bookingId);
    }

    [Fact]
    public async Task Cancel_returns_the_spot_and_allows_rebooking()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);
        var (_, booking) = await BookAsync(tenantId, userId, sessionId);
        Assert.Equal(1, (await GetSessionAsync(tenantId, sessionId)).BookedCount);

        var (cancelOutcome, _) = await CancelAsync(tenantId, userId, booking!.Id);
        Assert.Equal(BookingOutcome.Success, cancelOutcome);
        Assert.Equal(0, (await GetSessionAsync(tenantId, sessionId)).BookedCount);

        var (rebookOutcome, _) = await BookAsync(tenantId, userId, sessionId);
        Assert.Equal(BookingOutcome.Success, rebookOutcome);
    }

    [Fact]
    public async Task Cancel_someone_elses_booking_is_rejected()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var bookerId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, bookerId);
        var (_, booking) = await BookAsync(tenantId, bookerId, sessionId);

        var (outcome, _) = await CancelAsync(tenantId, Guid.NewGuid(), booking!.Id);

        Assert.Equal(BookingOutcome.SessionNotFound, outcome);
        Assert.Equal(1, (await GetSessionAsync(tenantId, sessionId)).BookedCount);
    }

    [Fact]
    public async Task Concurrent_bookings_never_exceed_capacity()
    {
        const int capacity = 3;
        const int attempts = 12;
        var (tenantId, sessionId) = await SeedSessionAsync(capacity, DateTime.UtcNow.AddDays(1));

        var userIds = Enumerable.Range(0, attempts).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var uid in userIds)
        {
            await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, uid);
        }

        var tasks = userIds
            .Select(uid => BookAsync(tenantId, uid, sessionId))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        var successes = results.Count(r => r.Outcome == BookingOutcome.Success);
        var full = results.Count(r => r.Outcome == BookingOutcome.SessionFull);

        Assert.Equal(capacity, successes);
        Assert.Equal(attempts - capacity, full);
        Assert.Equal(capacity, (await GetSessionAsync(tenantId, sessionId)).BookedCount);
    }
}
