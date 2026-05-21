namespace KinexusMockup.Models;

/// <summary>
/// Admin dashboard view model. Contains a list of users and some metadata 
/// about the current search/sort state of the dashboard.
/// </summary>
public class AdminDashboardViewModel
{
    public IReadOnlyList<AdminMockUser> Users { get; init; } = [];

    public int TotalUsers { get; init; }

    public string CurrentSort { get; init; } = "recent";

    public string CurrentSearch { get; init; } = string.Empty;

    public int VisibleUsers => Users.Count;
}
