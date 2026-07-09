using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class ClassTypeService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public ClassTypeService(AppDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<ClassType> CreateAsync(string name, string description, int defaultDurationMinutes, int defaultCapacity)
    {
        var classType = new ClassType
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            Name = name,
            Description = description,
            DefaultDurationMinutes = defaultDurationMinutes,
            DefaultCapacity = defaultCapacity,
            IsActive = true,
        };
        _dbContext.ClassTypes.Add(classType);
        await _dbContext.SaveChangesAsync();
        return classType;
    }

    public async Task<List<ClassType>> GetAllAsync()
    {
        return await _dbContext.ClassTypes.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<ClassType?> GetByIdAsync(Guid id)
    {
        return await _dbContext.ClassTypes.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> UpdateAsync(Guid id, string name, string description, int defaultDurationMinutes, int defaultCapacity)
    {
        var classType = await _dbContext.ClassTypes.FirstOrDefaultAsync(c => c.Id == id);
        if (classType is null)
        {
            return false;
        }

        classType.Name = name;
        classType.Description = description;
        classType.DefaultDurationMinutes = defaultDurationMinutes;
        classType.DefaultCapacity = defaultCapacity;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var classType = await _dbContext.ClassTypes.FirstOrDefaultAsync(c => c.Id == id);
        if (classType is null)
        {
            return false;
        }

        classType.IsActive = false;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
