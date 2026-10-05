using GymBooking.Api.Services;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class BookingSubscriptionTests
{
    private readonly PostgresApiFactory _factory;

    public BookingSubscriptionTests(PostgresApiFactory factory)
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

    private async Task<(BookingOutcome Outcome, Booking? Booking)> CancelAsync(Guid tenantId, Guid userId, Guid bookingId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var service = scope.ServiceProvider.GetRequiredService<BookingService>();
        return await service.CancelAsync(userId, bookingId);
    }

    [Fact]
    public async Task Booking_without_subscription_returns_NoSubscription()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));

        var (outcome, _) = await BookAsync(tenantId, Guid.NewGuid(), sessionId);

        Assert.Equal(BookingOutcome.NoSubscription, outcome);
    }

    [Fact]
    public async Task SessionPack_decrements_on_book_and_refunds_on_cancel()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var userId = Guid.NewGuid();
        await TestData.GiveSessionPackAsync(_factory.Services, tenantId, userId, sessions: 3);

        var (bookOutcome, booking) = await BookAsync(tenantId, userId, sessionId);
        Assert.Equal(BookingOutcome.Success, bookOutcome);
        Assert.Equal(2, await TestData.GetRemainingAsync(_factory.Services, tenantId, userId));

        await CancelAsync(tenantId, userId, booking!.Id);
        Assert.Equal(3, await TestData.GetRemainingAsync(_factory.Services, tenantId, userId));
    }

    [Fact]
    public async Task Unlimited_does_not_decrement()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5, startsAtUtc: DateTime.UtcNow.AddDays(1));
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);

        var (outcome, _) = await BookAsync(tenantId, userId, sessionId);

        Assert.Equal(BookingOutcome.Success, outcome);
        Assert.Null(await TestData.GetRemainingAsync(_factory.Services, tenantId, userId));
    }
}
