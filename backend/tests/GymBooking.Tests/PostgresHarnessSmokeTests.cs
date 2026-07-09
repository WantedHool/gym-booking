using System.Net.Http.Json;
using GymBooking.Core.Contracts;

namespace GymBooking.Tests;

[Collection("Postgres")]
public class PostgresHarnessSmokeTests
{
    private readonly PostgresApiFactory _factory;

    public PostgresHarnessSmokeTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seeded_admin_can_login_against_real_postgres()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest("admin@demo.gym", "Admin123!"));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
    }
}
