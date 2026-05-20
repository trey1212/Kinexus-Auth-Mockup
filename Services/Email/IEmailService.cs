namespace KinexusMockup.Services.Email;

/// <summary>
/// Email Service Interface for sending emails. Implemented by SmtpEmailService, 
/// but can be mocked for testing purposes.
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string message);
}