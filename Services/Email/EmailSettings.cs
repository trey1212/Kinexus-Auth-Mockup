namespace KinexusMockup.Services.Email;

public class EmailSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "Kinexus Admin";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}