using GymBooking.Api.Utilities;
using Microsoft.Extensions.Logging.Abstractions;

namespace GymBooking.Tests;

public class LoggingEmailSenderTests
{
    [Fact]
    public async Task SendAsync_does_not_throw()
    {
        var sender = new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance);

        await sender.SendAsync("to@demo.gym", "Subject", "Body with token=abc123");
    }
}
