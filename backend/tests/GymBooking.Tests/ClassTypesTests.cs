using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class ClassTypesTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ClassTypesTests(TestApiFactory factory)
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
    public async Task CreateClassType_as_plain_user_returns_403()
    {
        await CreateUserAsync("ct-user@demo.gym", "Test1234!", Roles.User);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "ct-user@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("Yoga", "Χαλαρωτικό", 60, 12));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateClassType_with_blank_name_returns_400()
    {
        await CreateUserAsync("ct-blank@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "ct-blank@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("   ", "χωρίς όνομα", 60, 12));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateClassType_with_nonpositive_capacity_returns_400()
    {
        await CreateUserAsync("ct-badcap@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "ct-badcap@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("Yoga", "", 60, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateClassType_as_instructor_then_appears_in_list()
    {
        await CreateUserAsync("ct-instr@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "ct-instr@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("Pilates", "Core", 45, 10));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var list = await client.GetFromJsonAsync<List<ClassTypeResponse>>("/class-types");
        Assert.Contains(list!, c => c.Name == "Pilates" && c.DefaultCapacity == 10);
    }
}
