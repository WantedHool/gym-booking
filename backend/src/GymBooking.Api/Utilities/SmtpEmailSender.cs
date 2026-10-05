using System.Net.Mail;
using GymBooking.Api.Interfaces;
using GymBooking.Core.Options;

namespace GymBooking.Api.Utilities;

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
