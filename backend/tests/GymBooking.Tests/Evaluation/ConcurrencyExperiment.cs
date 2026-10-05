using System.Diagnostics;
using GymBooking.Api.Services;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GymBooking.Tests.Evaluation;

[Collection("Evaluation")]
public class ConcurrencyExperiment
{
    private const int Capacity = 5;
    private const int Repetitions = 10;
    private static readonly int[] Attempts = { 10, 50, 100 };

    private readonly EvaluationApiFactory _factory;
    private readonly ITestOutputHelper _output;

    public ConcurrencyExperiment(EvaluationApiFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [EvaluationFact]
    public async Task Locked_booking_vs_naive_baseline()
    {
        EvaluationSupport.WriteEnvironment();
        var rows = new List<string>();

        foreach (var variant in new[] { "locked", "naive" })
        {
            foreach (var n in Attempts)
            {
                for (var rep = 1; rep <= Repetitions; rep++)
                {
                    var r = await RunOnceAsync(variant, n);
                    rows.Add(string.Join(",", variant, n, Capacity, rep, r.Successes, r.ConfirmedInDb,
                        r.BookedCountColumn, Math.Max(0, r.ConfirmedInDb - Capacity), r.Errors, EvaluationSupport.F(r.DurationMs)));
                    _output.WriteLine($"{variant} N={n} rep={rep}: success={r.Successes} confirmed={r.ConfirmedInDb} bookedCount={r.BookedCountColumn} errors={r.Errors} {r.DurationMs:0}ms");
                }
            }
        }

        EvaluationSupport.WriteCsv("ee1_concurrency.csv",
            "variant,attempts,capacity,rep,successes,confirmed_in_db,booked_count_column,overbooked,errors,duration_ms", rows);

        Assert.DoesNotContain(rows, row => row.StartsWith("locked,") && row.Split(',')[7] != "0");
    }

    private async Task<RunResult> RunOnceAsync(string variant, int attempts)
    {
        var tenantId = Guid.NewGuid();
        var instructorId = await EvaluationSupport.SeedInstructorAsync(_factory.Services, tenantId);
        var sessionId = (await EvaluationSupport.SeedSessionsAsync(_factory.Services, tenantId, instructorId, 1, Capacity, DateTime.UtcNow.AddDays(2)))[0];
        var users = Enumerable.Range(0, attempts).Select(_ => Guid.NewGuid()).ToList();
        await EvaluationSupport.GiveUnlimitedToAllAsync(_factory.Services, tenantId, users);

        var sw = Stopwatch.StartNew();
        var outcomes = await EvaluationSupport.RunConcurrentlyAsync(attempts, async i =>
        {
            try
            {
                if (variant == "locked")
                {
                    return await LockedBookAsync(tenantId, users[i], sessionId) ? "ok" : "rejected";
                }

                return await NaiveBookAsync(tenantId, users[i], sessionId) ? "ok" : "rejected";
            }
            catch (Exception)
            {
                return "error";
            }
        });
        sw.Stop();

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var confirmed = await db.Bookings.CountAsync(b => b.ClassSessionId == sessionId && b.Status == BookingStatus.Confirmed);
        var session = await db.ClassSessions.FirstAsync(s => s.Id == sessionId);

        return new RunResult(
            outcomes.Count(o => o == "ok"),
            confirmed,
            session.BookedCount,
            outcomes.Count(o => o == "error"),
            sw.Elapsed.TotalMilliseconds);
    }

    private async Task<bool> LockedBookAsync(Guid tenantId, Guid userId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var service = scope.ServiceProvider.GetRequiredService<BookingService>();
        var (outcome, _) = await service.BookAsync(userId, sessionId);
        return outcome == BookingOutcome.Success;
    }

    private async Task<bool> NaiveBookAsync(Guid tenantId, Guid userId, Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var session = await db.ClassSessions.AsTracking().FirstAsync(s => s.Id == sessionId);
        if (session.BookedCount >= session.Capacity)
        {
            return false;
        }

        var hasSubscription = await db.Subscriptions.AnyAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active);
        if (!hasSubscription)
        {
            return false;
        }

        db.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ClassSessionId = sessionId,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
        });
        session.BookedCount += 1;
        await db.SaveChangesAsync();
        return true;
    }

    private sealed record RunResult(int Successes, int ConfirmedInDb, int BookedCountColumn, int Errors, double DurationMs);
}
