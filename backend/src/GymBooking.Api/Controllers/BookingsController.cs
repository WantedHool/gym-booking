using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
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
        return outcome == BookingOutcome.SessionNotFound ? NotFound() : NoContent();
    }
}
