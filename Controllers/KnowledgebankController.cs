using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KinexusMockup.Models;

namespace KinexusMockup.Controllers;

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
}
