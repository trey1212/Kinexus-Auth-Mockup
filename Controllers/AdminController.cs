using KinexusMockup.Data;
using KinexusMockup.Models;
using Microsoft.AspNetCore.Mvc;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Dashboard()
    {
        var users = _context.Users.ToList();

        var model = new AdminDashboardViewModel
        {
            Users = users,
            TotalUsers = users.Count
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult UpdateUser(AdminMockUser user)
    {
        var dbUser = _context.Users.FirstOrDefault(x => x.Id == user.Id);

        if (dbUser == null)
            return NotFound();

        dbUser.FirstName = user.FirstName;
        dbUser.LastName = user.LastName;
        dbUser.Email = user.Email;

        _context.SaveChanges();

        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public IActionResult AddUser(AdminMockUser user)
    {
        user.JoinedOn = DateOnly.FromDateTime(DateTime.Now);

        _context.Users.Add(user);
        _context.SaveChanges();

        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public IActionResult DeleteUser(int id)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == id);

        if (user == null)
            return NotFound();

        _context.Users.Remove(user);
        _context.SaveChanges();

        return RedirectToAction("Dashboard");
    }
}