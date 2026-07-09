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
public class BookingEndpointsTests
{
    private readonly PostgresApiFactory _factory;

    public BookingEndpointsTests(PostgresApiFactory factory)
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

    private async Task<Guid> SeedSessionForTenantAsync(Guid tenantId, int capacity)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(tenantId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ct = new ClassType { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Yoga", DefaultDurationMinutes = 60, DefaultCapacity = capacity, IsActive = true };
        var s = new ClassSession { Id = Guid.NewGuid(), TenantId = tenantId, ClassTypeId = ct.Id, InstructorId = Guid.NewGuid(), StartsAt = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Capacity = capacity, IsActive = true };
        db.ClassTypes.Add(ct);
        db.ClassSessions.Add(s);
        await db.SaveChangesAsync();
        return s.Id;
    }

    [Fact]
    public async Task Member_books_session_then_it_appears_in_my_bookings()
    {
        var user = await CreateUserAsync("booker@demo.gym", "Test1234!", Roles.User);
        var sessionId = await SeedSessionForTenantAsync(user.TenantId, capacity: 5);

        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "booker@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookResponse = await client.PostAsJsonAsync("/bookings", new CreateBookingRequest(sessionId));
        Assert.Equal(HttpStatusCode.Created, bookResponse.StatusCode);

        var mine = await client.GetFromJsonAsync<List<BookingResponse>>("/bookings/me");
        Assert.Contains(mine!, b => b.ClassSessionId == sessionId && b.Status == "Confirmed");
    }

    [Fact]
    public async Task Booking_full_session_returns_409()
    {
        var user = await CreateUserAsync("booker-full@demo.gym", "Test1234!", Roles.User);
        var sessionId = await SeedSessionForTenantAsync(user.TenantId, capacity: 1);
        var other = await CreateUserAsync("booker-first@demo.gym", "Test1234!", Roles.User);

        // Ο "other" πρέπει να είναι στο ΙΔΙΟ tenant για να δει το session — απλούστερο: γέμισε το session
        // απευθείας μέσω του service σε scope του tenant.
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<CurrentTenant>().SetTenant(user.TenantId);
            var svc = scope.ServiceProvider.GetRequiredService<GymBooking.Api.Services.BookingService>();
            await svc.BookAsync(Guid.NewGuid(), sessionId);
        }

        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "booker-full@demo.gym", "Test1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/bookings", new CreateBookingRequest(sessionId));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
