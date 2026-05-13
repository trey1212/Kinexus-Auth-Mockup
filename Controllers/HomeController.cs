using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KinexusMockup.Models;

namespace KinexusMockup.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Kinatlas() => View();
    public IActionResult TranscriptoNet() => View();
    public IActionResult PhosphoNet() => View();
    public IActionResult OncoNet() => View();
    public IActionResult KinaseNet() => View();
    public IActionResult DrugKinet() => View();
    public IActionResult DrugProNet() => View();
    public IActionResult KinetAM() => View();
    public IActionResult Kinector() => View();
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
