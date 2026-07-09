using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("class-types")]
[Authorize(Policy = Policies.RequireInstructor)]
public class ClassTypesController : ControllerBase
{
    private readonly ClassTypeService _service;

    public ClassTypesController(ClassTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassTypeResponse>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClassTypeResponse>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ClassTypeResponse>> Create([FromBody] CreateClassTypeRequest request)
    {
        if (Validate(request.Name, request.DefaultDurationMinutes, request.DefaultCapacity) is { } error)
        {
            return BadRequest(error);
        }

        var created = await _service.CreateAsync(request.Name, request.Description, request.DefaultDurationMinutes, request.DefaultCapacity);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassTypeRequest request)
    {
        if (Validate(request.Name, request.DefaultDurationMinutes, request.DefaultCapacity) is { } error)
        {
            return BadRequest(error);
        }

        var updated = await _service.UpdateAsync(id, request.Name, request.Description, request.DefaultDurationMinutes, request.DefaultCapacity);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeactivateAsync(id);
        return ok ? NoContent() : NotFound();
    }

    private static string? Validate(string name, int defaultDurationMinutes, int defaultCapacity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (defaultDurationMinutes <= 0)
        {
            return "DefaultDurationMinutes must be greater than zero.";
        }

        if (defaultCapacity <= 0)
        {
            return "DefaultCapacity must be greater than zero.";
        }

        return null;
    }

    private static ClassTypeResponse ToResponse(ClassType c)
    {
        return new ClassTypeResponse(c.Id, c.Name, c.Description, c.DefaultDurationMinutes, c.DefaultCapacity, c.IsActive);
    }
}
