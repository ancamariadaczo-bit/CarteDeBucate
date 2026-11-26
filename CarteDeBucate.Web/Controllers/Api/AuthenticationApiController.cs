using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CarteDeBucate.Web.Controllers.Api;

[ApiController]
[Route("api/authentication")]
public class AuthenticationApiController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly IExtensionAuthenticationCodeService _extensionAuthenticationCodeService;
    private readonly IUserRepository _userRepository;

    public AuthenticationApiController(
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        IExtensionAuthenticationCodeService extensionAuthenticationCodeService,
        IUserRepository userRepository)
    {
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
        _extensionAuthenticationCodeService = extensionAuthenticationCodeService;
        _userRepository = userRepository;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        string? email = User.FindFirstValue(ClaimTypes.Email);

        return Ok(new
        {
            userId,
            email
        });
    }

    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpGet("extension-complete")]
    public IActionResult CompleteExtensionAuthentication()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out int parsedUserId))
        {
            return Unauthorized();
        }

        string? securityVersion = User.FindFirstValue(AuthenticationClaimTypes.SecurityVersion);

        if (!long.TryParse(
                securityVersion,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long parsedSecurityVersion)
            || parsedSecurityVersion <= 0)
        {
            return Unauthorized();
        }

        User? user = _userRepository.GetById(parsedUserId);

        if (user is null || user.EmailVerifiedAtUtc is null)
        {
            return Unauthorized();
        }

        string code = _extensionAuthenticationCodeService.CreateCode(user.Id, parsedSecurityVersion);

        string redirectUrl =
            _configuration["ChromeExtension:AuthenticationRedirectUrl"]
            ?? throw new InvalidOperationException(
                "Chrome extension authentication redirect URL is not configured.");

        return Redirect($"{redirectUrl}?code={Uri.EscapeDataString(code)}");
    }

    [AllowAnonymous]
    [HttpPost("exchange-code")]
    public IActionResult ExchangeCode(
    [FromBody] ExchangeAuthenticationCodeRequest? request)
    {
        string? code = request?.Code;

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new
            {
                message = "The authentication code is invalid or has expired."
            });
        }

        ExtensionAuthenticationCodeData? codeData =
            _extensionAuthenticationCodeService.ConsumeCode(code);

        if (codeData == null)
        {
            return BadRequest(new
            {
                message = "The authentication code is invalid or has expired."
            });
        }

        User? user = _userRepository.GetById(codeData.UserId);

        if (user is null
            || user.EmailVerifiedAtUtc is null
            || user.SecurityVersion != codeData.SecurityVersion)
        {
            return BadRequest(new
            {
                message = "The authentication code is invalid or has expired."
            });
        }

        string accessToken = _jwtTokenService.CreateToken(
            user.Id.ToString(CultureInfo.InvariantCulture),
            user.Email,
            user.SecurityVersion);

        return Ok(new
        {
            accessToken
        });
    }
}
