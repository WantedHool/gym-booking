using Microsoft.EntityFrameworkCore;

namespace GymBooking.Data;

/// <summary>
/// Η κεντρική "πύλη" προς τη βάση. Τα DbSets (πίνακες) και τα entity
/// configurations προστίθενται στη Φάση 1 (base entities + multi-tenancy).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
