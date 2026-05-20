using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KinexusMockup.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Recipient email is required.");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Email subject is required.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Email message is required.");
        }

        var email = new MimeMessage();

        email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        email.Body = new TextPart("plain")
        {
            Text = message
        };

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            _settings.Host,
            _settings.Port,
            SecureSocketOptions.StartTlsWhenAvailable
        );

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
        }

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}