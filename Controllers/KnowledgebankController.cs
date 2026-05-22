using Microsoft.AspNetCore.Mvc;

namespace KinexusMockup.Controllers;

// No [Authorize] here on purpose: unauthenticated users land on every
// Knowledgebank page and see the welcome / SigNET content (rendered by the
// shared layout via _KnowledgebankWelcome.cshtml). Once they sign in, the
// layout falls back to @RenderBody() and the actual page content shows up.
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
