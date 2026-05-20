using System.ComponentModel.DataAnnotations;

namespace KinexusMockup.Models;

/// <summary>
/// For sending individual emails. Requires UserID to know which user to send the email to.
/// </summary>
public class AdminEmailViewModel
{
    [Required]
    public string UserId { get; set; } = "";

    [Required]
    [EmailAddress]
    public string RecipientEmail { get; set; } = "";

    [Required]
    [StringLength(120)]
    public string Subject { get; set; } = "";

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = "";
}