using KinexusMockup.Data;
using KinexusMockup.Models;
using KinexusMockup.Services.Email;
using Microsoft.AspNetCore.Mvc;


public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public AdminController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
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