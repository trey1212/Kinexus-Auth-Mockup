using KinexusMockup.Data;
using KinexusMockup.Models;
using KinexusMockup.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


public class AdminController : Controller
{
    private readonly IEmailService _emailService;
    private readonly UserManager<AdminMockUser> _userManager;

    public AdminController(UserManager<AdminMockUser> userManager, IEmailService emailService)
    {
        _emailService = emailService;
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

        return RedirectToAction("Dashboard");
    }

    /// <summary>
    /// Sends an email to the specified recipient using the details provided in the model.
    /// </summary>
    /// <param name="model">An object containing the recipient's email address, subject, 
    /// message, and user identifier. The model must be valid and contain all required fields.</param>
    /// <returns>A redirect to the Dashboard view. If the email is sent successfully, 
    /// a success message is stored in TempData; otherwise, an error message is provided.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmail(AdminEmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Email could not be sent. Please check the subject and message.";
            return RedirectToAction("Dashboard");
        }

        var user = _context.Users.FirstOrDefault(x => x.Id == model.UserId);

        if (user == null)
        {
            TempData["AdminError"] = "User could not be found.";
            return RedirectToAction("Dashboard");
        }

        try
        {
            await _emailService.SendEmailAsync(
                model.RecipientEmail,
                model.Subject,
                model.Message
            );

            TempData["AdminMessage"] = $"Email sent to {model.RecipientEmail}.";
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"Email failed to send: {ex.Message}";
        }

        return RedirectToAction("Dashboard");
    }
}