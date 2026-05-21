
using Microsoft.AspNetCore.Identity;

namespace KinexusMockup.Models;

public class AdminMockUser : IdentityUser
{
    
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public DateOnly JoinedOn { get; set; }
}