using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public enum UserOutcome
{
    Success,
    NotFound,
    CannotModifySelf,
    InvalidRole,
}

// Λεπτό abstraction πάνω από το UserManager ώστε το UserService να είναι unit-testable χωρίς Identity stack.
public interface IRoleReaderWriter
{
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task SetSingleRoleAsync(ApplicationUser user, string role);
}

public class IdentityRoleReaderWriter : IRoleReaderWriter
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityRoleReaderWriter(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    public async Task SetSingleRoleAsync(ApplicationUser user, string role)
    {
        var current = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, current);
        await _userManager.AddToRoleAsync(user, role);
    }
}

public class UserService
{
    private static readonly string[] AllowedRoles = { Roles.User, Roles.Instructor, Roles.Admin };

    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly IRoleReaderWriter _roles;

    public UserService(AppDbContext dbContext, ICurrentTenant currentTenant, IRoleReaderWriter roles)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _roles = roles;
    }

    public async Task<List<UserListItem>> GetAllAsync()
    {
        var users = await _dbContext.Users.OrderBy(u => u.Email).ToListAsync();
        var result = new List<UserListItem>();
        foreach (var u in users)
        {
            var roles = await _roles.GetRolesAsync(u);
            result.Add(new UserListItem(
                u.Id, u.Email ?? string.Empty, u.FirstName, u.LastName,
                roles.ToList(), u.IsActive, u.CreatedAt));
        }
        return result;
    }

    public async Task<UserOutcome> ChangeRoleAsync(Guid actingUserId, Guid targetUserId, string role)
    {
        if (!AllowedRoles.Contains(role))
        {
            return UserOutcome.InvalidRole;
        }
        if (actingUserId == targetUserId)
        {
            return UserOutcome.CannotModifySelf; // no self-demote (μη μείνει tenant χωρίς admin)
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
        if (user is null)
        {
            return UserOutcome.NotFound;
        }

        await _roles.SetSingleRoleAsync(user, role);
        return UserOutcome.Success;
    }

    public async Task<UserOutcome> SetActiveAsync(Guid actingUserId, Guid targetUserId, bool isActive)
    {
        if (actingUserId == targetUserId)
        {
            return UserOutcome.CannotModifySelf; // no self-deactivate
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
        if (user is null)
        {
            return UserOutcome.NotFound;
        }

        user.IsActive = isActive;
        await _dbContext.SaveChangesAsync();
        return UserOutcome.Success;
    }
}
