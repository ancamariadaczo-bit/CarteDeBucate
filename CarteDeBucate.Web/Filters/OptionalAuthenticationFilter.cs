using CarteDeBucate.Web.Configuration;
using CarteDeBucate.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CarteDeBucate.Web.Filters;

public class OptionalAuthenticationFilter : IAuthorizationFilter
{
    private readonly WebAppSettings _settings;

    public OptionalAuthenticationFilter(WebAppSettings settings)
    {
        _settings = settings;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!_settings.AuthenticationEnabled)
        {
            return;
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            return;
        }

        string returnUrl =
            $"{context.HttpContext.Request.PathBase}{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}";

        context.Result = new RedirectToActionResult(
            nameof(AccountController.Login),
            "Account",
            new { returnUrl });
    }
}
