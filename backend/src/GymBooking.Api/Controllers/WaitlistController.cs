using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Authorize]
public class WaitlistController : ControllerBase
{
    private readonly WaitlistService _service;

    public WaitlistController(WaitlistService service)
    {
        _service = service;
    }

    [HttpPost("sessions/{sessionId:guid}/waitlist")]
    public async Task<IActionResult> Join(Guid sessionId)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var outcome = await _service.JoinAsync(userId, sessionId);
        return outcome switch
        {
            WaitlistOutcome.Joined => NoContent(),
            WaitlistOutcome.SessionNotFound => NotFound(),
            WaitlistOutcome.SessionNotFull => Conflict("Υπάρχει διαθέσιμη θέση — κάνε κανονική κράτηση."),
            WaitlistOutcome.AlreadyBooked => Conflict("Έχεις ήδη κράτηση σε αυτό το session."),
            WaitlistOutcome.AlreadyOnWaitlist => Conflict("Είσαι ήδη στη λίστα αναμονής."),
            _ => BadRequest(),
        };
    }

    [HttpDelete("sessions/{sessionId:guid}/waitlist")]
    public async Task<IActionResult> Leave(Guid sessionId)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var outcome = await _service.LeaveAsync(userId, sessionId);
        return outcome == WaitlistOutcome.NotOnWaitlist ? NotFound() : NoContent();
    }

    [HttpGet("waitlist/me")]
    public async Task<ActionResult<IEnumerable<WaitlistEntryResponse>>> GetMine()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        return Ok(await _service.GetMineAsync(userId));
    }
}
