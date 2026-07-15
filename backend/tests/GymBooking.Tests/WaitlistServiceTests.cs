using GymBooking.Api.Services;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class WaitlistServiceTests
{
    private readonly PostgresApiFactory _factory;

    public WaitlistServiceTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid TenantId, Guid SessionId)> SeedSessionAsync(int capacity)
    {
        var tenantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ct = new ClassType { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Yoga", DefaultDurationMinutes = 60, DefaultCapacity = capacity, IsActive = true };
        var s = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = Guid.NewGuid(), StartsAt = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Capacity = capacity, BookedCount = 0, IsActive = true };
        db.ClassTypes.Add(ct);
        db.ClassSessions.Add(s);
        await db.SaveChangesAsync();
        return (tenantId, s.Id);
    }

    private async Task FillSessionAsync(Guid tenantId, Guid sessionId, Guid fillerId)
    {
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, fillerId);
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        await scope.ServiceProvider.GetRequiredService<BookingService>().BookAsync(fillerId, sessionId);
    }

    private async Task<WaitlistOutcome> JoinAsync(Guid tenantId, Guid userId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        return await scope.ServiceProvider.GetRequiredService<WaitlistService>().JoinAsync(userId, sessionId);
    }

    [Fact]
    public async Task Join_full_session_succeeds()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1);
        await FillSessionAsync(tenantId, sessionId, Guid.NewGuid());
        var joiner = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, joiner);

        var outcome = await JoinAsync(tenantId, joiner, sessionId);

        Assert.Equal(WaitlistOutcome.Joined, outcome);
    }

    [Fact]
    public async Task Join_without_subscription_returns_NoSubscription()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1);
        await FillSessionAsync(tenantId, sessionId, Guid.NewGuid());

        var outcome = await JoinAsync(tenantId, Guid.NewGuid(), sessionId);

        Assert.Equal(WaitlistOutcome.NoSubscription, outcome);
    }

    [Fact]
    public async Task Join_session_with_space_returns_SessionNotFull()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 5);

        var outcome = await JoinAsync(tenantId, Guid.NewGuid(), sessionId);

        Assert.Equal(WaitlistOutcome.SessionNotFull, outcome);
    }

    [Fact]
    public async Task Join_twice_returns_AlreadyOnWaitlist()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1);
        await FillSessionAsync(tenantId, sessionId, Guid.NewGuid());
        var userId = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, userId);
        await JoinAsync(tenantId, userId, sessionId);

        var outcome = await JoinAsync(tenantId, userId, sessionId);

        Assert.Equal(WaitlistOutcome.AlreadyOnWaitlist, outcome);
    }

    [Fact]
    public async Task GetMine_reports_fifo_position()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1);
        await FillSessionAsync(tenantId, sessionId, Guid.NewGuid());
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, first);
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, second);
        await JoinAsync(tenantId, first, sessionId);
        await JoinAsync(tenantId, second, sessionId);

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var svc = scope.ServiceProvider.GetRequiredService<WaitlistService>();
        var firstMine = await svc.GetMineAsync(first);
        var secondMine = await svc.GetMineAsync(second);

        Assert.Equal(1, firstMine.Single().Position);
        Assert.Equal(2, secondMine.Single().Position);
    }
}
