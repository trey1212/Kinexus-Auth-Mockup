using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using KinexusMockup.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace KinexusMockup.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly SignInManager<AdminMockUser> _signInManager;
    private readonly UserManager<AdminMockUser> _userManager;
    private readonly IUserStore<AdminMockUser> _userStore;
    private readonly IUserEmailStore<AdminMockUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    //private readonly IEmailSender _emailSender;

    public RegisterModel(
        UserManager<AdminMockUser> userManager,
        IUserStore<AdminMockUser> userStore,
        SignInManager<AdminMockUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        //_emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = [];

    public class InputModel
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool AgreeToTerms { get; set; } = true;
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = CreateUser();

        await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password.");

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code, returnUrl },
                protocol: Request.Scheme);

            if (callbackUrl is not null)
            {
                var encodedUrl = HtmlEncoder.Default.Encode(callbackUrl);
                //await _emailSender.SendEmailAsync(
                //    Input.Email,
                //    "Confirm your email",
                //    $"Please confirm your account by <a href='{encodedUrl}'>clicking here</a>.");
            }

            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    private AdminMockUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<AdminMockUser>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Can't create an instance of '{nameof(AdminMockUser)}'. Ensure that the identity user type has a parameterless constructor.",
                ex);
        }
    }

    private IUserEmailStore<AdminMockUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }

        return (IUserEmailStore<AdminMockUser>)_userStore;
    }
}
