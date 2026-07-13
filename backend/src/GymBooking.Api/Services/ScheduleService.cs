using GymBooking.Core.Contracts;
using GymBooking.Core.Entities.Enums;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Services;

public class ScheduleService
{
    private readonly AppDbContext _dbContext;

    public ScheduleService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ScheduleSessionResponse>> GetScheduleAsync(
        Guid userId, DateTime fromUtc, DateTime toUtc, Guid? classTypeId, Guid? instructorId)
    {
        var query =
            from s in _dbContext.ClassSessions
            where s.IsActive && !s.IsCancelled && s.StartsAt >= fromUtc && s.StartsAt < toUtc
            join ct in _dbContext.ClassTypes on s.ClassTypeId equals ct.Id
            join u in _dbContext.Users on s.InstructorId equals u.Id
            select new { s, ClassTypeName = ct.Name, InstructorName = u.FirstName + " " + u.LastName };

        if (classTypeId.HasValue)
        {
            query = query.Where(x => x.s.ClassTypeId == classTypeId.Value);
        }
        if (instructorId.HasValue)
        {
            query = query.Where(x => x.s.InstructorId == instructorId.Value);
        }

        var rows = await query.OrderBy(x => x.s.StartsAt).ToListAsync();

        var mine = (await _dbContext.Bookings
                .Where(b => b.UserId == userId && b.Status == BookingStatus.Confirmed)
                .Select(b => new { b.Id, b.ClassSessionId })
                .ToListAsync())
            .ToDictionary(b => b.ClassSessionId, b => b.Id);

        return rows.Select(x => new ScheduleSessionResponse(
            x.s.Id,
            x.s.ClassTypeId,
            x.ClassTypeName,
            x.s.InstructorId,
            x.InstructorName,
            x.s.StartsAt,
            x.s.DurationMinutes,
            x.s.Capacity,
            x.s.BookedCount,
            mine.ContainsKey(x.s.Id),
            mine.TryGetValue(x.s.Id, out var bid) ? bid : (Guid?)null))
            .ToList();
    }
}
