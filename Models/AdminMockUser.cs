namespace KinexusMockup.Models;

public record AdminMockUser(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly JoinedOn);
