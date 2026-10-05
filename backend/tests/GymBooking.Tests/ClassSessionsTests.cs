using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class ClassSessionsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ClassSessionsTests(TestApiFactory factory)
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
    public async Task CreateSession_as_instructor_assigns_self_as_instructor()
    {
        var instructor = await CreateUserAsync("sess-instr@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "sess-instr@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ctResponse = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("Spin", "Ποδήλατο", 50, 15));
        var classType = await ctResponse.Content.ReadFromJsonAsync<ClassTypeResponse>();

        var sessionResponse = await client.PostAsJsonAsync("/class-sessions",
            new CreateClassSessionRequest(classType!.Id, null, DateTime.UtcNow.AddDays(2), 50, 15));

        Assert.Equal(HttpStatusCode.Created, sessionResponse.StatusCode);
        var session = await sessionResponse.Content.ReadFromJsonAsync<ClassSessionResponse>();
        Assert.Equal(instructor.Id, session!.InstructorId);
        Assert.Equal("Spin", session.ClassTypeName);
        Assert.Equal(0, session.BookedCount);
    }

    [Fact]
    public async Task CreateSession_with_unknown_class_type_returns_400()
    {
        await CreateUserAsync("sess-instr2@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "sess-instr2@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/class-sessions",
            new CreateClassSessionRequest(Guid.NewGuid(), null, DateTime.UtcNow.AddDays(2), 60, 10));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_with_past_start_returns_400()
    {
        await CreateUserAsync("sess-past@demo.gym", "Test1234!", Roles.Instructor);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "sess-past@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ctResponse = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("Zumba", "χορός", 45, 20));
        var classType = await ctResponse.Content.ReadFromJsonAsync<ClassTypeResponse>();

        var response = await client.PostAsJsonAsync("/class-sessions",
            new CreateClassSessionRequest(classType!.Id, null, DateTime.UtcNow.AddDays(-1), 45, 20));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_as_admin_with_unknown_instructor_returns_400()
    {
        await CreateUserAsync("sess-admin@demo.gym", "Test1234!", Roles.Admin);
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "sess-admin@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ctResponse = await client.PostAsJsonAsync("/class-types",
            new CreateClassTypeRequest("Boxing", "πυγμαχία", 60, 12));
        var classType = await ctResponse.Content.ReadFromJsonAsync<ClassTypeResponse>();

        var response = await client.PostAsJsonAsync("/class-sessions",
            new CreateClassSessionRequest(classType!.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), 60, 12));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
