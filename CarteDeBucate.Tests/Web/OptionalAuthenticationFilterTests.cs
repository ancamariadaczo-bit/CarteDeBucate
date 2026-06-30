using System.Security.Claims;
using CarteDeBucate.Web.Configuration;
using CarteDeBucate.Web.Controllers;
using CarteDeBucate.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

public class OptionalAuthenticationFilterTests
{
    [Fact]
    public void OnAuthorization_WhenAuthenticationIsDisabled_ShouldAllowRequest()
    {
        OptionalAuthenticationFilter filter = new OptionalAuthenticationFilter(
            new WebAppSettings { AuthenticationEnabled = false });
        AuthorizationFilterContext context = CreateAuthorizationContext();

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenAuthenticationIsEnabledAndUserIsAuthenticated_ShouldAllowRequest()
    {
        OptionalAuthenticationFilter filter = new OptionalAuthenticationFilter(
            new WebAppSettings { AuthenticationEnabled = true });
        AuthorizationFilterContext context = CreateAuthorizationContext(
            isAuthenticated: true);

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenAuthenticationIsEnabledAndUserIsAnonymous_ShouldRedirectToLogin()
    {
        OptionalAuthenticationFilter filter = new OptionalAuthenticationFilter(
            new WebAppSettings { AuthenticationEnabled = true });
        AuthorizationFilterContext context = CreateAuthorizationContext(
            path: "/Recipes/Details/12",
            queryString: "?returnTo=Index");

        filter.OnAuthorization(context);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal(nameof(AccountController.Login), redirectResult.ActionName);
        Assert.Equal("Account", redirectResult.ControllerName);
        Assert.Equal(
            "/Recipes/Details/12?returnTo=Index",
            redirectResult.RouteValues?["returnUrl"]);
    }

    [Fact]
    public void RecipesController_ShouldUseOptionalAuthenticationFilter()
    {
        AssertControllerUsesOptionalAuthenticationFilter(typeof(RecipesController));
    }

    [Fact]
    public void RecipeBackupsController_ShouldUseOptionalAuthenticationFilter()
    {
        AssertControllerUsesOptionalAuthenticationFilter(typeof(RecipeBackupsController));
    }

    private static AuthorizationFilterContext CreateAuthorizationContext(
        bool isAuthenticated = false,
        string path = "/Recipes",
        string queryString = "")
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.Request.QueryString = new QueryString(queryString);

        if (isAuthenticated)
        {
            Claim[] claims =
            {
                new Claim(ClaimTypes.NameIdentifier, "12"),
                new Claim(ClaimTypes.Name, "anca")
            };
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "Test"));
        }

        ActionContext actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());
    }

    private static void AssertControllerUsesOptionalAuthenticationFilter(Type controllerType)
    {
        ServiceFilterAttribute? filterAttribute = controllerType
            .GetCustomAttributes(typeof(ServiceFilterAttribute), inherit: false)
            .Cast<ServiceFilterAttribute>()
            .SingleOrDefault(attribute =>
                attribute.ServiceType == typeof(OptionalAuthenticationFilter));

        Assert.NotNull(filterAttribute);
    }
}
