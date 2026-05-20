namespace KinexusMockup.Services.Email;

/// <summary>
/// Email Settings class for configuring the SmtpEmailService. This class is used 
/// to bind the email settings from the appsettings.json file.
/// </summary>
public class EmailSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "Kinexus Admin";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}