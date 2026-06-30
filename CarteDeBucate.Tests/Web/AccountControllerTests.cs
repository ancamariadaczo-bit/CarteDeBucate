using CarteDeBucate.Web.Configuration;
using CarteDeBucate.Web.Controllers;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

public class AccountControllerTests
{
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
        AccountController controller = CreateController(authenticationService);
        RegisterViewModel model = new RegisterViewModel();
        controller.ModelState.AddModelError("Username", "Username-ul este obligatoriu.");

        IActionResult result = await controller.Register(model);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.False(authenticationService.RegisterWasCalled);
    }

    [Fact]
    public async Task RegisterPost_WhenRegisterSucceeds_ShouldSignInAndRedirectToRecipes()
    {
        FakeAuthenticationService authenticationService = new FakeAuthenticationService
        {
            RegisterResult = CreateSuccessfulAuthenticationResult()
        };
        TestHttpAuthenticationService httpAuthenticationService =
            new TestHttpAuthenticationService();
        AccountController controller = CreateController(
            authenticationService,
            httpAuthenticationService: httpAuthenticationService);
        RegisterViewModel model = new RegisterViewModel
        {
            Username = "anca",
            Password = "secret-password",
            ConfirmPassword = "secret-password"
        };

        IActionResult result = await controller.Register(model);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.Equal("Recipes", redirectResult.ControllerName);
        Assert.True(authenticationService.RegisterWasCalled);
        Assert.True(httpAuthenticationService.SignInWasCalled);
    }

    [Fact]
    public async Task Logout_WhenAuthenticationIsEnabled_ShouldSignOutAndRedirectToLogin()
    {
        FakeAuthenticationService authenticationService = new FakeAuthenticationService();
        TestHttpAuthenticationService httpAuthenticationService =
            new TestHttpAuthenticationService();
        AccountController controller = CreateController(
            authenticationService,
            new WebAppSettings { AuthenticationEnabled = true },
            httpAuthenticationService);

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

    private static AuthenticationResult CreateSuccessfulAuthenticationResult()
    {
        return new AuthenticationResult
        {
            IsSuccess = true,
            Message = "Login successful.",
            User = new User
            {
                Id = 12,
                Username = "anca"
            }
        };
    }

    private static AccountController CreateController(
        FakeAuthenticationService authenticationService,
        WebAppSettings? settings = null,
        TestHttpAuthenticationService? httpAuthenticationService = null)
    {
        ServiceCollection services = new ServiceCollection();
        httpAuthenticationService ??= new TestHttpAuthenticationService();
        services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationService>(
            httpAuthenticationService);

        DefaultHttpContext httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        AccountController controller = new AccountController(
            authenticationService,
            settings ?? new WebAppSettings())
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
