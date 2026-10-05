using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GymBooking.Tests.Evaluation;

[Collection("Evaluation")]
public class LatencyExperiment
{
    private const int Repetitions = 5;
    private const int MaxUsers = 50;
    private static readonly int[] Levels = { 1, 10, 25, 50 };

    private readonly EvaluationApiFactory _factory;
    private readonly ITestOutputHelper _output;

    public LatencyExperiment(EvaluationApiFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [EvaluationFact]
    public async Task Api_response_times_under_concurrency()
    {
        EvaluationSupport.WriteEnvironment();

        var tenantId = Guid.NewGuid();
        var instructorId = await EvaluationSupport.SeedInstructorAsync(_factory.Services, tenantId);
        var users = Enumerable.Range(0, MaxUsers).Select(_ => Guid.NewGuid()).ToList();
        await EvaluationSupport.GiveUnlimitedToAllAsync(_factory.Services, tenantId, users);
        var tokens = CreateTokens(users, tenantId);

        var weekStart = DateTime.UtcNow.Date.AddDays(1);
        for (var day = 0; day < 5; day++)
        {
            await EvaluationSupport.SeedSessionsAsync(_factory.Services, tenantId, instructorId, 4, 20, weekStart.AddDays(day).AddHours(8 + day));
        }

        var client = _factory.CreateClient();
        var samples = new List<(string Scenario, int Level, int Rep, double Ms, bool Ok)>();
        var slot = 0;

        await BookSameSessionAsync(client, tokens, tenantId, instructorId, 10, NextSlot(ref slot));
        await ScheduleAsync(client, tokens, 10, weekStart);

        foreach (var level in Levels)
        {
            for (var rep = 1; rep <= Repetitions; rep++)
            {
                foreach (var (ms, ok) in await BookSameSessionAsync(client, tokens, tenantId, instructorId, level, NextSlot(ref slot)))
                {
                    samples.Add(("booking_same_session", level, rep, ms, ok));
                }

                foreach (var (ms, ok) in await BookDifferentSessionsAsync(client, tokens, tenantId, instructorId, level, NextSlot(ref slot)))
                {
                    samples.Add(("booking_different_sessions", level, rep, ms, ok));
                }

                foreach (var (ms, ok) in await ScheduleAsync(client, tokens, level, weekStart))
                {
                    samples.Add(("schedule", level, rep, ms, ok));
                }
            }
        }

        EvaluationSupport.WriteCsv("ee2_latency_raw.csv", "scenario,concurrent_users,rep,latency_ms,ok",
            samples.Select(s => string.Join(",", s.Scenario, s.Level, s.Rep, EvaluationSupport.F(s.Ms), s.Ok ? 1 : 0)));

        var summary = new List<string>();
        foreach (var group in samples.GroupBy(s => (s.Scenario, s.Level)).OrderBy(g => g.Key.Scenario).ThenBy(g => g.Key.Level))
        {
            var sorted = group.Select(s => s.Ms).OrderBy(x => x).ToList();
            var p95PerRep = group.GroupBy(s => s.Rep)
                .Select(r => EvaluationSupport.Percentile(r.Select(x => x.Ms).OrderBy(x => x).ToList(), 95))
                .ToList();
            var line = string.Join(",", group.Key.Scenario, group.Key.Level, sorted.Count, group.Count(s => !s.Ok),
                EvaluationSupport.F(sorted.Average()),
                EvaluationSupport.F(EvaluationSupport.Percentile(sorted, 50)),
                EvaluationSupport.F(EvaluationSupport.Percentile(sorted, 95)),
                EvaluationSupport.F(EvaluationSupport.StdDev(p95PerRep)),
                EvaluationSupport.F(sorted[^1]));
            summary.Add(line);
            _output.WriteLine(line);
        }

        EvaluationSupport.WriteCsv("ee2_latency_summary.csv",
            "scenario,concurrent_users,samples,failed,mean_ms,p50_ms,p95_ms,p95_stddev_across_reps_ms,max_ms", summary);

        Assert.DoesNotContain(samples, s => !s.Ok);
    }

    private static DateTime NextSlot(ref int slot)
    {
        slot++;
        return DateTime.UtcNow.Date.AddDays(10).AddHours(slot * 2);
    }

    private List<string> CreateTokens(List<Guid> users, Guid tenantId)
    {
        var tokenService = _factory.Services.GetRequiredService<TokenService>();
        var roles = new[] { Roles.User };
        return users.Select(u => tokenService.CreateAccessToken(new ApplicationUser { Id = u }, roles, tenantId)).ToList();
    }

    private async Task<(double, bool)[]> BookSameSessionAsync(
        HttpClient client, List<string> tokens, Guid tenantId, Guid instructorId, int level, DateTime start)
    {
        var sessionId = (await EvaluationSupport.SeedSessionsAsync(_factory.Services, tenantId, instructorId, 1, level, start))[0];
        return await EvaluationSupport.RunConcurrentlyAsync(level, i => TimedAsync(() =>
            Send(client, tokens[i], HttpMethod.Post, "/bookings", new CreateBookingRequest(sessionId)), HttpStatusCode.Created));
    }

    private async Task<(double, bool)[]> BookDifferentSessionsAsync(
        HttpClient client, List<string> tokens, Guid tenantId, Guid instructorId, int level, DateTime start)
    {
        var sessionIds = await EvaluationSupport.SeedSessionsAsync(_factory.Services, tenantId, instructorId, level, 5, start);
        return await EvaluationSupport.RunConcurrentlyAsync(level, i => TimedAsync(() =>
            Send(client, tokens[i], HttpMethod.Post, "/bookings", new CreateBookingRequest(sessionIds[i])), HttpStatusCode.Created));
    }

    private static async Task<(double, bool)[]> ScheduleAsync(HttpClient client, List<string> tokens, int level, DateTime weekStart)
    {
        var from = weekStart.ToString("o", CultureInfo.InvariantCulture);
        var to = weekStart.AddDays(7).ToString("o", CultureInfo.InvariantCulture);
        var url = "/schedule?from=" + Uri.EscapeDataString(from) + "&to=" + Uri.EscapeDataString(to);
        return await EvaluationSupport.RunConcurrentlyAsync(level, i => TimedAsync(() =>
            Send(client, tokens[i], HttpMethod.Get, url, null), HttpStatusCode.OK));
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string token, HttpMethod method, string url, object? body)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request);
    }

    private static async Task<(double, bool)> TimedAsync(Func<Task<HttpResponseMessage>> call, HttpStatusCode expected)
    {
        var sw = Stopwatch.StartNew();
        using var response = await call();
        await response.Content.ReadAsByteArrayAsync();
        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, response.StatusCode == expected);
    }
}
