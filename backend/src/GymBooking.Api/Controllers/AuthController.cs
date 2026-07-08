using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly CurrentTenant _currentTenant;
    private readonly InvitationService _invitationService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService,
        CurrentTenant currentTenant,
        InvitationService invitationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _currentTenant = currentTenant;
        _invitationService = invitationService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Δεν ξέρουμε ακόμα το tenant του χρήστη — το CurrentTenant είναι Guid.Empty σε αυτό το
        // σημείο, άρα το query filter του AppDbContext θα έκρυβε ΚΑΘΕ χρήστη. Το IgnoreQueryFilters
        // εδώ είναι σκόπιμο: το email είναι global unique (shared UserNameIndex), άρα το lookup
        // είναι ασφαλές χωρίς tenant scope.
        var normalizedEmail = _userManager.NormalizeEmail(request.Email);
        var user = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (user is null)
        {
            return Unauthorized();
        }

        // Μόλις βρεθεί ο χρήστης, ξέρουμε το tenant του — από εδώ και πέρα στο ίδιο request
        // (π.χ. UserManager.GetRolesAsync) το query filter δουλεύει σωστά.
        _currentTenant.SetTenant(user.TenantId);

        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!passwordCheck.Succeeded)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.CreateAccessToken(user, roles, user.TenantId);

        return Ok(new LoginResponse(token));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            UserId = User.FindFirst("sub")?.Value,
            TenantId = User.FindFirst("tenantId")?.Value,
            Roles = User.FindAll("role").Select(c => c.Value),
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromQuery] string token, [FromBody] RegisterRequest request)
    {
        var invitation = await _invitationService.ValidateTokenAsync(token);
        if (invitation is null)
        {
            return BadRequest("Invalid, expired, or already used invitation token.");
        }

        // Γνωστό tenant πλέον (από το invitation) — ίδιο σκεπτικό με το login: πρέπει να μπει
        // πριν το CreateAsync, αλλιώς ο εσωτερικός uniqueness-check του Identity θα έψαχνε με
        // CurrentTenant = Guid.Empty.
        _currentTenant.SetTenant(invitation.TenantId);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            UserName = invitation.Email,
            Email = invitation.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        // Αν αποτύχει ο role assignment (π.χ. ο ρόλος δεν υπάρχει), διαγράφουμε τον μόλις
        // δημιουργημένο χρήστη — αλλιώς το invitation θα έμενε unused αλλά το email θα ήταν ήδη
        // "πιασμένο", και ένα retry του invitee θα κολλούσε μόνιμα σε duplicate-username 400.
        IdentityResult roleResult;
        try
        {
            roleResult = await _userManager.AddToRoleAsync(user, invitation.Role);
        }
        catch (InvalidOperationException)
        {
            roleResult = IdentityResult.Failed(new IdentityError
            {
                Description = $"Role '{invitation.Role}' does not exist.",
            });
        }

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(roleResult.Errors.Select(e => e.Description));
        }

        await _invitationService.MarkUsedAsync(invitation);

        return NoContent();
    }
}
