using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace KinexusMockup.Models;

/// <summary>
/// For sending bulk emails, Does not require UserID.
/// </summary>
public class AdminBulkEmailViewModel
{
    [Required]
    [StringLength(120)]
    public string Subject { get; set; } = "";

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = "";

    public IFormFile? ImageAttachment { get; set; }
}