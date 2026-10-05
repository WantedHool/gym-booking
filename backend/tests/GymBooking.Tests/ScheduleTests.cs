using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class ScheduleTests
{
    private readonly PostgresApiFactory _factory;

    public ScheduleTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Seeded(Guid TenantId, Guid ClassTypeId, Guid InstructorId, Guid SessionThisWeek, Guid SessionNextWeek);

    private async Task<Seeded> SeedAsync(DateTime weekStartUtc)
    {
        var tenantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var instructor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = $"instr-{tenantId}@demo.gym",
            Email = $"instr-{tenantId}@demo.gym",
            FirstName = "Maria",
            LastName = "Papadopoulou",
        };
        var ct = new ClassType { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Yoga", DefaultDurationMinutes = 60, DefaultCapacity = 10, IsActive = true };
        var s1 = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = instructor.Id, StartsAt = weekStartUtc.AddDays(1).AddHours(18), DurationMinutes = 60, Capacity = 10, IsActive = true };
        var s2 = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = instructor.Id, StartsAt = weekStartUtc.AddDays(8).AddHours(18), DurationMinutes = 60, Capacity = 10, IsActive = true };

        db.Users.Add(instructor);
        db.ClassTypes.Add(ct);
        db.ClassSessions.AddRange(s1, s2);
        await db.SaveChangesAsync();

        return new Seeded(tenantId, ct.Id, instructor.Id, s1.Id, s2.Id);
    }

    private async Task<List<ScheduleSessionResponse>> GetScheduleAsync(
        Guid tenantId, Guid userId, DateTime fromUtc, DateTime toUtc, Guid? classTypeId = null)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var svc = scope.ServiceProvider.GetRequiredService<ScheduleService>();
        return await svc.GetScheduleAsync(userId, fromUtc, toUtc, classTypeId, null);
    }

    [Fact]
    public async Task Schedule_returns_only_this_week_with_booked_flag()
    {
        var weekStart = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);
        var seeded = await SeedAsync(weekStart);
        var userId = Guid.NewGuid();

        await TestData.GiveUnlimitedAsync(_factory.Services, seeded.TenantId, userId);
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(seeded.TenantId);
            var booking = scope.ServiceProvider.GetRequiredService<BookingService>();
            await booking.BookAsync(userId, seeded.SessionThisWeek);
        }

        var result = await GetScheduleAsync(seeded.TenantId, userId, weekStart, weekStart.AddDays(7));

        Assert.Single(result);
        Assert.Equal(seeded.SessionThisWeek, result[0].Id);
        Assert.True(result[0].IsBookedByMe);
        Assert.NotNull(result[0].MyBookingId);
        Assert.Equal("Maria Papadopoulou", result[0].InstructorName);
    }

    [Fact]
    public async Task Schedule_filters_by_class_type()
    {
        var weekStart = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);
        var seeded = await SeedAsync(weekStart);

        var none = await GetScheduleAsync(seeded.TenantId, Guid.NewGuid(), weekStart, weekStart.AddDays(7), classTypeId: Guid.NewGuid());
        Assert.Empty(none);

        var some = await GetScheduleAsync(seeded.TenantId, Guid.NewGuid(), weekStart, weekStart.AddDays(7), classTypeId: seeded.ClassTypeId);
        Assert.Single(some);
    }
}
