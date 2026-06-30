using System.Security.Claims;
using CarteDeBucate.Web.Configuration;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CarteDeBucate.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthenticationService _authenticationService;
    private readonly WebAppSettings _settings;

    public AccountController(
        IAuthenticationService authenticationService,
        WebAppSettings settings)
    {
        _authenticationService = authenticationService;
        _settings = settings;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AuthenticationResult result =
            _authenticationService.Login(model.Username, model.Password);

        if (!result.IsSuccess || result.User == null)
        {
            ModelState.AddModelError("", result.Message);

            return View(model);
        }

        await SignInUser(result.User);

        return RedirectAfterAuthentication(model.ReturnUrl);
    }

    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AuthenticationResult result =
            _authenticationService.Register(model.Username, model.Password);

        if (!result.IsSuccess || result.User == null)
        {
            ModelState.AddModelError("", result.Message);

            return View(model);
        }

        await SignInUser(result.User);

        return RedirectToAction(
            nameof(RecipesController.Index),
            "Recipes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        _authenticationService.Logout();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (_settings.AuthenticationEnabled)
        {
            return RedirectToAction(nameof(Login));
        }

        return RedirectToAction(
            nameof(RecipesController.Index),
            "Recipes");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInUser(User user)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        ];
        ClaimsIdentity identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);
    }

    private IActionResult RedirectAfterAuthentication(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl)
            && Url?.IsLocalUrl(returnUrl) == true)
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(
            nameof(RecipesController.Index),
            "Recipes");
    }
}
