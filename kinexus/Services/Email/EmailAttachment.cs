namespace KinexusMockup.Services.Email;

public class EmailAttachment
{
    public EmailAttachment(string fileName, string contentType, byte[] content)
    {
        FileName = fileName;
        ContentType = contentType;
        Content = content;
    }

    public string FileName { get; }
    public string ContentType { get; }
    public byte[] Content { get; }
}