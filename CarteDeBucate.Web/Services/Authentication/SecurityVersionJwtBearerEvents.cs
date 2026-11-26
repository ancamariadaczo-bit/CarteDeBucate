using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CarteDeBucate.Web.Services.Authentication;

public sealed class SecurityVersionJwtBearerEvents : JwtBearerEvents
{
    private readonly IAuthenticatedSessionValidator _sessionValidator;

    public SecurityVersionJwtBearerEvents(IAuthenticatedSessionValidator sessionValidator)
    {
        ArgumentNullException.ThrowIfNull(sessionValidator);

        _sessionValidator = sessionValidator;
    }

    public override Task TokenValidated(TokenValidatedContext context)
    {
        if (!_sessionValidator.IsValid(context.Principal))
        {
            context.Fail("The authentication session is invalid.");
        }

        return Task.CompletedTask;
    }
}
