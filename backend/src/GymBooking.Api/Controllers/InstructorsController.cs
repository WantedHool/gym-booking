using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymBooking.Api.Controllers;

[ApiController]
[Route("instructors")]
[Authorize]
public class InstructorsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public InstructorsController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InstructorResponse>>> GetAll()
    {
        var instructors = await _userManager.GetUsersInRoleAsync(Roles.Instructor);
        return Ok(instructors
            .OrderBy(u => u.LastName)
            .Select(u => new InstructorResponse(u.Id, u.FirstName + " " + u.LastName)));
    }
}
