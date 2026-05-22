using KinexusMockup.Auth;
using KinexusMockup.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KinexusMockup.Controllers;

/// <summary>
/// Kinexus's user-facing auth surface. Distinct from
/// <see cref="AuthorizationController"/>, which exposes the protocol-level OIDC
/// endpoints (<c>/connect/authorize</c>, <c>/connect/token</c>,
/// <c>/connect/logout</c>) used by relying parties.
///
/// <para>
/// The two reasons this exists:
/// <list type="number">
///   <item>Routes the <em>user-clicked</em> "Log in" button to ASP.NET Identity's
///         scaffolded login page.</item>
///   <item>Implements the SSO single-logout chain: clicking "Log out" on
///         Kinexus must clear cookies on the Kinexus side <em>and</em> at every
///         relying party, otherwise the user is silently re-authenticated on
///         the next OIDC round-trip.</item>
/// </list>
/// </para>
/// </summary>
public sealed class AccountController : Controller
{
    private readonly SignInManager<AdminMockUser> _signInManager;
    private readonly SsoOptions _sso;

    public AccountController(
        SignInManager<AdminMockUser> signInManager,
        IOptions<SsoOptions> sso)
    {
        _signInManager = signInManager;
        _sso = sso.Value;
    }

    /// <summary>
    /// Routes "Log in" button clicks to the scaffolded Identity login page.
    /// Existed only so layouts can use <c>asp-controller</c>/<c>asp-action</c>
    /// tag helpers consistently instead of mixing in raw area+page links.
    /// </summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
        => RedirectToPage(
            "/Account/Login",
            new { area = "Identity", returnUrl });

    /// <summary>
    /// Single-logout entry point.
    ///
    /// <para>
    /// Step 1: sign out Kinexus's Identity cookie locally.
    /// </para>
    /// <para>
    /// Step 2: hand control to Phosphonet's <c>/Account/Logout</c>. Phosphonet
    /// performs its own SignOut (clears <c>Phosphonet.Auth</c>) and triggers
    /// RP-initiated end-session against this server — which is a near-no-op
    /// since we already cleared the Identity cookie, but it completes the
    /// protocol-level OIDC logout. Phosphonet finally redirects the browser
    /// back to the absolute Kinexus URL it was handed.
    /// </para>
    /// <para>
    /// <paramref name="returnUrl"/> is the local Kinexus path the user was on
    /// when they clicked "Log out" — passed through so they land back there
    /// instead of always at the home page. Non-local values are ignored
    /// (avoids open redirects via this endpoint).
    /// </para>
    /// </summary>
    [HttpGet]
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();

        // Local-path-only allow-list. Anything else collapses to "/".
        var localPath = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/";

        var kinexusReturn = _sso.KinexusBase + localPath;
        var phosphonetLogout =
            $"{_sso.PhosphonetBase}{_sso.PhosphonetLogoutPath}" +
            $"?returnUrl={Uri.EscapeDataString(kinexusReturn)}";

        return Redirect(phosphonetLogout);
    }
}
