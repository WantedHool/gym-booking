using GymBooking.Core.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Invitations;

[ApiController]
[Route("invitations")]
public class InvitationsController : ControllerBase
{
    private readonly InvitationService _invitationService;

    public InvitationsController(InvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateInvitationRequest request)
    {
        if (request.Role != Roles.User && request.Role != Roles.Instructor)
        {
            return BadRequest($"Role must be '{Roles.User}' or '{Roles.Instructor}'.");
        }

        await _invitationService.CreateAsync(request.Email, request.Role);

        return NoContent();
    }
}

public record CreateInvitationRequest(string Email, string Role);
