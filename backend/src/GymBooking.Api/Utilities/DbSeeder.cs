using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Options;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Utilities;

public class DbSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedOptions _options;

    public DbSeeder(
        AppDbContext dbContext,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        SeedOptions options)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options;
    }

    public async Task SeedAsync()
    {
        if (await _dbContext.Tenants.AnyAsync())
        {
            return;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = _options.TenantName,
            Slug = _options.TenantSlug,
            CancellationHours = _options.CancellationHours,
            IsActive = true,
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        foreach (var role in new[] { Roles.User, Roles.Instructor, Roles.Admin })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserName = _options.AdminEmail,
            Email = _options.AdminEmail,
            FirstName = "Admin",
            LastName = tenant.Name,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(admin, _options.AdminPassword);
        if (!result.Succeeded)
        {
            _dbContext.Tenants.Remove(tenant);
            await _dbContext.SaveChangesAsync();
            throw new InvalidOperationException(
                "Seeding admin user failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(admin);
            _dbContext.Tenants.Remove(tenant);
            await _dbContext.SaveChangesAsync();
            throw new InvalidOperationException(
                "Seeding admin role assignment failed: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }

    public async Task SeedAdditionalTenantAsync(AdditionalTenantSeed seed)
    {
        if (string.IsNullOrWhiteSpace(seed.TenantSlug))
        {
            return;
        }

        var exists = await _dbContext.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Slug == seed.TenantSlug);
        if (exists)
        {
            return;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = seed.TenantName,
            Slug = seed.TenantSlug,
            CancellationHours = seed.CancellationHours,
            IsActive = true,
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        foreach (var role in new[] { Roles.User, Roles.Instructor, Roles.Admin })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserName = seed.AdminEmail,
            Email = seed.AdminEmail,
            FirstName = "Admin",
            LastName = tenant.Name,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(admin, seed.AdminPassword);
        if (!result.Succeeded)
        {
            _dbContext.Tenants.Remove(tenant);
            await _dbContext.SaveChangesAsync();
            throw new InvalidOperationException(
                "Seeding additional tenant admin failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(admin);
            _dbContext.Tenants.Remove(tenant);
            await _dbContext.SaveChangesAsync();
            throw new InvalidOperationException(
                "Seeding additional tenant admin role failed: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}
