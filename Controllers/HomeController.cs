using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KinexusMockup.Models;
using Microsoft.AspNetCore.Authorization;

namespace KinexusMockup.Controllers;

// Public pages only. Anything that should require a login (the Knowledgebank
// mockups) lives in KnowledgebankController, which has [Authorize] on it.
// Don't add knowledge-bank actions here — doing so would expose those pages
// without authentication and silently bypass the SSO surface.

public class HomeController : Controller
{
    public IActionResult Index() => View();

    [Authorize]
    public IActionResult Privacy() => View();

    public IActionResult NotFoundPage()
    {
        Response.StatusCode = 404;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
