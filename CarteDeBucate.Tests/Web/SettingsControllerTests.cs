using System.Security.Claims;
using CarteDeBucate.Web.Controllers;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

public class SettingsControllerTests
{
    [Fact]
    public void Index_ShouldReturnTheAuthenticatedUsersPreferredCulture()
    {
        SettingsController controller = CreateController(
            CreateUserRepository("ro-RO"));

        IActionResult result = controller.Index();

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        SettingsIndexViewModel model =
            Assert.IsType<SettingsIndexViewModel>(viewResult.Model);
        Assert.Equal("ro-RO", model.PreferredCulture.PreferredCulture);
    }

    [Fact]
    public void UpdatePreferredCulture_ShouldPersistTheValueAndSetTheCultureCookie()
    {
        FakeUserRepository repository = CreateUserRepository("en-US");
        SettingsController controller = CreateController(repository);

        IActionResult result = controller.UpdatePreferredCulture(
            new PreferredCultureViewModel { PreferredCulture = "ro-RO" });

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(SettingsController.Index), redirectResult.ActionName);
        Assert.Equal("ro-RO", repository.GetById(7)?.PreferredCulture);
        string setCookie = controller.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains(
            ".AspNetCore.Culture",
            setCookie);
        Assert.Contains("ro-RO", setCookie);
    }

    [Fact]
    public void UpdatePreferredCulture_WithUnsupportedValue_ShouldNotPersist()
    {
        FakeUserRepository repository = CreateUserRepository("en-US");
        SettingsController controller = CreateController(repository);

        IActionResult result = controller.UpdatePreferredCulture(
            new PreferredCultureViewModel { PreferredCulture = "de-DE" });

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(SettingsController.Index), viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal("en-US", repository.GetById(7)?.PreferredCulture);
    }

    [Fact]
    public async Task RequestEmailChange_ShouldDelegateTheAuthenticatedUserAndRedirectToPending()
    {
        FakeUserRepository repository = CreateUserRepository("en-US");
        FakeAccountEmailService emailService = new();
        SettingsController controller = CreateController(repository, emailService);

        IActionResult result = await controller.RequestEmailChange(new EmailChangeViewModel
        {
            NewEmail = "new@example.com",
            CurrentPassword = "correct-password"
        });

        RedirectToActionResult redirect =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(SettingsController.EmailChangePending), redirect.ActionName);
        Assert.True(emailService.RequestWasCalled);
        Assert.Equal(7, emailService.RequestInput?.UserId);
        Assert.Equal("new@example.com", emailService.RequestInput?.NewEmail);
    }

    [Fact]
    public async Task ConfirmEmailChange_WhenCommitted_ShouldSignOutAndRedirectToCleanUrl()
    {
        FakeUserRepository repository = CreateUserRepository("en-US");
        FakeAccountEmailService emailService = new();
        TrackingAuthenticationService authenticationService = new();
        SettingsController controller = CreateController(
            repository,
            emailService,
            authenticationService);

        IActionResult result = await controller.ConfirmEmailChange(
            new ConfirmEmailChangeViewModel { Token = "valid-token" });

        RedirectToActionResult redirect =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.EmailChanged), redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
        Assert.True(authenticationService.SignOutWasCalled);
        Assert.Equal(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authenticationService.SignOutScheme);
        Assert.Null(redirect.RouteValues);
    }

    [Fact]
    public void ConfirmEmailChangeGet_ShouldApplyNoReferrerPolicy()
    {
        SettingsController controller = CreateController(CreateUserRepository("en-US"));

        IActionResult result = controller.ConfirmEmailChange("raw-token");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        ConfirmEmailChangeViewModel model =
            Assert.IsType<ConfirmEmailChangeViewModel>(viewResult.Model);
        Assert.Equal("raw-token", model.Token);
        Assert.Equal(
            "no-referrer",
            controller.Response.Headers["Referrer-Policy"].ToString());
    }

    private static SettingsController CreateController(
        FakeUserRepository repository,
        FakeAccountEmailService? emailService = null,
        TrackingAuthenticationService? authenticationService = null)
    {
        ServiceCollection services = new();
        authenticationService ??= new TrackingAuthenticationService();
        services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationService>(
            authenticationService);
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "7")],
                "Test")),
            RequestServices = services.BuildServiceProvider()
        };

        SettingsController controller = new SettingsController(
            repository,
            emailService ?? new FakeAccountEmailService())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider()),
            Url = new TestUrlHelper()
        };

        return controller;
    }

    private static FakeUserRepository CreateUserRepository(string preferredCulture)
    {
        FakeUserRepository repository = new();
        repository.Users.Add(new User
        {
            Id = 7,
            Email = "anca@example.com",
            PreferredCulture = preferredCulture
        });

        return repository;
    }

    private sealed class TrackingAuthenticationService
        : Microsoft.AspNetCore.Authentication.IAuthenticationService
    {
        public bool SignOutWasCalled { get; private set; }
        public string? SignOutScheme { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(
            HttpContext context,
            string? scheme) => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            SignOutWasCalled = true;
            SignOutScheme = scheme;
            return Task.CompletedTask;
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }

    private sealed class TestUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new ActionContext();

        public string? Action(UrlActionContext actionContext) => null;

        public string? Content(string? contentPath) => contentPath;

        public bool IsLocalUrl(string? url) =>
            !string.IsNullOrWhiteSpace(url) &&
            url.StartsWith("/", StringComparison.Ordinal) &&
            !url.StartsWith("//", StringComparison.Ordinal);

        public string? Link(string? routeName, object? values) => null;

        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }
}
