using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Security.Claims;
using CarteDeBucate.Web.Models;
using CarteDeBucate.Web.Resources;

namespace CarteDeBucate.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IAccountEmailService _accountEmailService;

    public SettingsController(
        IUserRepository userRepository,
        IAccountEmailService accountEmailService)
    {
        _userRepository = userRepository;
        _accountEmailService = accountEmailService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        User? user = GetCurrentUser();

        if (user is null)
        {
            return Forbid();
        }

        return View(CreateIndexViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePreferredCulture(
        [Bind(Prefix = nameof(SettingsIndexViewModel.PreferredCulture))]
        PreferredCultureViewModel model)
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
                $"{nameof(SettingsIndexViewModel.PreferredCulture)}.{nameof(model.PreferredCulture)}",
                ValidationMessages.CultureInvalid);
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), CreateIndexViewModel(user, model));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AuthenticationRateLimitPolicies.EmailChange)]
    public async Task<IActionResult> RequestEmailChange(
        [Bind(Prefix = nameof(SettingsIndexViewModel.EmailChange))]
        EmailChangeViewModel model)
    {
        User? user = GetCurrentUser();

        if (user is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), CreateIndexViewModel(user, emailChange: model));
        }

        AccountEmailChangeRequestResult result =
            await _accountEmailService.RequestEmailChangeAsync(
                new AccountEmailChangeRequestInput(
                    user.Id,
                    model.CurrentPassword,
                    model.NewEmail,
                    CultureInfo.CurrentUICulture.Name),
                HttpContext.RequestAborted);

        if (result.Code is AccountEmailChangeRequestResultCode.Sent
            or AccountEmailChangeRequestResultCode.DeliveryFailed)
        {
            return RedirectToAction(nameof(EmailChangePending));
        }

        AddEmailChangeRequestError(result.Code);
        return View(nameof(Index), CreateIndexViewModel(user, emailChange: model));
    }

    [HttpGet]
    public IActionResult EmailChangePending()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AuthenticationRateLimitPolicies.EmailChange)]
    public async Task<IActionResult> ResendEmailChange()
    {
        User? user = GetCurrentUser();

        if (user is null)
        {
            return Forbid();
        }

        await _accountEmailService.ResendEmailChangeAsync(
            user.Id,
            CultureInfo.CurrentUICulture.Name,
            HttpContext.RequestAborted);

        return RedirectToAction(nameof(EmailChangePending));
    }

    [HttpGet]
    public IActionResult ConfirmEmailChange(string? token)
    {
        User? user = GetCurrentUser();

        if (user is null)
        {
            return Forbid();
        }

        AccountEmailChangeReviewResult result =
            _accountEmailService.ReviewEmailChange(user.Id, token);

        return result.Code switch
        {
            AccountEmailChangeReviewResultCode.Valid =>
                View(new ConfirmEmailChangeViewModel { Token = token! }),
            AccountEmailChangeReviewResultCode.ExpiredToken =>
                RedirectToAction(nameof(EmailChangeExpired)),
            AccountEmailChangeReviewResultCode.InvalidToken =>
                RedirectToAction(nameof(EmailChangeInvalid)),
            _ => throw new InvalidOperationException(
                $"Unexpected email-change review result: {result.Code}.")
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AuthenticationRateLimitPolicies.EmailVerification)]
    public async Task<IActionResult> ConfirmEmailChange(
        ConfirmEmailChangeViewModel model)
    {
        User? user = GetCurrentUser();

        if (user is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(EmailChangeInvalid));
        }

        AccountEmailChangeConfirmationResult result =
            await _accountEmailService.ConfirmEmailChangeAsync(
                user.Id,
                model.Token,
                HttpContext.RequestAborted);

        switch (result.Code)
        {
            case AccountEmailChangeConfirmationResultCode.Success:
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(AccountController.EmailChanged), "Account");
            case AccountEmailChangeConfirmationResultCode.ExpiredToken:
                return RedirectToAction(nameof(EmailChangeExpired));
            case AccountEmailChangeConfirmationResultCode.InvalidToken:
            case AccountEmailChangeConfirmationResultCode.DuplicateEmail:
                return RedirectToAction(nameof(EmailChangeInvalid));
            default:
                throw new InvalidOperationException(
                    $"Unexpected email-change confirmation result: {result.Code}.");
        }
    }

    [HttpGet]
    public IActionResult EmailChangeInvalid()
    {
        return View();
    }

    [HttpGet]
    public IActionResult EmailChangeExpired()
    {
        return View();
    }

    private User? GetCurrentUser()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userId, out int parsedUserId)
            ? _userRepository.GetById(parsedUserId)
            : null;
    }

    private static SettingsIndexViewModel CreateIndexViewModel(
        User user,
        PreferredCultureViewModel? preferredCulture = null,
        EmailChangeViewModel? emailChange = null)
    {
        string culture = ApplicationLocalization
            .TryNormalizeSupportedCulture(user.PreferredCulture, out string storedCulture)
                ? storedCulture
                : ApplicationLocalization
                    .GetSupportedCultureOrFallback(CultureInfo.CurrentUICulture.Name)
                    .Name;

        return new SettingsIndexViewModel
        {
            CurrentEmail = user.Email,
            PreferredCulture = preferredCulture ?? new PreferredCultureViewModel
            {
                PreferredCulture = culture
            },
            EmailChange = emailChange ?? new EmailChangeViewModel()
        };
    }

    private void AddEmailChangeRequestError(AccountEmailChangeRequestResultCode code)
    {
        (string field, string message) = code switch
        {
            AccountEmailChangeRequestResultCode.InvalidCurrentPassword =>
                ($"{nameof(SettingsIndexViewModel.EmailChange)}.{nameof(EmailChangeViewModel.CurrentPassword)}",
                    ValidationMessages.CurrentPasswordInvalid),
            AccountEmailChangeRequestResultCode.InvalidEmail =>
                ($"{nameof(SettingsIndexViewModel.EmailChange)}.{nameof(EmailChangeViewModel.NewEmail)}",
                    ValidationMessages.EmailInvalid),
            AccountEmailChangeRequestResultCode.DuplicateEmail =>
                ($"{nameof(SettingsIndexViewModel.EmailChange)}.{nameof(EmailChangeViewModel.NewEmail)}",
                    ValidationMessages.EmailAlreadyInUse),
            AccountEmailChangeRequestResultCode.UserNotFound =>
                (string.Empty, SharedMessages.AuthInvalidCredentials),
            _ => throw new InvalidOperationException(
                $"Unexpected email-change request result: {code}.")
        };

        ModelState.AddModelError(field, message);
    }
}
