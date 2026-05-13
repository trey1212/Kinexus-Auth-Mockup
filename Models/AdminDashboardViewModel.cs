namespace KinexusMockup.Models;

public class AdminDashboardViewModel
{
    public IReadOnlyList<AdminMockUser> Users { get; init; } = [];

    public int TotalUsers { get; init; }

    public string CurrentSort { get; init; } = "recent";

    public string CurrentSearch { get; init; } = string.Empty;

    public int VisibleUsers => Users.Count;
}
