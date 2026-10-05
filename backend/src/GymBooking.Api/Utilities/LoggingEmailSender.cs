using GymBooking.Api.Interfaces;

namespace GymBooking.Api.Utilities;

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Email (not sent) → {To} · {Subject} · {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
