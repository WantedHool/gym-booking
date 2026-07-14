using GymBooking.Api.Services;
using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("tenant/settings")]
[Authorize(Policy = Policies.RequireAdmin)]
public class TenantController : ControllerBase
{
    private readonly TenantSettingsService _service;

    public TenantController(TenantSettingsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<TenantSettingsResponse>> Get()
    {
        var settings = await _service.GetAsync();
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTenantSettingsRequest request)
    {
        var ok = await _service.UpdateAsync(request);
        return ok ? NoContent() : BadRequest("Άκυρες ρυθμίσεις.");
    }
}
