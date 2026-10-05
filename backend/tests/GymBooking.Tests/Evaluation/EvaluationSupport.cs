using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests.Evaluation;

internal static class EvaluationSupport
{
    public static async Task<Guid> SeedInstructorAsync(IServiceProvider services, Guid tenantId)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid();
        var email = "instructor-" + id.ToString("N") + "@eval.gym";
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            TenantId = tenantId,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FirstName = "Eval",
            LastName = "Instructor",
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        await db.SaveChangesAsync();
        return id;
    }

    public static async Task<List<Guid>> SeedSessionsAsync(
        IServiceProvider services, Guid tenantId, Guid instructorId, int count, int capacity, DateTime firstStartUtc)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var classType = new ClassType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Eval class",
            DefaultDurationMinutes = 60,
            DefaultCapacity = capacity,
            IsActive = true,
        };
        db.ClassTypes.Add(classType);

        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var session = new ClassSession
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ClassTypeId = classType.Id,
                InstructorId = instructorId,
                StartsAt = firstStartUtc,
                DurationMinutes = 60,
                Capacity = capacity,
                BookedCount = 0,
                IsActive = true,
            };
            db.ClassSessions.Add(session);
            ids.Add(session.Id);
        }

        await db.SaveChangesAsync();
        return ids;
    }

    public static async Task GiveUnlimitedToAllAsync(IServiceProvider services, Guid tenantId, IEnumerable<Guid> userIds)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var plan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Unlimited",
            Type = PlanType.Unlimited,
            DurationDays = 30,
            Price = 50m,
            IsActive = true,
        };
        db.MembershipPlans.Add(plan);
        foreach (var userId in userIds)
        {
            db.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                MembershipPlanId = plan.Id,
                RemainingSessions = null,
                ValidFrom = now.AddDays(-1),
                ValidTo = now.AddDays(30),
                Status = SubscriptionStatus.Active,
            });
        }

        await db.SaveChangesAsync();
    }

    public static async Task<T[]> RunConcurrentlyAsync<T>(int count, Func<int, Task<T>> work)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = new Task<T>[count];
        for (var i = 0; i < count; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                await gate.Task;
                return await work(index);
            });
        }

        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    public static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        if (sorted.Count == 0)
        {
            return double.NaN;
        }

        var rank = (int)Math.Ceiling(p / 100.0 * sorted.Count);
        rank = Math.Clamp(rank, 1, sorted.Count);
        return sorted[rank - 1];
    }

    public static double StdDev(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var mean = values.Average();
        var sum = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sum / (values.Count - 1));
    }

    public static string F(double value)
    {
        return value.ToString("0.0", CultureInfo.InvariantCulture);
    }

    public static string ResultsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docker-compose.full.yml")))
        {
            dir = dir.Parent;
        }

        var root = dir?.FullName ?? AppContext.BaseDirectory;
        var results = Path.Combine(root, "evaluation", "results");
        Directory.CreateDirectory(results);
        return results;
    }

    public static void WriteCsv(string fileName, string header, IEnumerable<string> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);
        foreach (var row in rows)
        {
            sb.AppendLine(row);
        }

        File.WriteAllText(Path.Combine(ResultsDirectory(), fileName), sb.ToString(), new UTF8Encoding(false));
    }

    public static void WriteEnvironment()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var lines = new[]
        {
            "date_utc=" + DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture),
            "os=" + RuntimeInformation.OSDescription,
            "cpu=" + (Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown"),
            "logical_cores=" + Environment.ProcessorCount,
            "memory_available_gb=" + (gcInfo.TotalAvailableMemoryBytes / 1024.0 / 1024 / 1024).ToString("0.0", CultureInfo.InvariantCulture),
            "runtime=" + RuntimeInformation.FrameworkDescription,
            "database=postgres:16-alpine (Testcontainers, Docker Desktop), max_connections=300",
            "api=in-process (WebApplicationFactory / TestServer), environment=Testing",
        };
        File.WriteAllLines(Path.Combine(ResultsDirectory(), "environment.txt"), lines);
    }
}
