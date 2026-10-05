using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class InvitationsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public InvitationsTests(TestApiFactory factory)
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

    private async Task EnsureRoleExistsAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = role });
        }
    }

    private async Task SeedInvitationAsync(Invitation invitation)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Invitations.Add(invitation);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateInvitation_as_non_admin_returns_403()
    {
        await CreateUserAsync("invite-non-admin@demo.gym", "Test1234!");
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "invite-non-admin@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/invitations", new CreateInvitationRequest("invitee1@demo.gym", Roles.User));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvitation_with_invalid_role_returns_400()
    {
        var admin = await CreateUserAsync("invite-admin-badrole@demo.gym", "Test1234!", Roles.Admin);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "invite-admin-badrole@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/invitations", new CreateInvitationRequest("invitee2@demo.gym", "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvitation_as_admin_returns_register_link_with_token()
    {
        await CreateUserAsync("invite-admin-ok@demo.gym", "Test1234!", Roles.Admin);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "invite-admin-ok@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/invitations", new CreateInvitationRequest("invitee3@demo.gym", Roles.Instructor));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InvitationResponse>();
        Assert.NotNull(body);
        Assert.Contains("token=", body!.RegisterLink);

        var emailSender = _factory.Services.GetRequiredService<FakeEmailSender>();
        Assert.Contains(emailSender.SentEmails, e => e.To == "invitee3@demo.gym" && e.Body.Contains("token="));
    }

    [Fact]
    public async Task Register_with_valid_token_creates_active_user_with_correct_role_and_tenant()
    {
        var admin = await CreateUserAsync("invite-admin-reg@demo.gym", "Test1234!", Roles.Admin);
        await EnsureRoleExistsAsync(Roles.User);
        var client = _factory.CreateClient();
        var adminToken = await LoginAsync(client, "invite-admin-reg@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        await client.PostAsJsonAsync("/invitations", new CreateInvitationRequest("new-member@demo.gym", Roles.User));

        var emailSender = _factory.Services.GetRequiredService<FakeEmailSender>();
        var email = emailSender.SentEmails.Last(e => e.To == "new-member@demo.gym");
        var rawToken = Regex.Match(email.Body, "token=([^\\s]+)").Groups[1].Value;

        var anonymousClient = _factory.CreateClient();
        var registerResponse = await anonymousClient.PostAsJsonAsync(
            $"/auth/register?token={rawToken}",
            new RegisterRequest("Member1234!", "New", "Member"));

        Assert.Equal(HttpStatusCode.NoContent, registerResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var createdUser = await userManager.Users.IgnoreQueryFilters()
            .SingleAsync(u => u.NormalizedEmail == "NEW-MEMBER@DEMO.GYM");

        Assert.Equal(admin.TenantId, createdUser.TenantId);
        Assert.Equal(UserStatus.Active, createdUser.Status);
        Assert.Contains(Roles.User, await userManager.GetRolesAsync(createdUser));

        var secondAttempt = await anonymousClient.PostAsJsonAsync(
            $"/auth/register?token={rawToken}",
            new RegisterRequest("Whatever1234!", "X", "Y"));
        Assert.Equal(HttpStatusCode.BadRequest, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task Register_with_expired_token_returns_400()
    {
        var rawToken = InvitationService.GenerateRawToken();
        await SeedInvitationAsync(new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "expired@demo.gym",
            Role = Roles.User,
            TokenHash = InvitationService.HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        });

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/auth/register?token={rawToken}",
            new RegisterRequest("Test1234!", "A", "B"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_already_used_token_returns_400()
    {
        var rawToken = InvitationService.GenerateRawToken();
        await SeedInvitationAsync(new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "used@demo.gym",
            Role = Roles.User,
            TokenHash = InvitationService.HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UsedAt = DateTime.UtcNow.AddMinutes(-5),
        });

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/auth/register?token={rawToken}",
            new RegisterRequest("Test1234!", "A", "B"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_unknown_token_returns_400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/auth/register?token={InvitationService.GenerateRawToken()}",
            new RegisterRequest("Test1234!", "A", "B"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
