using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinexusMockup.Controllers;

// [Authorize] on the whole controller means EVERY action below requires
// a logged-in user. If someone hits any of these URLs without being signed
// in, they'll be sent to the login page first and bounced back after.
[Authorize]
[Route("[action]")]
public class KnowledgebankController : Controller
{
    public IActionResult Kinatlas() => View();
    public IActionResult TranscriptoNet() => View();
    public IActionResult PhosphoNet() => View();
    public IActionResult OncoNet() => View();
    public IActionResult KinaseNet() => View();
    public IActionResult DrugKinet() => View();
    public IActionResult DrugProNet() => View();
    public IActionResult KinetAM() => View();
    public IActionResult Kinector() => View();
    public IActionResult Signet() => View();
}
