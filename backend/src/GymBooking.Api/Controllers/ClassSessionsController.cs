using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("class-sessions")]
[Authorize]
public class ClassSessionsController : ControllerBase
{
    private readonly ClassSessionService _service;

    public ClassSessionsController(ClassSessionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassSessionResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClassSessionResponse>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireInstructor)]
    public async Task<ActionResult<ClassSessionResponse>> Create([FromBody] CreateClassSessionRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirst("sub")!.Value);
        var isAdmin = User.IsInRole(Roles.Admin);

        var instructorId = isAdmin && request.InstructorId.HasValue
            ? request.InstructorId.Value
            : currentUserId;

        var result = await _service.CreateAsync(request.ClassTypeId, instructorId, request.StartsAt, request.DurationMinutes, request.Capacity);
        if (result.Error is not null)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Response!.Id }, result.Response);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.RequireInstructor)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var ok = await _service.CancelAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
