using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Enums;
using GymBooking.Core.Entities.Models;
using GymBooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Api.Utilities;

public class DemoDataSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public DemoDataSeeder(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        var tenant = await _dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync();
        if (tenant is null)
        {
            return;
        }

        if (await _dbContext.ClassTypes.IgnoreQueryFilters().AnyAsync())
        {
            return;
        }

        var yoga = new ClassType
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Yoga",
            Description = "Χαλαρωτικό μάθημα yoga για όλα τα επίπεδα.",
            DefaultDurationMinutes = 60,
            DefaultCapacity = 8,
            IsActive = true,
        };
        var crossfit = new ClassType
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "CrossFit",
            Description = "Έντονη προπόνηση λειτουργικής δύναμης.",
            DefaultDurationMinutes = 45,
            DefaultCapacity = 8,
            IsActive = true,
        };
        _dbContext.ClassTypes.AddRange(yoga, crossfit);

        var instructor1 = await CreateUserAsync("instructor@demo.gym", "Γιώργος", "Παπαδόπουλος", tenant.Id, Roles.Instructor);
        var instructor2 = await CreateUserAsync("instructor2@demo.gym", "Ελένη", "Νικολάου", tenant.Id, Roles.Instructor);

        var sessionYoga1 = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ClassTypeId = yoga.Id,
            InstructorId = instructor1.Id,
            StartsAt = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            DurationMinutes = 60,
            Capacity = 8,
            BookedCount = 0,
            IsActive = true,
        };
        var sessionCrossfit1 = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ClassTypeId = crossfit.Id,
            InstructorId = instructor2.Id,
            StartsAt = DateTime.UtcNow.AddDays(2).Date.AddHours(18),
            DurationMinutes = 45,
            Capacity = 1,
            BookedCount = 0,
            IsActive = true,
        };
        var sessionYoga2 = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ClassTypeId = yoga.Id,
            InstructorId = instructor1.Id,
            StartsAt = DateTime.UtcNow.AddDays(3).Date.AddHours(9),
            DurationMinutes = 60,
            Capacity = 8,
            BookedCount = 0,
            IsActive = true,
        };
        var sessionCrossfit2 = new ClassSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ClassTypeId = crossfit.Id,
            InstructorId = instructor2.Id,
            StartsAt = DateTime.UtcNow.AddDays(5).Date.AddHours(18),
            DurationMinutes = 45,
            Capacity = 8,
            BookedCount = 0,
            IsActive = true,
        };
        _dbContext.ClassSessions.AddRange(sessionYoga1, sessionCrossfit1, sessionYoga2, sessionCrossfit2);

        var unlimitedPlan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Απεριόριστο Μηνιαίο",
            Type = PlanType.Unlimited,
            SessionsCount = 0,
            DurationDays = 30,
            Price = 80m,
            IsActive = true,
        };
        var sessionPackPlan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Πακέτο 10 Συνεδριών",
            Type = PlanType.SessionPack,
            SessionsCount = 10,
            DurationDays = 60,
            Price = 50m,
            IsActive = true,
        };
        _dbContext.MembershipPlans.AddRange(unlimitedPlan, sessionPackPlan);

        var member1 = await CreateUserAsync("member@demo.gym", "Μαρία", "Ιωάννου", tenant.Id, Roles.User);
        var member2 = await CreateUserAsync("member2@demo.gym", "Νίκος", "Δημητρίου", tenant.Id, Roles.User);
        var member3 = await CreateUserAsync("member3@demo.gym", "Άννα", "Κωνσταντίνου", tenant.Id, Roles.User);

        var now = DateTime.UtcNow;
        var subscriptionMember1 = new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = member1.Id,
            MembershipPlanId = unlimitedPlan.Id,
            RemainingSessions = null,
            ValidFrom = now,
            ValidTo = now.AddDays(unlimitedPlan.DurationDays),
            Status = SubscriptionStatus.Active,
        };
        var subscriptionMember2 = new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = member2.Id,
            MembershipPlanId = sessionPackPlan.Id,
            RemainingSessions = sessionPackPlan.SessionsCount,
            ValidFrom = now,
            ValidTo = now.AddDays(sessionPackPlan.DurationDays),
            Status = SubscriptionStatus.Active,
        };
        var subscriptionMember3 = new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = member3.Id,
            MembershipPlanId = sessionPackPlan.Id,
            RemainingSessions = sessionPackPlan.SessionsCount,
            ValidFrom = now,
            ValidTo = now.AddDays(sessionPackPlan.DurationDays),
            Status = SubscriptionStatus.Active,
        };
        _dbContext.Subscriptions.AddRange(subscriptionMember1, subscriptionMember2, subscriptionMember3);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = member1.Id,
            ClassSessionId = sessionCrossfit1.Id,
            SubscriptionId = subscriptionMember1.Id,
            Status = BookingStatus.Confirmed,
            CreatedAt = now,
        };
        _dbContext.Bookings.Add(booking);
        sessionCrossfit1.BookedCount = 1;

        var waitlistEntry = new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = member2.Id,
            ClassSessionId = sessionCrossfit1.Id,
            Status = WaitlistStatus.Waiting,
            CreatedAt = now,
        };
        _dbContext.WaitlistEntries.Add(waitlistEntry);

        await _dbContext.SaveChangesAsync();
    }

    private const string DemoPassword = "Demo1234!";

    private async Task<ApplicationUser> CreateUserAsync(string email, string firstName, string lastName, Guid tenantId, string role)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Δημιουργία demo χρήστη '{email}' απέτυχε: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Ανάθεση ρόλου '{role}' στον demo χρήστη '{email}' απέτυχε: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        return user;
    }
}
