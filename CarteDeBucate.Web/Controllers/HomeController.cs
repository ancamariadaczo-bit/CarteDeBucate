using System.Diagnostics;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CarteDeBucate.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Recipes");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        IExceptionHandlerPathFeature? exceptionFeature =
            HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionFeature != null)
        {
            _logger.LogError(
                exceptionFeature.Error,
                "Unhandled exception while processing {RequestMethod} {RequestPath}. Trace identifier: {TraceIdentifier}",
                HttpContext.Request.Method,
                exceptionFeature.Path,
                HttpContext.TraceIdentifier);
        }

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
