using System.Security.Claims;
using System.Reflection;
using CarteDeBucate.Web.Controllers;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

public class AccountControllerTests
{
    [Theory]
    [InlineData(
        nameof(AccountController.Login),
        typeof(LoginViewModel),
        AuthenticationRateLimitPolicies.Login)]
    [InlineData(
        nameof(AccountController.Register),
        typeof(RegisterViewModel),
        AuthenticationRateLimitPolicies.Registration)]
    [InlineData(
        nameof(AccountController.ResendVerification),
        typeof(ResendVerificationViewModel),
        AuthenticationRateLimitPolicies.ResendVerification)]
    [InlineData(
        nameof(AccountController.VerifyEmail),
        typeof(VerifyEmailViewModel),
        AuthenticationRateLimitPolicies.EmailVerification)]
    public void AuthenticationPostActions_ShouldUseExpectedRateLimitPolicy(
        string actionName,
        Type modelType,
        string expectedPolicyName)
    {
        MethodInfo action = typeof(AccountController).GetMethod(
            actionName,
            [modelType])!;

        EnableRateLimitingAttribute attribute = Assert.IsType<EnableRateLimitingAttribute>(
            Assert.Single(action.GetCustomAttributes<EnableRateLimitingAttribute>()));

        Assert.Equal(expectedPolicyName, attribute.PolicyName);
    }

    [Fact]
    public void VerifyEmailGet_ShouldNotUseRateLimitPolicy()
    {
        MethodInfo action = typeof(AccountController).GetMethod(
            nameof(AccountController.VerifyEmail),
            [typeof(string)])!;

        Assert.Empty(action.GetCustomAttributes<EnableRateLimitingAttribute>());
    }

    [Fact]
    public void LoginGet_ShouldReturnViewWithReturnUrl()
    {
        AccountController controller = CreateController(
            new FakeAuthenticationService());

        IActionResult result = controller.Login("/Recipes/Create");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        LoginViewModel model = Assert.IsType<LoginViewModel>(viewResult.Model);
        Assert.Equal("/Recipes/Create", model.ReturnUrl);
    }

    [Fact]
    public void LoginGet_WhenUserIsAuthenticated_ShouldRedirectToLocalReturnUrl()
    {
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            user: CreateAuthenticatedUser());

        IActionResult result = controller.Login(
            "/api/authentication/extension-complete");

