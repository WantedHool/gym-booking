using System.Net.Mail;
using GymBooking.Core.Options;

namespace GymBooking.Api.Services;

// Dev-only: στέλνει μέσω SMTP στο τοπικό Papercut (docker/docker-compose.yml) — τα emails
// φαίνονται στο http://localhost:8080, δεν φεύγουν πραγματικά πουθενά.
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(EmailOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort);
        using var message = new MailMessage(_options.FromAddress, to, subject, body);
        await client.SendMailAsync(message);
    }
}
