using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly BookingService _service;

    public BookingsController(BookingService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetMine()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        return Ok(await _service.GetMineAsync(userId));
    }

    [HttpPost]
    public async Task<IActionResult> Book([FromBody] CreateBookingRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var (outcome, _) = await _service.BookAsync(userId, request.ClassSessionId);
        return outcome switch
        {
            BookingOutcome.Success => CreatedAtAction(nameof(GetMine), null, null),
            BookingOutcome.SessionNotFound => NotFound(),
            BookingOutcome.SessionCancelled => Conflict("Το session έχει ακυρωθεί."),
            BookingOutcome.SessionFull => Conflict("Το session είναι πλήρες."),
            BookingOutcome.AlreadyBooked => Conflict("Έχεις ήδη κράτηση σε αυτό το session."),
            BookingOutcome.TimeConflict => Conflict("Έχεις άλλη κράτηση που επικαλύπτεται χρονικά."),
            BookingOutcome.NoSubscription => Conflict("Δεν έχεις ενεργή συνδρομή."),
            _ => BadRequest(),
        };
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var (outcome, _) = await _service.CancelAsync(userId, id);
        return outcome switch
        {
            BookingOutcome.SessionNotFound => NotFound(),
            BookingOutcome.CancellationTooLate => Conflict("Δεν μπορείς να ακυρώσεις τόσο κοντά στην ώρα του μαθήματος."),
            _ => NoContent(),
        };
    }

    [HttpGet("/sessions/{sessionId:guid}/roster")]
    [Authorize(Policy = Policies.RequireInstructor)]
    public async Task<ActionResult<RosterResponse>> GetRoster(Guid sessionId)
    {
        var roster = await _service.GetRosterAsync(sessionId);
        return roster is null ? NotFound() : Ok(roster);
    }

    [HttpPost("/sessions/{sessionId:guid}/bookings")]
    [Authorize(Policy = Policies.RequireInstructor)]
    public async Task<IActionResult> StaffBook(Guid sessionId, [FromBody] StaffBookRequest request)
    {
        var (outcome, _) = await _service.BookForAsync(request.UserId, sessionId);
        return outcome switch
        {
            BookingOutcome.Success => NoContent(),
            BookingOutcome.SessionNotFound => NotFound(),
            BookingOutcome.SessionCancelled => Conflict("Το session έχει ακυρωθεί."),
            BookingOutcome.SessionFull => Conflict("Το session είναι πλήρες."),
            BookingOutcome.AlreadyBooked => Conflict("Ο πελάτης έχει ήδη κράτηση."),
            BookingOutcome.TimeConflict => Conflict("Ο πελάτης έχει επικαλυπτόμενη κράτηση."),
            BookingOutcome.NoSubscription => Conflict("Ο πελάτης δεν έχει ενεργή συνδρομή."),
            _ => BadRequest(),
        };
    }

    [HttpDelete("/bookings/{id:guid}/staff")]
    [Authorize(Policy = Policies.RequireInstructor)]
    public async Task<IActionResult> StaffCancel(Guid id)
    {
        var (outcome, _) = await _service.CancelByStaffAsync(id);
        return outcome switch
        {
            BookingOutcome.SessionNotFound => NotFound(),
            _ => NoContent(),
        };
    }
}
