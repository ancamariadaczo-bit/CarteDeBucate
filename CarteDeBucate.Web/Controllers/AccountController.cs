using System.Security.Claims;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CarteDeBucate.Web.Resources;
using System.Globalization;

namespace CarteDeBucate.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAccountRegistrationService _accountRegistrationService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthenticationService authenticationService,
        IAccountRegistrationService accountRegistrationService,
        ILogger<AccountController> logger)
    {
        _authenticationService = authenticationService;
        _accountRegistrationService = accountRegistrationService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectAfterAuthentication(returnUrl);
        }

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

            ModelState.AddModelError("", SharedMessages.AuthInvalidCredentials);

            return View(model);
        }

        await SignInUser(result.User);

        _logger.LogInformation(
            "User login succeeded. User identifier: {UserId}",
            result.User.Id);

        return RedirectAfterAuthentication(model.ReturnUrl);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(RecipesController.Index), "Recipes");
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AccountRegistrationServiceResult result =
            await _accountRegistrationService.RegisterAsync(
                new AccountRegistrationInput(
                    model.Email,
                    model.Password,
                    model.ConfirmPassword,
                    CultureInfo.CurrentUICulture.Name),
                HttpContext.RequestAborted);

        if (result.Code is AccountRegistrationServiceResultCode.Success
            or AccountRegistrationServiceResultCode.DuplicateEmail
            or AccountRegistrationServiceResultCode.DeliveryFailed)
        {
            if (result.Code == AccountRegistrationServiceResultCode.DeliveryFailed)
            {
                _logger.LogWarning(
                    "Registration was persisted, but verification email delivery failed");
            }
            else
            {
                _logger.LogInformation("Registration request completed");
            }

            return RedirectToAction(nameof(CheckEmail));
        }

        AddRegistrationValidationError(result.Code);
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult CheckEmail()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult VerifyEmail(string? token)
    {
        AccountEmailVerificationReviewResult result =
            _accountRegistrationService.ReviewEmailVerification(token);

        return result.Code switch
        {
            AccountEmailVerificationReviewResultCode.Valid =>
                View(new VerifyEmailViewModel { Token = token! }),
            AccountEmailVerificationReviewResultCode.ExpiredToken =>
                RedirectToAction(nameof(EmailVerificationExpired)),
            AccountEmailVerificationReviewResultCode.InvalidToken =>
                RedirectToAction(nameof(EmailVerificationInvalid)),
            _ => throw new InvalidOperationException(
                $"Unexpected email verification review result: {result.Code}.")
        };
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyEmail(VerifyEmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(EmailVerificationInvalid));
        }

        AccountEmailVerificationResult result =
            _accountRegistrationService.ConfirmEmailVerification(model.Token);

        return result.Code switch
        {
            AccountEmailVerificationResultCode.Success =>
                RedirectToAction(nameof(EmailVerified)),
            AccountEmailVerificationResultCode.ExpiredToken =>
                RedirectToAction(nameof(EmailVerificationExpired)),
            AccountEmailVerificationResultCode.InvalidToken =>
                RedirectToAction(nameof(EmailVerificationInvalid)),
            _ => throw new InvalidOperationException(
                $"Unexpected email verification result: {result.Code}.")
        };
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult EmailVerified()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult EmailVerificationInvalid()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult EmailVerificationExpired()
    {
        return View();
    }

    private void AddRegistrationValidationError(
        AccountRegistrationServiceResultCode resultCode)
    {
        (string field, string message) = resultCode switch
        {
            AccountRegistrationServiceResultCode.InvalidEmail =>
                (nameof(RegisterViewModel.Email), ValidationMessages.EmailInvalid),
            AccountRegistrationServiceResultCode.PasswordRequired =>
                (nameof(RegisterViewModel.Password), ValidationMessages.PasswordRequired),
            AccountRegistrationServiceResultCode.PasswordTooShort =>
                (nameof(RegisterViewModel.Password),
                    ValidationMessages.RegistrationPasswordLength),
            AccountRegistrationServiceResultCode.PasswordConfirmationRequired =>
                (nameof(RegisterViewModel.ConfirmPassword),
                    ValidationMessages.ConfirmPasswordRequired),
            AccountRegistrationServiceResultCode.PasswordConfirmationMismatch =>
                (nameof(RegisterViewModel.ConfirmPassword),
                    ValidationMessages.PasswordMismatch),
            _ => throw new InvalidOperationException(
                $"Unexpected registration result: {resultCode}.")
        };

        ModelState.AddModelError(field, message);
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

        return RedirectToAction(nameof(Login));
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
            new Claim(ClaimTypes.Name, user.Email)
        ];
        ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    private IActionResult RedirectAfterAuthentication(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url?.IsLocalUrl(returnUrl) == true)
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(RecipesController.Index), "Recipes");
    }
}
