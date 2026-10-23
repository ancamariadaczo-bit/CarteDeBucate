using System.Reflection;
using System.Security.Claims;
using CarteDeBucate.Web.Controllers.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

public class AuthenticationApiControllerTests
{
    private const string AuthenticationRedirectUrl =
        "https://extension-id.chromiumapp.org/authentication-callback";

    [Fact]
    public void GetCurrentUser_ShouldReturnUserIdAndUsernameFromClaims()
    {
        AuthenticationApiController controller = CreateController(
            user: CreateUser("user-42", "chef"));

        IActionResult result = controller.GetCurrentUser();

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(
            "user-42",
            GetResponseProperty<string>(okResult.Value, "userId"));
        Assert.Equal(
            "chef",
            GetResponseProperty<string>(okResult.Value, "username"));
    }

    [Theory]
    [InlineData(null, "chef")]
    [InlineData("user-42", null)]
    public void CompleteExtensionAuthentication_WhenARequiredClaimIsMissing_ShouldReturnUnauthorized(
        string? userId,
        string? username)
    {
        FakeExtensionAuthenticationCodeService codeService = new();
        AuthenticationApiController controller = CreateController(
            codeService: codeService,
            user: CreateUser(userId, username));

        IActionResult result = controller.CompleteExtensionAuthentication();

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(codeService.CreateCodeCalls);
    }

    [Fact]
    public void CompleteExtensionAuthentication_ShouldCreateCodeAndRedirectWithEncodedCode()
    {
        const string authenticationCode = "code with spaces/+?=";
        FakeExtensionAuthenticationCodeService codeService = new()
        {
            CodeToCreate = authenticationCode
        };
        AuthenticationApiController controller = CreateController(
            codeService: codeService,
            user: CreateUser("user-42", "chef"));

        IActionResult result = controller.CompleteExtensionAuthentication();

        RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(
            ("user-42", "chef"),
            Assert.Single(codeService.CreateCodeCalls));
        Assert.Equal(
            $"{AuthenticationRedirectUrl}?code={Uri.EscapeDataString(authenticationCode)}",
            redirectResult.Url);
        Assert.DoesNotContain(authenticationCode, redirectResult.Url);
    }

    [Fact]
    public void ExchangeCode_WithValidCode_ShouldCreateAndReturnJwt()
    {
        FakeJwtTokenService jwtTokenService = new()
        {
            TokenToReturn = "issued-jwt"
        };
        FakeExtensionAuthenticationCodeService codeService = new();
        codeService.AddCode(
            "valid-code",
            new ExtensionAuthenticationCodeData
            {
                UserId = "user-42",
                Username = "chef",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1)
            });
        AuthenticationApiController controller = CreateController(
            jwtTokenService,
            codeService);

        IActionResult result = controller.ExchangeCode(
            new ExchangeAuthenticationCodeRequest
            {
                Code = "valid-code"
            });

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(
            "issued-jwt",
            GetResponseProperty<string>(okResult.Value, "accessToken"));
        Assert.Equal(
            ("user-42", "chef"),
            Assert.Single(jwtTokenService.CreateTokenCalls));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("unknown-code", false)]
    [InlineData("expired-code", false)]
    public void ExchangeCode_WithInvalidRequest_ShouldReturnBadRequestWithoutCreatingJwt(
        string? code,
        bool bodyIsMissing)
    {
        FakeJwtTokenService jwtTokenService = new();
        FakeExtensionAuthenticationCodeService codeService = new();
        AuthenticationApiController controller = CreateController(
            jwtTokenService,
            codeService);
        ExchangeAuthenticationCodeRequest? request = bodyIsMissing
            ? null
            : new ExchangeAuthenticationCodeRequest
            {
                Code = code
            };

        IActionResult result = controller.ExchangeCode(request);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(jwtTokenService.CreateTokenCalls);

        if (string.IsNullOrWhiteSpace(code))
        {
            Assert.Empty(codeService.ConsumeCodeCalls);
        }
        else
        {
            Assert.Equal([code], codeService.ConsumeCodeCalls);
        }
    }

    [Fact]
    public void ExchangeCode_ShouldConsumeAValidCodeOnlyOnce()
    {
        FakeJwtTokenService jwtTokenService = new();
        FakeExtensionAuthenticationCodeService codeService = new();
        codeService.AddCode(
            "single-use-code",
            new ExtensionAuthenticationCodeData
            {
                UserId = "user-42",
                Username = "chef",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1)
            });
        AuthenticationApiController controller = CreateController(
            jwtTokenService,
            codeService);
        ExchangeAuthenticationCodeRequest request = new()
        {
            Code = "single-use-code"
        };

        IActionResult firstResult = controller.ExchangeCode(request);
        IActionResult secondResult = controller.ExchangeCode(request);

        Assert.IsType<OkObjectResult>(firstResult);
        Assert.IsType<BadRequestObjectResult>(secondResult);
        Assert.Single(jwtTokenService.CreateTokenCalls);
        Assert.Equal(
            ["single-use-code", "single-use-code"],
            codeService.ConsumeCodeCalls);
    }

    private static AuthenticationApiController CreateController(
        FakeJwtTokenService? jwtTokenService = null,
        FakeExtensionAuthenticationCodeService? codeService = null,
        ClaimsPrincipal? user = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChromeExtension:AuthenticationRedirectUrl"] =
                    AuthenticationRedirectUrl
            })
            .Build();
        AuthenticationApiController controller = new(
            jwtTokenService ?? new FakeJwtTokenService(),
            configuration,
            codeService ?? new FakeExtensionAuthenticationCodeService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        return controller;
    }

    private static ClaimsPrincipal CreateUser(string? userId, string? username)
    {
        List<Claim> claims = [];

        if (userId != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (username != null)
        {
            claims.Add(new Claim(ClaimTypes.Name, username));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static T GetResponseProperty<T>(object? response, string propertyName)
    {
        Assert.NotNull(response);

        PropertyInfo? property = response.GetType().GetProperty(propertyName);

        Assert.NotNull(property);

        return Assert.IsType<T>(property.GetValue(response));
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string TokenToReturn { get; init; } = "test-jwt";

        public List<(string UserId, string Username)> CreateTokenCalls { get; } = [];

        public string CreateToken(string userId, string username)
        {
            CreateTokenCalls.Add((userId, username));

            return TokenToReturn;
        }
    }

    private sealed class FakeExtensionAuthenticationCodeService
        : IExtensionAuthenticationCodeService
    {
        private readonly Dictionary<string, ExtensionAuthenticationCodeData> _codes = [];

        public string CodeToCreate { get; init; } = "created-code";

        public List<(string UserId, string Username)> CreateCodeCalls { get; } = [];

        public List<string> ConsumeCodeCalls { get; } = [];

        public string CreateCode(string userId, string username)
        {
            CreateCodeCalls.Add((userId, username));

            return CodeToCreate;
        }

        public ExtensionAuthenticationCodeData? ConsumeCode(string code)
        {
            ConsumeCodeCalls.Add(code);

            return _codes.Remove(code, out ExtensionAuthenticationCodeData? data)
                ? data
                : null;
        }

        public void AddCode(
            string code,
            ExtensionAuthenticationCodeData codeData)
        {
            _codes.Add(code, codeData);
        }
    }
}
