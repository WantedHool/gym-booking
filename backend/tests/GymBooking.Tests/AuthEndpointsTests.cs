using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Api.Auth;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class AuthEndpointsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AuthEndpointsTests(TestApiFactory factory)
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

    [Fact]
    public async Task Login_with_correct_credentials_returns_200_and_token()
    {
        await CreateUserAsync("login-ok@demo.gym", "Test1234!");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest("login-ok@demo.gym", "Test1234!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.AccessToken));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        await CreateUserAsync("login-badpass@demo.gym", "Test1234!");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest("login-badpass@demo.gym", "WrongPassword1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest("no-such-user@demo.gym", "Test1234!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_valid_token_returns_200()
    {
        await CreateUserAsync("auth-me@demo.gym", "Test1234!");
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "auth-me@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
