using KinexusMockup.Data;
using KinexusMockup.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class AdminController : Controller
{

    private readonly UserManager<AdminMockUser> _userManager;

    public AdminController(UserManager<AdminMockUser> userManager)
    {
        _userManager = userManager;
    }

    public IActionResult Dashboard()
    {
        var users = _userManager.Users.ToList();

        var model = new AdminDashboardViewModel
        {
            Users = users,
            TotalUsers = users.Count
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUser(string id, string firstName, string lastName, string email)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.UserName = email;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> AddUser(string firstName, string lastName, string email, string password)
    {
        var user = new AdminMockUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            JoinedOn = DateOnly.FromDateTime(DateTime.Now)
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            return RedirectToAction("Dashboard");
        }

        return View();
    }
}