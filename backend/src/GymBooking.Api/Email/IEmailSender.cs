namespace GymBooking.Api.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
