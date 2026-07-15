using GymBooking.Api.Interfaces;

namespace GymBooking.Api.Utilities;

// Production-safe: δεν στέλνει πραγματικό email — καταγράφει το μήνυμα στο log. Χρησιμοποιείται
// στο deployed demo (δεν υπάρχει SMTP· ο admin παίρνει το invite link μέσα από το staff UI).
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
