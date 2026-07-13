using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class InstructorsTests
{
    private readonly PostgresApiFactory _factory;

    public InstructorsTests(PostgresApiFactory factory)
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
            FirstName = "First",
            LastName = email.Split('@')[0],
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

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task Instructors_list_contains_instructors_not_plain_members()
    {
        var tenantId = Guid.NewGuid();
        await CreateUserInTenantAsync(tenantId, "the-instr@demo.gym", "Test1234!", Roles.Instructor);
        await CreateUserInTenantAsync(tenantId, "the-member@demo.gym", "Test1234!", Roles.User);

        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "the-member@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var list = await client.GetFromJsonAsync<List<InstructorResponse>>("/instructors");

        Assert.Contains(list!, i => i.Name.Contains("the-instr"));
        Assert.DoesNotContain(list!, i => i.Name.Contains("the-member"));
    }
}
