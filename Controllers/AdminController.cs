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

    /// <summary>
    /// Ends the session of a user based on their id.
    /// </summary>
    /// <param name="userId">The id of the user to have their session ended.</param>
    /// <returns>Redirect to admin dashboard</returns>
    [HttpPost]
    public async Task<IActionResult> EndSession(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        await _userManager.UpdateSecurityStampAsync(user);

        return RedirectToAction("Dashboard");
    }

    /// <summary>
    /// Resets a users password based on their id.
    /// </summary>
    /// <param name="userId">The id of the target user.</param>
    /// <param name="newPassword">The new password of which will become their actual password.</param>
    /// <returns>Redirect to dashboard.</returns>
    [HttpPost]
    public async Task<IActionResult> ResetPassword(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        var removeResult = await _userManager.RemovePasswordAsync(user);

        if (!removeResult.Succeeded)
            return BadRequest(removeResult.Errors);

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);

        if (!addResult.Succeeded)
            return BadRequest(addResult.Errors);

        await _userManager.UpdateSecurityStampAsync(user);

        return RedirectToAction("Dashboard");
    }

    /// <summary>
    /// Updates a users info based on their id.
    /// </summary>
    /// <param name="id">The users id.</param>
    /// <param name="firstName">The users new name.</param>
    /// <param name="lastName">The users new last name</param>
    /// <param name="email">The users new email.</param>
    /// <returns>Redirect to dashboard.</returns>
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

    /// <summary>
    /// Adds a user to the database.
    /// </summary>
    /// <param name="firstName">The users first name</param>
    /// <param name="lastName">The users last name</param>
    /// <param name="email">The users email</param>
    /// <param name="password">The users password</param>
    /// <returns>Redirect to dashboard</returns>
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
            TempData["AdminError"] = "User could not be added.";
            return RedirectToAction("Dashboard");
        }

        TempData["AdminMessage"] = $"User {email} was added successfully.";
        return RedirectToAction("Dashboard");
    }

    /// <summary>
    /// Deletes the user with the specified id.
    /// </summary>
    /// <param name="id">The unique id of the user to delete.</param>
    /// <returns>Redirect to dashboard.</returns>
    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            TempData["AdminError"] = "User could not be found.";
            return NotFound();
        }

        var email = user.Email;

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            TempData["AdminError"] = "User could not be deleted.";
            return RedirectToAction("Dashboard");
        }

        TempData["AdminMessage"] = $"User {email} was deleted successfully.";
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
            TempData["AdminError"] = "Email could not be sent.";
            return RedirectToAction("Dashboard");
        }

        var user = await _userManager.FindByIdAsync(model.UserId);

        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            TempData["AdminError"] = "User could not be found or does not have an email address.";
            return RedirectToAction("Dashboard");
        }

        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                model.Subject,
                model.Message
            );

            TempData["AdminMessage"] = $"Email sent successfully to {user.Email}.";
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"Email failed to send: {ex.Message}";
        }

        return RedirectToAction("Dashboard");
    }
}