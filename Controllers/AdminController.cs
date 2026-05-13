using Microsoft.AspNetCore.Mvc;
using KinexusMockup.Models;

namespace KinexusMockup.Controllers;

public class AdminController : Controller
{
    private static readonly IReadOnlyList<AdminMockUser> MockUsers =
    [
        new(1, "Ava", "Mitchell", "ava.mitchell@kinexusmock.com", new DateOnly(2026, 5, 10)),
        new(2, "Noah", "Bennett", "noah.bennett@kinexusmock.com", new DateOnly(2026, 5, 9)),
        new(3, "Mia", "Reynolds", "mia.reynolds@kinexusmock.com", new DateOnly(2026, 5, 8)),
        new(4, "Liam", "Foster", "liam.foster@kinexusmock.com", new DateOnly(2026, 5, 7)),
        new(5, "Sophia", "Parker", "sophia.parker@kinexusmock.com", new DateOnly(2026, 5, 6)),
        new(6, "Ethan", "Hayes", "ethan.hayes@kinexusmock.com", new DateOnly(2026, 5, 4)),
        new(7, "Isabella", "Cole", "isabella.cole@kinexusmock.com", new DateOnly(2026, 5, 2)),
        new(8, "Lucas", "Ward", "lucas.ward@kinexusmock.com", new DateOnly(2026, 4, 30)),
        new(9, "Charlotte", "Brooks", "charlotte.brooks@kinexusmock.com", new DateOnly(2026, 4, 28)),
        new(10, "James", "Diaz", "james.diaz@kinexusmock.com", new DateOnly(2026, 4, 26))
    ];

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Dashboard(string? search, string? sort)
    {
        IEnumerable<AdminMockUser> users = MockUsers;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            users = users.Where(user =>
                user.FirstName.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase) ||
                user.LastName.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase) ||
                user.Email.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase));
        }

        var selectedSort = string.IsNullOrWhiteSpace(sort) ? "recent" : sort.Trim().ToLowerInvariant();

        users = selectedSort switch
        {
            "alpha-asc" => users.OrderBy(user => user.FirstName).ThenBy(user => user.LastName),
            "alpha-desc" => users.OrderByDescending(user => user.FirstName).ThenByDescending(user => user.LastName),
            "oldest" => users.OrderBy(user => user.JoinedOn),
            _ => users.OrderByDescending(user => user.JoinedOn)
        };

        var viewModel = new AdminDashboardViewModel
        {
            Users = users.ToList(),
            TotalUsers = MockUsers.Count,
            CurrentSearch = search?.Trim() ?? string.Empty,
            CurrentSort = selectedSort
        };

        return View(viewModel);
    }
}
