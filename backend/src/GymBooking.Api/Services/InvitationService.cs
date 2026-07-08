using System.Security.Cryptography;
using System.Text;
using GymBooking.Api.Interfaces;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Core.Options;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class InvitationService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly ICurrentTenant _currentTenant;
    private readonly InvitationOptions _options;

    public InvitationService(AppDbContext dbContext, IEmailSender emailSender, ICurrentTenant currentTenant, InvitationOptions options)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _currentTenant = currentTenant;
        _options = options;
    }

    public static string GenerateRawToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public async Task CreateAsync(string email, string role)
    {
        var rawToken = GenerateRawToken();
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            Email = email,
            Role = role,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        _dbContext.Invitations.Add(invitation);
        await _dbContext.SaveChangesAsync();

        var registerLink = $"{_options.RegisterUrlBase}?token={rawToken}";
        await _emailSender.SendAsync(email, "Πρόσκληση εγγραφής", $"Κάνε εγγραφή εδώ: {registerLink}");
    }

    public async Task<Invitation?> ValidateTokenAsync(string rawToken)
    {
        var tokenHash = HashToken(rawToken);

        // Δεν ξέρουμε ακόμα το tenant του invitation — ίδιο σκεπτικό με το login (AuthController).
        var invitation = await _dbContext.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);

        if (invitation is null || invitation.UsedAt is not null || invitation.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        return invitation;
    }

    public async Task MarkUsedAsync(Invitation invitation)
    {
        invitation.UsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }
}
