using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using CarteDeBucate.Web.Models;
using CarteDeBucate.Web.Resources;

namespace CarteDeBucate.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly IUserRepository _userRepository;

    public SettingsController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public IActionResult Index()
    {
        User? user = GetCurrentUser();

        if (user is null)
        {
            return Forbid();
        }

        string preferredCulture = ApplicationLocalization
            .TryNormalizeSupportedCulture(user.PreferredCulture, out string storedCulture)
                ? storedCulture
                : ApplicationLocalization
                    .GetSupportedCultureOrFallback(CultureInfo.CurrentUICulture.Name)
                    .Name;

        return View(new PreferredCultureViewModel
        {
            PreferredCulture = preferredCulture
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePreferredCulture(PreferredCultureViewModel model)
    {
        User? user = GetCurrentUser();

        if (user is null)
        {
            return Forbid();
        }

        if (!ApplicationLocalization.TryNormalizeSupportedCulture(
                model.PreferredCulture,
                out string culture))
        {
            ModelState.AddModelError(
                nameof(model.PreferredCulture),
                ValidationMessages.CultureInvalid);
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), model);
        }

        if (!_userRepository.UpdatePreferredCulture(user.Id, culture))
        {
            return Forbid();
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        return RedirectToAction(nameof(Index));
    }

    private User? GetCurrentUser()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userId, out int parsedUserId)
            ? _userRepository.GetById(parsedUserId)
            : null;
    }
}
