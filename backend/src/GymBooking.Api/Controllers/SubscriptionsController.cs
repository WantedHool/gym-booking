using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionService _service;

    public SubscriptionsController(SubscriptionService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<ActionResult<SubscriptionResponse?>> GetMine()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        return Ok(await _service.GetActiveForUserAsync(userId));
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Assign([FromBody] AssignSubscriptionRequest request)
    {
        var (ok, error) = await _service.AssignAsync(request.Email, request.PlanId);
        return ok ? NoContent() : BadRequest(error);
    }
}
