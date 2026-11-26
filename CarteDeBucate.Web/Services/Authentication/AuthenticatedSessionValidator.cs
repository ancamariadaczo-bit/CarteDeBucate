using System.Globalization;
using System.Security.Claims;

namespace CarteDeBucate.Web.Services.Authentication;

public sealed class AuthenticatedSessionValidator : IAuthenticatedSessionValidator
{
    private readonly IUserRepository _userRepository;

    public AuthenticatedSessionValidator(IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);

        _userRepository = userRepository;
    }

    public bool IsValid(ClaimsPrincipal? principal)
    {
        if (principal is null
            || !TryGetSinglePositiveIntClaim(
                principal,
                ClaimTypes.NameIdentifier,
                out int userId)
            || !TryGetSinglePositiveLongClaim(
                principal,
                AuthenticationClaimTypes.SecurityVersion,
                out long claimedSecurityVersion))
        {
            return false;
        }

        long? persistedSecurityVersion = _userRepository.GetSecurityVersion(userId);

        return persistedSecurityVersion == claimedSecurityVersion;
    }

    private static bool TryGetSinglePositiveIntClaim(
        ClaimsPrincipal principal,
        string claimType,
        out int value)
    {
        value = 0;
        Claim[] claims = principal.FindAll(claimType).ToArray();

        return claims.Length == 1
            && int.TryParse(
                claims[0].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value)
            && value > 0;
    }

    private static bool TryGetSinglePositiveLongClaim(
        ClaimsPrincipal principal,
        string claimType,
        out long value)
    {
        value = 0;
        Claim[] claims = principal.FindAll(claimType).ToArray();

        return claims.Length == 1
            && long.TryParse(
                claims[0].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value)
            && value > 0;
    }
}
