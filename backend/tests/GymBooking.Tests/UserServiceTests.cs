using GymBooking.Api.Services;
using GymBooking.Core.Entities.Constants;
using GymBooking.Core.Entities.Models;
using GymBooking.Core.Multitenancy;
using GymBooking.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBooking.Tests;

public class UserServiceTests
{
    private static AppDbContext Ctx(string db, Guid tenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(db).Options;
        return new AppDbContext(options, new FakeTenant(tenant));
    }

    private sealed class FakeTenant : ICurrentTenant
    {
        public FakeTenant(Guid id) { TenantId = id; }
        public Guid TenantId { get; }
    }

    private sealed class FakeRoleReader : IRoleReaderWriter
    {
        private readonly Dictionary<Guid, List<string>> _roles;
        public FakeRoleReader(Dictionary<Guid, List<string>> roles) { _roles = roles; }
        public Task<IList<string>> GetRolesAsync(ApplicationUser u) =>
            Task.FromResult<IList<string>>(_roles.TryGetValue(u.Id, out var r) ? r : new List<string>());
        public Task SetSingleRoleAsync(ApplicationUser u, string role)
        {
            _roles[u.Id] = new List<string> { role };
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ChangeRole_blocks_admin_from_demoting_self()
    {
        var tenant = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();

        using (var seed = Ctx(db, tenant))
        {
            seed.Users.Add(new ApplicationUser
            {
                Id = adminId, TenantId = tenant, UserName = "admin@demo.gym",
                Email = "admin@demo.gym", FirstName = "Ad", LastName = "Min",
            });
            seed.SaveChanges();
        }

        using var ctx = Ctx(db, tenant);
        var roles = new FakeRoleReader(new() { [adminId] = new() { Roles.Admin } });
        var service = new UserService(ctx, new FakeTenant(tenant), roles);

        var outcome = await service.ChangeRoleAsync(actingUserId: adminId, targetUserId: adminId, Roles.User);

        Assert.Equal(UserOutcome.CannotModifySelf, outcome);
    }

    [Fact]
    public async Task SetActive_blocks_self_deactivation()
    {
        var tenant = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.Users.Add(new ApplicationUser { Id = adminId, TenantId = tenant, UserName = "a", Email = "a", FirstName = "A", LastName = "A" });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new UserService(ctx, new FakeTenant(tenant), new FakeRoleReader(new()));
        var outcome = await service.SetActiveAsync(adminId, adminId, false);
        Assert.Equal(UserOutcome.CannotModifySelf, outcome);
    }

    [Fact]
    public async Task ChangeRole_rejects_unknown_role()
    {
        var tenant = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using var ctx = Ctx(db, tenant);
        var service = new UserService(ctx, new FakeTenant(tenant), new FakeRoleReader(new()));
        var outcome = await service.ChangeRoleAsync(Guid.NewGuid(), Guid.NewGuid(), "Wizard");
        Assert.Equal(UserOutcome.InvalidRole, outcome);
    }

    [Fact]
    public async Task SetActive_deactivates_other_user()
    {
        var tenant = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var db = Guid.NewGuid().ToString();
        using (var seed = Ctx(db, tenant))
        {
            seed.Users.Add(new ApplicationUser { Id = targetId, TenantId = tenant, UserName = "t", Email = "t", FirstName = "T", LastName = "T", IsActive = true });
            seed.SaveChanges();
        }
        using var ctx = Ctx(db, tenant);
        var service = new UserService(ctx, new FakeTenant(tenant), new FakeRoleReader(new()));
        var outcome = await service.SetActiveAsync(adminId, targetId, false);
        Assert.Equal(UserOutcome.Success, outcome);
        Assert.False((await ctx.Users.FirstAsync(u => u.Id == targetId)).IsActive);
    }
}
