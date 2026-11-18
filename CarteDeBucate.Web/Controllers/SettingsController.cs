using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarteDeBucate.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
