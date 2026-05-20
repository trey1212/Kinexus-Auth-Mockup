using System.ComponentModel.DataAnnotations;

namespace KinexusMockup.Models;

public class AdminBulkEmailViewModel
{
    [Required]
    [StringLength(120)]
    public string Subject { get; set; } = "";

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = "";
}