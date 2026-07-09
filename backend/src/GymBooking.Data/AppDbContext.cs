using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Data;

/// <summary>
/// Η κεντρική "πύλη" προς τη βάση. Identity (users/roles) + tenant-owned entities.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ICurrentTenant _currentTenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<ClassType> ClassTypes => Set<ClassType>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u => u.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<Invitation>().HasQueryFilter(i => i.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<ClassType>().HasQueryFilter(c => c.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<ClassSession>().HasQueryFilter(s => s.TenantId == _currentTenant.TenantId);
        modelBuilder.Entity<Booking>().HasQueryFilter(b => b.TenantId == _currentTenant.TenantId);

        // DB-level δικλείδα ασφαλείας ενάντια σε διπλή confirmed κράτηση (πέρα από τον έλεγχο στο service).
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.UserId, b.ClassSessionId })
            .IsUnique()
            .HasFilter("\"Status\" = 0");
    }
}
