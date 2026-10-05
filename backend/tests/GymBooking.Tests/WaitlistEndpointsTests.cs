using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class WaitlistEndpointsTests
{
    private readonly PostgresApiFactory _factory;

    public WaitlistEndpointsTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string password, params string[] roles)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        foreach (var role in roles)
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    private async Task<Guid> SeedFullSessionAsync(Guid tenantId, Guid fillerId)
    {
        await TestData.GiveUnlimitedAsync(_factory.Services, tenantId, fillerId);
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ct = new ClassType { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Yoga", DefaultDurationMinutes = 60, DefaultCapacity = 1, IsActive = true };
        var s = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = Guid.NewGuid(), StartsAt = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Capacity = 1, BookedCount = 0, IsActive = true };
        db.ClassTypes.Add(ct);
        db.ClassSessions.Add(s);
        await db.SaveChangesAsync();
        await scope.ServiceProvider.GetRequiredService<GymBooking.Api.Services.BookingService>().BookAsync(fillerId, s.Id);
        return s.Id;
    }

    [Fact]
    public async Task Member_joins_waitlist_of_full_session_then_sees_it_in_my_waitlist()
    {
        var user = await CreateUserAsync("waiter@demo.gym", "Test1234!", Roles.User);
        var sessionId = await SeedFullSessionAsync(user.TenantId, Guid.NewGuid());
        await TestData.GiveUnlimitedAsync(_factory.Services, user.TenantId, user.Id);

        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "waiter@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var join = await client.PostAsync($"/sessions/{sessionId}/waitlist", null);
        Assert.Equal(HttpStatusCode.NoContent, join.StatusCode);

        var mine = await client.GetFromJsonAsync<List<WaitlistEntryResponse>>("/waitlist/me");
        Assert.Contains(mine!, w => w.ClassSessionId == sessionId && w.Position == 1);
    }

    [Fact]
    public async Task Leave_waitlist_removes_entry()
    {
        var user = await CreateUserAsync("leaver@demo.gym", "Test1234!", Roles.User);
        var sessionId = await SeedFullSessionAsync(user.TenantId, Guid.NewGuid());
        await TestData.GiveUnlimitedAsync(_factory.Services, user.TenantId, user.Id);

        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "leaver@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await client.PostAsync($"/sessions/{sessionId}/waitlist", null);

        var leave = await client.DeleteAsync($"/sessions/{sessionId}/waitlist");
        Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);

        var mine = await client.GetFromJsonAsync<List<WaitlistEntryResponse>>("/waitlist/me");
        Assert.DoesNotContain(mine!, w => w.ClassSessionId == sessionId);
    }
}
