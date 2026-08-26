using Microsoft.AspNetCore.Mvc;

namespace YTDownloaderXPro.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() { ViewBag.ActivePage = "dashboard"; return View("Dashboard"); }
    public IActionResult Downloads() { ViewBag.ActivePage = "downloads"; return View(); }
    public IActionResult History() { ViewBag.ActivePage = "history"; return View(); }
    public IActionResult Settings() { ViewBag.ActivePage = "settings"; return View(); }
    public IActionResult About() { ViewBag.ActivePage = "about"; return View(); }
}
