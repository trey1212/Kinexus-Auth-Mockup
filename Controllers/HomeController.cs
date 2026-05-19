using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KinexusMockup.Services;
using KinexusMockup.Models;

namespace KinexusMockup.Controllers;

// Public pages only. Anything that should require a login (the Knowledgebank
// mockups) lives in KnowledgebankController, which has [Authorize] on it.
// Don't add knowledge-bank actions here — doing so would expose those pages
// without authentication and silently bypass the SSO surface.
public class HomeController : Controller
{
    private readonly HomeContentService _contentService;

    public HomeController(HomeContentService contentService)
    {
        _contentService = contentService;
    }

    public IActionResult Index()
    {
        var model = _contentService.GetHomeContent();
        return View(model);
    }

    public IActionResult Privacy() => View();

    public IActionResult Kinatlas() => View();
    public IActionResult TranscriptoNet() => View();
    public IActionResult PhosphoNet() => View();
    public IActionResult OncoNet() => View();
    public IActionResult KinaseNet() => View();
    public IActionResult DrugKinet() => View();
    public IActionResult DrugProNet() => View();
    public IActionResult KinetAM() => View();
    public IActionResult Kinector() => View();
    public IActionResult Login() => View();
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
