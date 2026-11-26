using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CarteDeBucate.Web.Services.Authentication;

public sealed class SecurityVersionCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly IAuthenticatedSessionValidator _sessionValidator;

    public SecurityVersionCookieAuthenticationEvents(IAuthenticatedSessionValidator sessionValidator)
    {
        ArgumentNullException.ThrowIfNull(sessionValidator);

        _sessionValidator = sessionValidator;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (_sessionValidator.IsValid(context.Principal))
        {
            return;
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}