        LocalRedirectResult redirectResult =
            Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal(
            "/api/authentication/extension-complete",
            redirectResult.Url);
    }

    [Fact]
    public void LoginGet_WhenUserIsAuthenticatedWithoutReturnUrl_ShouldRedirectToRecipes()
    {
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            user: CreateAuthenticatedUser());

        IActionResult result = controller.Login();

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.Equal("Recipes", redirectResult.ControllerName);
    }

    [Fact]
    public void LoginGet_WhenUserIsAuthenticatedWithExternalReturnUrl_ShouldRedirectToRecipes()
    {
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            user: CreateAuthenticatedUser());

        IActionResult result = controller.Login("https://example.test/steal-cookie");

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.Equal("Recipes", redirectResult.ControllerName);
    }

    [Fact]
    public async Task LoginPost_WithInvalidModel_ShouldReturnViewAndNotCallService()
    {
        FakeAuthenticationService authenticationService = new FakeAuthenticationService();
        AccountController controller = CreateController(authenticationService);
        LoginViewModel model = new LoginViewModel();
        controller.ModelState.AddModelError("Username", "Username-ul este obligatoriu.");

        IActionResult result = await controller.Login(model);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.False(authenticationService.LoginWasCalled);
    }

    [Fact]
    public async Task LoginPost_WhenLoginFails_ShouldReturnViewWithModelError()
    {
        FakeAuthenticationService authenticationService = new FakeAuthenticationService
        {
            LoginResult = new AuthenticationResult
            {
                IsSuccess = false,
                Message = "Invalid username or password."
            }
        };
        AccountController controller = CreateController(authenticationService);
        LoginViewModel model = CreateLoginModel();

        IActionResult result = await controller.Login(model);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.True(authenticationService.LoginWasCalled);
        Assert.Equal("anca", authenticationService.UsernamePassedToLogin);
        Assert.Equal("secret-password", authenticationService.PasswordPassedToLogin);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task LoginPost_WhenLoginSucceeds_ShouldSignInAndRedirectToRecipes()
    {
        FakeAuthenticationService authenticationService = new FakeAuthenticationService
        {
            LoginResult = CreateSuccessfulAuthenticationResult()
        };
        TestHttpAuthenticationService httpAuthenticationService =
            new TestHttpAuthenticationService();
        AccountController controller = CreateController(
            authenticationService,
            httpAuthenticationService: httpAuthenticationService);

        IActionResult result = await controller.Login(CreateLoginModel());

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.Equal("Recipes", redirectResult.ControllerName);
        Assert.True(httpAuthenticationService.SignInWasCalled);
        Assert.Equal(
            CookieAuthenticationDefaults.AuthenticationScheme,
            httpAuthenticationService.SignInScheme);
        Assert.Equal(
            "12",
            httpAuthenticationService.SignedInPrincipal?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void RegisterGet_ShouldReturnViewWithEmptyModel()
    {
        AccountController controller = CreateController(
            new FakeAuthenticationService());

        IActionResult result = controller.Register();

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<RegisterViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task RegisterPost_WithInvalidModel_ShouldReturnViewAndNotCallService()
    {
        FakeAuthenticationService authenticationService = new FakeAuthenticationService();
        FakeAccountRegistrationService registrationService = new();
        AccountController controller = CreateController(
            authenticationService,
            registrationService);
        RegisterViewModel model = new RegisterViewModel();
        controller.ModelState.AddModelError("Email", "Adresa de e-mail este obligatorie.");

        IActionResult result = await controller.Register(model);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.False(registrationService.RegisterWasCalled);
        Assert.False(authenticationService.RegisterWasCalled);
    }

    [Fact]
    public async Task RegisterPost_WhenRegisterSucceeds_ShouldRedirectWithoutSigningIn()
    {
        FakeAuthenticationService authenticationService = new();
        FakeAccountRegistrationService registrationService = new()
        {
            Result = new AccountRegistrationServiceResult(
                AccountRegistrationServiceResultCode.Success)
        };
        TestHttpAuthenticationService httpAuthenticationService =
            new TestHttpAuthenticationService();
        AccountController controller = CreateController(
            authenticationService,
            registrationService,
            httpAuthenticationService: httpAuthenticationService);
        RegisterViewModel model = new RegisterViewModel
        {
            Email = "Anca@Example.com",
            Password = "secret-password",
            ConfirmPassword = "secret-password"
        };

        IActionResult result = await controller.Register(model);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.CheckEmail), redirectResult.ActionName);
        Assert.Null(redirectResult.ControllerName);
        Assert.True(registrationService.RegisterWasCalled);
        Assert.Equal("Anca@Example.com", registrationService.Input?.Email);
        Assert.Equal("secret-password", registrationService.Input?.Password);
        Assert.Equal("secret-password", registrationService.Input?.PasswordConfirmation);
        Assert.False(authenticationService.RegisterWasCalled);
        Assert.False(httpAuthenticationService.SignInWasCalled);
    }

    [Theory]
    [InlineData(AccountRegistrationServiceResultCode.DuplicateEmail)]
    [InlineData(AccountRegistrationServiceResultCode.DeliveryFailed)]
    public async Task RegisterPost_WithNonDisclosableOutcome_ShouldUseSameResponse(
        AccountRegistrationServiceResultCode resultCode)
    {
        FakeAccountRegistrationService registrationService = new()
        {
            Result = new AccountRegistrationServiceResult(resultCode)
        };
        TestHttpAuthenticationService httpAuthenticationService =
            new TestHttpAuthenticationService();
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            registrationService,
            httpAuthenticationService);

        IActionResult result = await controller.Register(CreateRegisterModel());

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.CheckEmail), redirectResult.ActionName);
        Assert.Null(redirectResult.ControllerName);
        Assert.False(httpAuthenticationService.SignInWasCalled);
    }

    [Fact]
    public async Task RegisterPost_WhenServiceRejectsInput_ShouldReturnFieldError()
    {
        FakeAccountRegistrationService registrationService = new()
        {
            Result = new AccountRegistrationServiceResult(
                AccountRegistrationServiceResultCode.InvalidEmail)
        };
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            registrationService);
        RegisterViewModel model = CreateRegisterModel();

        IActionResult result = await controller.Register(model);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.Contains(
            controller.ModelState[nameof(RegisterViewModel.Email)]!.Errors,
            error => error.ErrorMessage == "Introdu o adresă de e-mail validă.");
    }

    [Fact]
    public void CheckEmail_ShouldReturnViewWithoutAddressModel()
    {
        AccountController controller = CreateController(
            new FakeAuthenticationService());

        IActionResult result = controller.CheckEmail();

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
    }

    [Fact]
    public void ResendVerificationGet_ShouldReturnEmptyModel()
    {
        AccountController controller = CreateController(
            new FakeAuthenticationService());

        IActionResult result = controller.ResendVerification();

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ResendVerificationViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task ResendVerificationPost_WithInvalidModel_ShouldNotCallService()
    {
        FakeAccountRegistrationService registrationService = new();
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            registrationService);
        ResendVerificationViewModel model = new();
        controller.ModelState.AddModelError(
            nameof(ResendVerificationViewModel.Email),
            "Adresa de e-mail este obligatorie.");

        IActionResult result = await controller.ResendVerification(model);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.False(registrationService.ResendWasCalled);
    }

    [Theory]
    [InlineData(AccountRegistrationResendServiceResultCode.Sent)]
    [InlineData(AccountRegistrationResendServiceResultCode.NotEligible)]
    [InlineData(AccountRegistrationResendServiceResultCode.Cooldown)]
    [InlineData(AccountRegistrationResendServiceResultCode.DailyLimit)]
    [InlineData(AccountRegistrationResendServiceResultCode.DeliveryFailed)]
    [InlineData(AccountRegistrationResendServiceResultCode.InvalidEmail)]
    public async Task ResendVerificationPost_ShouldReturnTheSameGenericResponse(
        AccountRegistrationResendServiceResultCode resultCode)
    {
        FakeAccountRegistrationService registrationService = new()
        {
            ResendResult = new AccountRegistrationResendServiceResult(resultCode)
        };
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            registrationService);
        ResendVerificationViewModel model = new()
        {
            Email = "anca@example.com"
        };

        IActionResult result = await controller.ResendVerification(model);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(
            nameof(AccountController.ResendVerificationSent),
            redirectResult.ActionName);
        Assert.Null(redirectResult.RouteValues);
        Assert.True(registrationService.ResendWasCalled);
        Assert.Equal("anca@example.com", registrationService.ResendInput?.Email);
    }

    [Fact]
    public void ResendVerificationSent_ShouldReturnViewWithoutAddressModel()
    {
        AccountController controller = CreateController(
            new FakeAuthenticationService());

        IActionResult result = controller.ResendVerificationSent();

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
    }

    [Fact]
    public void VerifyEmailGet_WithValidToken_ShouldRenderReviewWithoutConfirming()
    {
        FakeAccountRegistrationService registrationService = new();
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            registrationService);

        IActionResult result = controller.VerifyEmail("raw-token");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        VerifyEmailViewModel model =
            Assert.IsType<VerifyEmailViewModel>(viewResult.Model);
        Assert.Equal("raw-token", model.Token);
        Assert.True(registrationService.ReviewWasCalled);
        Assert.False(registrationService.ConfirmWasCalled);
    }

    [Theory]
    [InlineData(
        AccountEmailVerificationReviewResultCode.InvalidToken,
        nameof(AccountController.EmailVerificationInvalid))]
    [InlineData(
        AccountEmailVerificationReviewResultCode.ExpiredToken,
        nameof(AccountController.EmailVerificationExpired))]
    public void VerifyEmailGet_WithUnusableToken_ShouldRedirectWithoutTokenDetails(
        AccountEmailVerificationReviewResultCode resultCode,
        string expectedAction)
    {
        FakeAccountRegistrationService registrationService = new()
        {
            ReviewResult = new AccountEmailVerificationReviewResult(resultCode)
        };
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            registrationService);

        IActionResult result = controller.VerifyEmail("raw-token");

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(expectedAction, redirectResult.ActionName);
        Assert.Null(redirectResult.RouteValues);
        Assert.False(registrationService.ConfirmWasCalled);
    }

    [Fact]
    public void VerifyEmailPost_WhenConfirmationSucceeds_ShouldRedirectWithoutSigningIn()
    {
        FakeAccountRegistrationService registrationService = new();
        TestHttpAuthenticationService httpAuthenticationService =
            new TestHttpAuthenticationService();
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            registrationService,
            httpAuthenticationService);

        IActionResult result = controller.VerifyEmail(
            new VerifyEmailViewModel { Token = "raw-token" });

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.EmailVerified), redirectResult.ActionName);
        Assert.Null(redirectResult.RouteValues);
        Assert.True(registrationService.ConfirmWasCalled);
        Assert.Equal("raw-token", registrationService.ConfirmedToken);
        Assert.False(httpAuthenticationService.SignInWasCalled);
    }

    [Theory]
    [InlineData(
        AccountEmailVerificationResultCode.InvalidToken,
        nameof(AccountController.EmailVerificationInvalid))]
    [InlineData(
        AccountEmailVerificationResultCode.ExpiredToken,
        nameof(AccountController.EmailVerificationExpired))]
    public void VerifyEmailPost_WhenConfirmationFails_ShouldRedirectWithoutTokenDetails(
        AccountEmailVerificationResultCode resultCode,
        string expectedAction)
    {
        FakeAccountRegistrationService registrationService = new()
        {
            ConfirmationResult = new AccountEmailVerificationResult(resultCode)
        };
        AccountController controller = CreateController(
            new FakeAuthenticationService(),
            registrationService);

        IActionResult result = controller.VerifyEmail(
            new VerifyEmailViewModel { Token = "raw-token" });

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(expectedAction, redirectResult.ActionName);
        Assert.Null(redirectResult.RouteValues);
    }

    [Fact]
    public async Task Logout_ShouldSignOutAndRedirectToLogin()
    {
        FakeAuthenticationService authenticationService = new FakeAuthenticationService();
        TestHttpAuthenticationService httpAuthenticationService =
            new TestHttpAuthenticationService();
        AccountController controller = CreateController(
            authenticationService,
            httpAuthenticationService: httpAuthenticationService);

        IActionResult result = await controller.Logout();

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.Login), redirectResult.ActionName);
        Assert.True(authenticationService.LogoutWasCalled);
        Assert.True(httpAuthenticationService.SignOutWasCalled);
        Assert.Equal(
            CookieAuthenticationDefaults.AuthenticationScheme,
            httpAuthenticationService.SignOutScheme);
    }

    private static LoginViewModel CreateLoginModel()
    {
        return new LoginViewModel
        {
            Username = "anca",
            Password = "secret-password"
        };
    }

    private static RegisterViewModel CreateRegisterModel()
    {
        return new RegisterViewModel
        {
            Email = "anca@example.com",
            Password = "secret-password",
            ConfirmPassword = "secret-password"
        };
    }

    private static AuthenticationResult CreateSuccessfulAuthenticationResult()
    {
        return new AuthenticationResult
        {
            IsSuccess = true,
            Message = "Login successful.",
            User = new User
            {
                Id = 12,
                Email = "anca@anca.com"
            }
        };
    }

    private static ClaimsPrincipal CreateAuthenticatedUser()
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "12"),
                new Claim(ClaimTypes.Name, "anca")
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }

    private static AccountController CreateController(
        FakeAuthenticationService authenticationService,
        FakeAccountRegistrationService? registrationService = null,
        TestHttpAuthenticationService? httpAuthenticationService = null,
        ClaimsPrincipal? user = null)
    {
        ServiceCollection services = new ServiceCollection();
        httpAuthenticationService ??= new TestHttpAuthenticationService();
        services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationService>(
            httpAuthenticationService);

        DefaultHttpContext httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = user ?? new ClaimsPrincipal()
        };
        AccountController controller = new AccountController(
            authenticationService,
            registrationService ?? new FakeAccountRegistrationService(),
            NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(
                httpContext,
                new TestTempDataProvider()),
            Url = new TestUrlHelper()
        };

        return controller;
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }

    private sealed class TestUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new ActionContext();

        public string? Action(UrlActionContext actionContext)
        {
            return null;
        }

        public string? Content(string? contentPath)
        {
            return contentPath;
        }

        public bool IsLocalUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(url)
                && url.StartsWith("/", StringComparison.Ordinal)
                && !url.StartsWith("//", StringComparison.Ordinal);
        }

        public string? Link(string? routeName, object? values)
        {
            return null;
        }

        public string? RouteUrl(UrlRouteContext routeContext)
        {
            return null;
        }
    }

    private sealed class TestHttpAuthenticationService
        : Microsoft.AspNetCore.Authentication.IAuthenticationService
    {
        public bool SignInWasCalled { get; private set; }

        public bool SignOutWasCalled { get; private set; }

        public string? SignInScheme { get; private set; }

        public string? SignOutScheme { get; private set; }

        public System.Security.Claims.ClaimsPrincipal? SignedInPrincipal { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(
            HttpContext context,
            string? scheme)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            System.Security.Claims.ClaimsPrincipal principal,
            AuthenticationProperties? properties)
        {
            SignInWasCalled = true;
            SignInScheme = scheme;
            SignedInPrincipal = principal;

            return Task.CompletedTask;
        }

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
}
