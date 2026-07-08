using System.IdentityModel.Tokens.Jwt;
using GymBooking.Api.Auth;
using GymBooking.Core.Entities.Models;

namespace GymBooking.Tests;

public class TokenServiceTests
{
    private static TokenService CreateService() => new(new JwtOptions
    {
        Issuer = "GymBooking.Tests",
        Audience = "GymBooking.Tests",
        SigningKey = "unit-test-signing-key-1f8a6c2e9b4d7f01a3c5e8b2d4f60719",
        AccessTokenExpiryMinutes = 120,
    });

    [Fact]
    public void CreateAccessToken_includes_sub_tenantId_and_role_claims()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };
        var service = CreateService();

        var token = service.CreateAccessToken(user, new[] { "Admin" }, user.TenantId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal(user.TenantId.ToString(), jwt.Claims.Single(c => c.Type == "tenantId").Value);
        Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == "role").Value);
    }

    [Fact]
    public void CreateAccessToken_expires_in_about_two_hours()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };
        var service = CreateService();

        var token = service.CreateAccessToken(user, Array.Empty<string>(), user.TenantId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expectedExpiry = DateTime.UtcNow.AddMinutes(120);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalMinutes) < 1);
    }
}
