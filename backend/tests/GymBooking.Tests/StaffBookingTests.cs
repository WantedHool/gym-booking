using GymBooking.Api.Services;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class StaffBookingTests
{
    private readonly PostgresApiFactory _factory;

    public StaffBookingTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid TenantId, Guid SessionId)> SeedSessionAsync(int capacity, DateTime startsAtUtc, int bookedCount = 0, int durationMinutes = 60)
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
            BookedCount = bookedCount,
            IsActive = true,
        };
        db.ClassTypes.Add(classType);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return (tenantId, session.Id);
    }

    private async Task SeedUserAsync(Guid tenantId, Guid userId, string firstName, string lastName)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            UserName = $"{userId}@demo.gym",
            Email = $"{userId}@demo.gym",
            FirstName = firstName,
            LastName = lastName,
        });
        await db.SaveChangesAsync();
    }

    private async Task<Booking> SeedBookingAsync(Guid tenantId, Guid userId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ClassSessionId = sessionId,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private async Task<ClassSession> GetSessionAsync(Guid tenantId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ClassSessions.FirstAsync(s => s.Id == sessionId);
    }

    [Fact]
    public async Task GetRoster_returns_confirmed_bookings_with_user_names()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 10, startsAtUtc: DateTime.UtcNow.AddDays(1), bookedCount: 1);
        var userId = Guid.NewGuid();
        await SeedUserAsync(tenantId, userId, "Mel", "Loi");
        await SeedBookingAsync(tenantId, userId, sessionId);

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var service = scope.ServiceProvider.GetRequiredService<BookingService>();
        var roster = await service.GetRosterAsync(sessionId);

        Assert.NotNull(roster);
        Assert.Single(roster!.Confirmed);
        Assert.Equal("Mel Loi", roster.Confirmed[0].Name);
        Assert.Equal("Yoga", roster.ClassTypeName);
    }

    [Fact]
    public async Task BookFor_returns_SessionFull_when_capacity_reached()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 1, startsAtUtc: DateTime.UtcNow.AddDays(1), bookedCount: 1);
        var userId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var service = scope.ServiceProvider.GetRequiredService<BookingService>();
        var (outcome, _) = await service.BookForAsync(userId, sessionId);

        Assert.Equal(BookingOutcome.SessionFull, outcome);
    }

    [Fact]
    public async Task CancelByStaff_cancels_without_ownership_check()
    {
        var (tenantId, sessionId) = await SeedSessionAsync(capacity: 10, startsAtUtc: DateTime.UtcNow.AddDays(1), bookedCount: 1);
        var ownerId = Guid.NewGuid();
        var booking = await SeedBookingAsync(tenantId, ownerId, sessionId);

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var service = scope.ServiceProvider.GetRequiredService<BookingService>();
        var (outcome, _) = await service.CancelByStaffAsync(booking.Id);

        Assert.Equal(BookingOutcome.Success, outcome);
        Assert.Equal(0, (await GetSessionAsync(tenantId, sessionId)).BookedCount);
    }
}
