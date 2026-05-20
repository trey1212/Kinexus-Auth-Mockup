using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KinexusMockup.Services.Email;

/// <summary>
/// SMTP Email Service implementation of the IEmailService interface. 
/// This service uses the MailKit library to send emails via SMTP.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    /// <summary>
    /// SMTP Email Service constructor that takes in the email settings via dependency injection.
    /// </summary>
    /// <param name="settings">The email settings.</param>
    public SmtpEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>
    /// Sends an email message asynchronously to the specified recipient with the given subject and message body.
    /// </summary>
    /// <remarks>
    /// The method uses SMTP to send the email. If authentication settings are provided, the method
    /// will attempt to authenticate with the SMTP server before sending the message.
    /// </remarks>
    /// <param name="toEmail">The email address of the recipient. Cannot be null, 
    /// empty, or consist only of white-space characters.</param>
    /// <param name="subject">The subject line of the email. Cannot be null, 
    /// empty, or consist only of white-space characters.</param>
    /// <param name="message">The body content of the email message. Cannot be null, 
    /// empty, or consist only of white-space characters.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="toEmail"/>, <paramref name="subject"/>, or <paramref name="message"/> is null, empty,
    /// or consists only of white-space characters.</exception>
    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        //toEmail = toEmail.Trim();

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
        email.To.Add(MailboxAddress.Parse(toEmail.Trim()));
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