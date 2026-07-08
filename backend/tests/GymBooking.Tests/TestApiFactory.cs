using GymBooking.Api.Services;
using GymBooking.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymBooking.Tests;

public class TestApiFactory : WebApplicationFactory<Program>
{
    // Μοναδικό όνομα ανά instance — χωρίς αυτό, δύο test classes που τρέχουν παράλληλα θα
    // μοιράζονταν κατά λάθος την ίδια InMemory βάση (το EF InMemory provider την κλειδώνει by name).
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));

            // Αντικαθιστά το πραγματικό SmtpEmailSender — "last registration wins" στο DI.
            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<FakeEmailSender>());
        });
    }
}
