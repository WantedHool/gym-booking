using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Tests.Evaluation;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class TenantIsolationHttpTests
{
    private readonly PostgresApiFactory _factory;

    public TenantIsolationHttpTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid TenantId, Guid SessionId)> SeedTenantWithSessionAsync()
    {
        var tenantId = Guid.NewGuid();
        var instructorId = await EvaluationSupport.SeedInstructorAsync(_factory.Services, tenantId);
        var sessions = await EvaluationSupport.SeedSessionsAsync(_factory.Services, tenantId, instructorId, 1, 10, DateTime.UtcNow.AddDays(3));
        return (tenantId, sessions[0]);
    }

    private HttpClient ClientFor(Guid tenantId, Guid userId)
    {
        var tokenService = _factory.Services.GetRequiredService<TokenService>();
        var token = tokenService.CreateAccessToken(new ApplicationUser { Id = userId }, new[] { Roles.User }, tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Member_cannot_read_session_of_another_tenant_by_id()
    {
        var (tenantA, _) = await SeedTenantWithSessionAsync();
        var (_, sessionB) = await SeedTenantWithSessionAsync();

        var response = await ClientFor(tenantA, Guid.NewGuid()).GetAsync("/class-sessions/" + sessionB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_book_session_of_another_tenant()
    {
        var (tenantA, _) = await SeedTenantWithSessionAsync();
        var (tenantB, sessionB) = await SeedTenantWithSessionAsync();
        var userA = Guid.NewGuid();
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantA, userA);

        var response = await ClientFor(tenantA, userA).PostAsJsonAsync("/bookings", new CreateBookingRequest(sessionB));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var sessionAsSeenByB = await ClientFor(tenantB, Guid.NewGuid()).GetFromJsonAsync<ClassSessionResponse>("/class-sessions/" + sessionB);
        Assert.Equal(0, sessionAsSeenByB!.BookedCount);
    }

    [Fact]
    public async Task Schedule_contains_only_own_tenant_sessions()
    {
        var (tenantA, sessionA) = await SeedTenantWithSessionAsync();
        var (_, sessionB) = await SeedTenantWithSessionAsync();
        var from = Uri.EscapeDataString(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        var to = Uri.EscapeDataString(DateTime.UtcNow.AddDays(7).ToString("o", CultureInfo.InvariantCulture));

        var schedule = await ClientFor(tenantA, Guid.NewGuid())
            .GetFromJsonAsync<List<ScheduleSessionResponse>>("/schedule?from=" + from + "&to=" + to);

        Assert.Contains(schedule!, s => s.Id == sessionA);
        Assert.DoesNotContain(schedule!, s => s.Id == sessionB);
    }
}
