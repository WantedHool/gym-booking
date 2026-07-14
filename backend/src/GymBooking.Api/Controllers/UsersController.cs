using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize(Policy = Policies.RequireAdmin)]
public class UsersController : ControllerBase
{
    private readonly UserService _service;

    public UsersController(UserService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserListItem>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        var actingUserId = Guid.Parse(User.FindFirst("sub")!.Value);
        var outcome = await _service.ChangeRoleAsync(actingUserId, id, request.Role);
        return Map(outcome);
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var actingUserId = Guid.Parse(User.FindFirst("sub")!.Value);
        return Map(await _service.SetActiveAsync(actingUserId, id, false));
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var actingUserId = Guid.Parse(User.FindFirst("sub")!.Value);
        return Map(await _service.SetActiveAsync(actingUserId, id, true));
    }

    private IActionResult Map(UserOutcome outcome)
    {
        return outcome switch
        {
            UserOutcome.Success => NoContent(),
            UserOutcome.NotFound => NotFound(),
            UserOutcome.CannotModifySelf => Conflict("Δεν μπορείς να τροποποιήσεις τον δικό σου λογαριασμό."),
            UserOutcome.InvalidRole => BadRequest("Άκυρος ρόλος."),
            _ => BadRequest(),
        };
    }
}
