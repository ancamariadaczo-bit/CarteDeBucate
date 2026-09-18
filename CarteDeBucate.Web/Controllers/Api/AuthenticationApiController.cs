using System.Security.Claims;
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

    public AuthenticationApiController(
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        IExtensionAuthenticationCodeService extensionAuthenticationCodeService)
    {
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
        _extensionAuthenticationCodeService = extensionAuthenticationCodeService;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        string? userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        string? username =
            User.FindFirstValue(ClaimTypes.Name);

        return Ok(new
        {
            userId,
            username
        });
    }

    [Authorize(
        AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpGet("extension-complete")]
    public IActionResult CompleteExtensionAuthentication()
    {
        string? userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        string? username =
            User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        string code =
            _extensionAuthenticationCodeService.CreateCode(
                userId,
                username);

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

        string accessToken =
            _jwtTokenService.CreateToken(
                codeData.UserId,
                codeData.Username);

        return Ok(new
        {
            accessToken
        });
    }
}
