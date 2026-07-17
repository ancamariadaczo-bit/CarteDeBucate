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
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthenticationService authenticationService,
        WebAppSettings settings,
        ILogger<AccountController> logger)
    {
        _authenticationService = authenticationService;
        _settings = settings;
        _logger = logger;
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
            _logger.LogWarning("User login failed");

            ModelState.AddModelError("", result.Message);

            return View(model);
        }

        await SignInUser(result.User);

        _logger.LogInformation(
            "User login succeeded. User identifier: {UserId}",
            result.User.Id);

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
            _logger.LogWarning("User registration failed");

            ModelState.AddModelError("", result.Message);

            return View(model);
        }

        await SignInUser(result.User);

        _logger.LogInformation(
            "User registration succeeded. User identifier: {UserId}",
            result.User.Id);

        return RedirectToAction(
            nameof(RecipesController.Index),
            "Recipes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _authenticationService.Logout();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation(
            "User logout succeeded. User identifier: {UserId}",
            userId);

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
        _logger.LogWarning(
            "Access denied for user {UserId}",
            User.FindFirstValue(ClaimTypes.NameIdentifier));

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
