using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

/// <summary>
/// Αποτέλεσμα δημιουργίας session: είτε το έτοιμο response, είτε μήνυμα σφάλματος (validation).
/// </summary>
public record SessionCreateResult(ClassSessionResponse? Response, string? Error)
{
    public static SessionCreateResult Ok(ClassSessionResponse response)
    {
        return new SessionCreateResult(response, null);
    }

    public static SessionCreateResult Invalid(string error)
    {
        return new SessionCreateResult(null, error);
    }
}

public class ClassSessionService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClassSessionService(
        AppDbContext dbContext,
        ICurrentTenant currentTenant,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _userManager = userManager;
    }

    /// <summary>
    /// Δημιουργεί session αφού επικυρώσει: θετική χωρητικότητα/διάρκεια, μελλοντική έναρξη,
    /// ενεργό ClassType (ίδιο tenant), και instructor που υπάρχει στο tenant με ρόλο Instructor ή Admin.
    /// </summary>
    public async Task<SessionCreateResult> CreateAsync(Guid classTypeId, Guid instructorId, DateTime startsAtUtc, int durationMinutes, int capacity)
    {
        if (capacity <= 0)
        {
            return SessionCreateResult.Invalid("Capacity must be greater than zero.");
        }

        if (durationMinutes <= 0)
        {
            return SessionCreateResult.Invalid("Duration must be greater than zero.");
        }

        if (startsAtUtc <= DateTime.UtcNow)
        {
            return SessionCreateResult.Invalid("StartsAt must be in the future.");
        }

        // Το query filter περιορίζει το ClassType στο τρέχον tenant.
        var classType = await _dbContext.ClassTypes.FirstOrDefaultAsync(c => c.Id == classTypeId && c.IsActive);
        if (classType is null)
        {
            return SessionCreateResult.Invalid("Invalid or inactive class type.");
        }

        // FindByIdAsync περνά από το ίδιο DbContext → το tenant query filter ισχύει
        // (instructor άλλου tenant → null).
        var instructor = await _userManager.FindByIdAsync(instructorId.ToString());
        if (instructor is null)
        {
            return SessionCreateResult.Invalid("Instructor not found in this tenant.");
        }

        var isInstructor = await _userManager.IsInRoleAsync(instructor, Roles.Instructor);
        var isAdmin = await _userManager.IsInRoleAsync(instructor, Roles.Admin);
        if (!isInstructor && !isAdmin)
        {
            return SessionCreateResult.Invalid("Assigned user is not an instructor.");
        }

        var session = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenant.TenantId,
            ClassTypeId = classTypeId,
            InstructorId = instructorId,
            StartsAt = startsAtUtc,
            DurationMinutes = durationMinutes,
            Capacity = capacity,
            BookedCount = 0,
            IsCancelled = false,
            IsActive = true,
        };
        _dbContext.ClassSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        // Χτίζουμε το response από δεδομένα που ήδη έχουμε στο χέρι — χωρίς δεύτερο round-trip.
        var response = new ClassSessionResponse(
            session.Id,
            session.ClassTypeId,
            classType.Name,
            session.InstructorId,
            instructor.FirstName + " " + instructor.LastName,
            session.StartsAt,
            session.DurationMinutes,
            session.Capacity,
            session.BookedCount,
            session.IsCancelled);
        return SessionCreateResult.Ok(response);
    }

    public async Task<List<ClassSessionResponse>> GetAllAsync()
    {
        return await (
            from s in _dbContext.ClassSessions
            where s.IsActive
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            join u in _dbContext.Users on s.InstructorId equals u.Id
            orderby s.StartsAt
            select new ClassSessionResponse(
                s.Id,
                s.ClassTypeId,
                ct.Name,
                s.InstructorId,
                u.FirstName + " " + u.LastName,
                s.StartsAt,
                s.DurationMinutes,
                s.Capacity,
                s.BookedCount,
                s.IsCancelled))
            .ToListAsync();
    }

    public async Task<ClassSessionResponse?> GetByIdAsync(Guid id)
    {
        return await (
            from s in _dbContext.ClassSessions
            where s.Id == id
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            join u in _dbContext.Users on s.InstructorId equals u.Id
            select new ClassSessionResponse(
                s.Id,
                s.ClassTypeId,
                ct.Name,
                s.InstructorId,
                u.FirstName + " " + u.LastName,
                s.StartsAt,
                s.DurationMinutes,
                s.Capacity,
                s.BookedCount,
                s.IsCancelled))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var session = await _dbContext.ClassSessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session is null)
        {
            return false;
        }

        session.IsCancelled = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
