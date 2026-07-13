using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class SubscriptionsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public SubscriptionsTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    private async Task CreateUserInTenantAsync(Guid tenantId, string email, string password, params string[] roles)
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
            TenantId = tenantId,
            UserName = email,
            Email = email,
            FirstName = "T",
            LastName = "U",
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
    }

    private async Task<Guid> SeedPlanAsync(Guid tenantId, PlanType type, int sessions, int durationDays)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var plan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Plan",
            Type = type,
            SessionsCount = sessions,
            DurationDays = durationDays,
            Price = 10m,
            IsActive = true,
        };
        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan.Id;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task Admin_assigns_subscription_and_member_sees_it()
    {
        var tenantId = Guid.NewGuid();
        await CreateUserInTenantAsync(tenantId, "sub-admin@demo.gym", "Test1234!", Roles.Admin);
        await CreateUserInTenantAsync(tenantId, "sub-member@demo.gym", "Test1234!", Roles.User);
        var planId = await SeedPlanAsync(tenantId, PlanType.SessionPack, sessions: 10, durationDays: 60);

        var adminClient = _factory.CreateClient();
        var adminToken = await LoginAsync(adminClient, "sub-admin@demo.gym", "Test1234!");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var assign = await adminClient.PostAsJsonAsync("/subscriptions", new AssignSubscriptionRequest("sub-member@demo.gym", planId));
        assign.EnsureSuccessStatusCode();

        var memberClient = _factory.CreateClient();
        var memberToken = await LoginAsync(memberClient, "sub-member@demo.gym", "Test1234!");
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var mine = await memberClient.GetFromJsonAsync<SubscriptionResponse>("/subscriptions/me");

        Assert.NotNull(mine);
        Assert.Equal(10, mine!.RemainingSessions);
        Assert.Equal("SessionPack", mine.Type);
    }
}
