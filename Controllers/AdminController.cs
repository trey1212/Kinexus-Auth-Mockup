using KinexusMockup.Models;
using KinexusMockup.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KinexusMockup.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IEmailService _emailService;
    private readonly UserManager<AdminMockUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        UserManager<AdminMockUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmailService emailService)
    {
        _emailService = emailService;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        var users = _userManager.Users.ToList();
        var adminUserIds = new HashSet<string>();

        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                adminUserIds.Add(user.Id);
            }
        }

        ViewBag.AdminUserIds = adminUserIds;

        var model = new AdminDashboardViewModel
        {
            Users = users,
            TotalUsers = users.Count
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeAdmin(string userId)
    {
        const string adminRole = "Admin";

        if (!await _roleManager.RoleExistsAsync(adminRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            TempData["AdminError"] = "User could not be found.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (!await _userManager.IsInRoleAsync(user, adminRole))
        {
            var result = await _userManager.AddToRoleAsync(user, adminRole);

            if (!result.Succeeded)
            {
                TempData["AdminError"] = "User could not be promoted to admin.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        TempData["AdminMessage"] = $"{user.Email} is now an admin.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAdmin(string userId)
    {
        const string adminRole = "Admin";

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            TempData["AdminError"] = "User could not be found.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (await _userManager.IsInRoleAsync(user, adminRole))
        {
            var result = await _userManager.RemoveFromRoleAsync(user, adminRole);

            if (!result.Succeeded)
            {
                TempData["AdminError"] = "Admin role could not be removed.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        TempData["AdminMessage"] = $"{user.Email} is no longer an admin.";
        return RedirectToAction(nameof(Dashboard));
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
            var attachment = await BuildImageAttachmentAsync(model.ImageAttachment);

            await _emailService.SendEmailAsync(
                user.Email!,
                model.Subject,
                model.Message,
                attachment
            );

            TempData["AdminMessage"] = attachment == null
                ? $"Email sent successfully to {user.Email}."
                : $"Email with image sent successfully to {user.Email}.";
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"Email failed to send: {ex.Message}";
        }

        return RedirectToAction("Dashboard");
    }

    /// <summary>
    /// Sends an email to every registered user with an email address.
    /// Each recipient gets an individual email so user email addresses are not exposed to other users.
    /// </summary>
    /// <param name="model">The subject and message entered by the admin.</param>
    /// <returns>Redirects to the dashboard with a success or error message.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmailToAll(AdminBulkEmailViewModel model)
    {
        EmailAttachment? attachment;

        try
        {
            attachment = await BuildImageAttachmentAsync(model.ImageAttachment);
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"Email image could not be attached: {ex.Message}";
            return RedirectToAction("Dashboard");
        }

        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Email could not be sent. Please enter a subject and message.";
            return RedirectToAction("Dashboard");
        }

        var users = _userManager.Users
            .Where(user => !string.IsNullOrWhiteSpace(user.Email))
            .ToList();

        if (users.Count == 0)
        {
            TempData["AdminError"] = "No registered users with email addresses were found.";
            return RedirectToAction("Dashboard");
        }

        var sentCount = 0;
        var failedResults = new List<string>();

        // Due to Mailtraps rate limit, the below won't work. However it will work with a real
        // SMTP server and should be swapped with the loop below once set up.
        //foreach (var user in users)
        //{
        //    var recipientEmail = user.Email?.Trim();

        //    if (string.IsNullOrWhiteSpace(recipientEmail))
        //    {
        //        failedResults.Add("A user had no email address.");
        //        continue;
        //    }

        //    try
        //    {
        //        Console.WriteLine($"Sending bulk email to: [{recipientEmail}]");

        //        await _emailService.SendEmailAsync(
        //            recipientEmail,
        //            model.Subject,
        //            model.Message
        //        );

        //        sentCount++;
        //    }
        //    catch (Exception ex)
        //    {
        //        failedResults.Add($"{recipientEmail}: {ex.Message}");
        //    }
        //}

        // Replace this with the above once a real SMTP is in place.
        for (var i = 0; i < users.Count; i++)
        {
            var user = users[i];
            var recipientEmail = user.Email?.Trim();

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                failedResults.Add("A user had no email address.");
                continue;
            }

            try
            {
                await _emailService.SendEmailAsync(
                    recipientEmail,
                    model.Subject,
                    model.Message,
                    attachment
                );

                sentCount++;
            }
            catch (Exception ex)
            {
                failedResults.Add($"{recipientEmail}: {ex.Message}");
            }

            // Mailtrap's free/testing plan can rate-limit rapid bulk sends.
            // For the prototype, we send one email at a time with a short delay.
            //if (i < users.Count - 1)
            //{
            //    await Task.Delay(TimeSpan.FromSeconds(10));
            //}
        }

        if (failedResults.Count == 0)
        {
            TempData["AdminMessage"] = attachment == null
                ? $"Email sent successfully to all {sentCount} user(s)."
                : $"Email with image sent successfully to all {sentCount} user(s).";
        }
        else
        {
            TempData["AdminError"] =
                $"Email sent to {sentCount} user(s), but failed for {failedResults.Count} user(s). " +
                $"Failed: {string.Join("; ", failedResults)}";
        }

        return RedirectToAction("Dashboard");
    }

    private static async Task<EmailAttachment?> BuildImageAttachmentAsync(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            return null;
        }

        const long maxImageSizeBytes = 2 * 1024 * 1024;

        var allowedContentTypes = new HashSet<string>
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

        if (imageFile.Length > maxImageSizeBytes)
        {
            throw new InvalidOperationException("Image must be 2 MB or smaller.");
        }

        if (!allowedContentTypes.Contains(imageFile.ContentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException("Only JPG, PNG, GIF, or WebP images can be attached.");
        }

        await using var memoryStream = new MemoryStream();
        await imageFile.CopyToAsync(memoryStream);

        return new EmailAttachment(
            Path.GetFileName(imageFile.FileName),
            imageFile.ContentType,
            memoryStream.ToArray()
        );
    }
}