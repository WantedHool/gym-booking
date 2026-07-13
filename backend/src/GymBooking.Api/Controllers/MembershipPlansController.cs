using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("membership-plans")]
[Authorize(Policy = Policies.RequireAdmin)]
public class MembershipPlansController : ControllerBase
{
    private readonly MembershipPlanService _service;

    public MembershipPlansController(MembershipPlanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanResponse>>> GetAll()
    {
        var plans = await _service.GetAllAsync();
        return Ok(plans.Select(ToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<PlanResponse>> Create([FromBody] CreatePlanRequest request)
    {
        var created = await _service.CreateAsync(request.Name, request.Type, request.SessionsCount, request.DurationDays, request.Price);
        if (created is null)
        {
            return BadRequest($"Type must be '{nameof(PlanType.SessionPack)}' or '{nameof(PlanType.Unlimited)}'.");
        }

        return CreatedAtAction(nameof(GetAll), null, ToResponse(created));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeactivateAsync(id);
        return ok ? NoContent() : NotFound();
    }

    private static PlanResponse ToResponse(MembershipPlan p)
    {
        return new PlanResponse(p.Id, p.Name, p.Type.ToString(), p.SessionsCount, p.DurationDays, p.Price, p.IsActive);
    }
}
