using System.Security.Claims;

namespace CarteDeBucate.Web.Services.Authentication;

public interface IAuthenticatedSessionValidator
{
    bool IsValid(ClaimsPrincipal? principal);
}
