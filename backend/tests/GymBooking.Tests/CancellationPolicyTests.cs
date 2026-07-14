using GymBooking.Api.Services;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class CancellationPolicyTests
{
    private readonly PostgresApiFactory _factory;

    public CancellationPolicyTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid TenantId, Guid SessionId)> SeedAsync(int cancellationHours, DateTime startsAtUtc, int capacity = 5)
    {
        var tenantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = tenantId.ToString(), CancellationHours = cancellationHours, IsActive = true });
        var ct = new ClassType { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Yoga", DefaultDurationMinutes = 60, DefaultCapacity = capacity, IsActive = true };
        var s = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = Guid.NewGuid(), StartsAt = startsAtUtc, DurationMinutes = 60, Capacity = capacity, BookedCount = 0, IsActive = true };
        db.ClassTypes.Add(ct);
        db.ClassSessions.Add(s);
        await db.SaveChangesAsync();
        return (tenantId, s.Id);
    }

    private async Task<(BookingOutcome Outcome, Booking? Booking)> BookAsync(Guid tenantId, Guid userId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BookingService>().BookAsync(userId, sessionId);
    }

    private async Task<(BookingOutcome Outcome, Booking? Booking)> CancelAsync(Guid tenantId, Guid userId, Guid bookingId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BookingService>().CancelAsync(userId, bookingId);
    }

    [Fact]
    public async Task Cancel_within_window_is_blocked()
    {
        var (tenantId, sessionId) = await SeedAsync(cancellationHours: 24, startsAtUtc: DateTime.UtcNow.AddHours(2));
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);
        var (_, booking) = await BookAsync(tenantId, userId, sessionId);

        var (outcome, _) = await CancelAsync(tenantId, userId, booking!.Id);

        Assert.Equal(BookingOutcome.CancellationTooLate, outcome);
    }

    [Fact]
    public async Task Cancel_outside_window_succeeds()
    {
        var (tenantId, sessionId) = await SeedAsync(cancellationHours: 24, startsAtUtc: DateTime.UtcNow.AddDays(2));
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);
        var (_, booking) = await BookAsync(tenantId, userId, sessionId);

        var (outcome, _) = await CancelAsync(tenantId, userId, booking!.Id);

        Assert.Equal(BookingOutcome.Success, outcome);
    }

    [Fact]
    public async Task Cancellation_hours_zero_allows_anytime()
    {
        var (tenantId, sessionId) = await SeedAsync(cancellationHours: 0, startsAtUtc: DateTime.UtcNow.AddHours(1));
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);
        var (_, booking) = await BookAsync(tenantId, userId, sessionId);

        var (outcome, _) = await CancelAsync(tenantId, userId, booking!.Id);

        Assert.Equal(BookingOutcome.Success, outcome);
    }
}
