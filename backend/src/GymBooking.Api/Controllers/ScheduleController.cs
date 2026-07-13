using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("schedule")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly ScheduleService _service;

    public ScheduleController(ScheduleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleSessionResponse>>> Get(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? classTypeId,
        [FromQuery] Guid? instructorId)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var result = await _service.GetScheduleAsync(userId, from.UtcDateTime, to.UtcDateTime, classTypeId, instructorId);
        return Ok(result);
    }
}
