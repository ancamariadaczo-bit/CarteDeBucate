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
    public void GetCurrentUser_ShouldReturnUserIdAndEmailFromClaims()
    {
        AuthenticationApiController controller = CreateController(
            user: CreatePrincipal("42", "anca@example.com"));

        IActionResult result = controller.GetCurrentUser();

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("42", GetResponseProperty<string>(okResult.Value, "userId"));
        Assert.Equal(
            "anca@example.com",
            GetResponseProperty<string>(okResult.Value, "email"));
        Assert.Null(okResult.Value!.GetType().GetProperty("username"));
    }

    [Fact]
    public void CompleteExtensionAuthentication_WithInvalidUserIdClaim_ShouldReturnUnauthorized()
    {
        FakeExtensionAuthenticationCodeService codeService = new();
        AuthenticationApiController controller = CreateController(
            codeService: codeService,
            user: CreatePrincipal("not-an-id", "anca@example.com"));

        IActionResult result = controller.CompleteExtensionAuthentication();

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(codeService.CreateCodeCalls);
    }

    [Fact]
    public void CompleteExtensionAuthentication_WhenCurrentUserIsVerified_ShouldCreateCode()
    {
        const string authenticationCode = "code with spaces/+?=";
        FakeExtensionAuthenticationCodeService codeService = new()
        {
            CodeToCreate = authenticationCode
        };
        AuthenticationApiController controller = CreateController(
            codeService: codeService,
            userRepository: CreateUserRepository(CreateVerifiedUser(42)),
            user: CreatePrincipal("42", "stale@example.com"));

        IActionResult result = controller.CompleteExtensionAuthentication();

        RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal((42, 1L), Assert.Single(codeService.CreateCodeCalls));
        Assert.Equal(
            $"{AuthenticationRedirectUrl}?code={Uri.EscapeDataString(authenticationCode)}",
            redirectResult.Url);
        Assert.DoesNotContain(authenticationCode, redirectResult.Url);
    }

    [Fact]
    public void CompleteExtensionAuthentication_WithoutSecurityVersionClaim_ShouldReturnUnauthorized()
    {
        FakeExtensionAuthenticationCodeService codeService = new();
        AuthenticationApiController controller = CreateController(
            codeService: codeService,
            userRepository: CreateUserRepository(CreateVerifiedUser(42)),
            user: CreatePrincipal("42", "anca@example.com", securityVersion: null));

        IActionResult result = controller.CompleteExtensionAuthentication();

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(codeService.CreateCodeCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompleteExtensionAuthentication_WhenCurrentUserIsMissingOrUnverified_ShouldReturnUnauthorized(
        bool userExists)
    {
        FakeExtensionAuthenticationCodeService codeService = new();
        FakeUserRepository userRepository = userExists
            ? CreateUserRepository(CreateVerifiedUser(42, emailVerified: false))
            : CreateUserRepository();
        AuthenticationApiController controller = CreateController(
            codeService: codeService,
            userRepository: userRepository,
            user: CreatePrincipal("42", "stale@example.com"));

        IActionResult result = controller.CompleteExtensionAuthentication();

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(codeService.CreateCodeCalls);
    }

    [Fact]
    public void ExchangeCode_WithVerifiedCurrentUser_ShouldCreateAndReturnJwt()
    {
        FakeJwtTokenService jwtTokenService = new()
        {
            TokenToReturn = "issued-jwt"
        };
        FakeExtensionAuthenticationCodeService codeService = new();
        codeService.AddCode("valid-code", CreateCodeData(42));
        AuthenticationApiController controller = CreateController(
            jwtTokenService,
            codeService,
            CreateUserRepository(CreateVerifiedUser(42)));

        IActionResult result = controller.ExchangeCode(
            new ExchangeAuthenticationCodeRequest { Code = "valid-code" });

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(
            "issued-jwt",
            GetResponseProperty<string>(okResult.Value, "accessToken"));
        Assert.Equal(
            ("42", "anca@example.com", 1L),
            Assert.Single(jwtTokenService.CreateTokenCalls));
    }

    [Fact]
    public void ExchangeCode_WhenSecurityVersionIsStale_ShouldConsumeCodeWithoutIssuingJwt()
    {
        FakeJwtTokenService jwtTokenService = new();
        FakeExtensionAuthenticationCodeService codeService = new();
        codeService.AddCode("old-code", CreateCodeData(42, securityVersion: 1));
        AuthenticationApiController controller = CreateController(
            jwtTokenService,
            codeService,
            CreateUserRepository(CreateVerifiedUser(42, securityVersion: 2)));

        IActionResult firstResult = controller.ExchangeCode(
            new ExchangeAuthenticationCodeRequest { Code = "old-code" });
        IActionResult secondResult = controller.ExchangeCode(
            new ExchangeAuthenticationCodeRequest { Code = "old-code" });

        Assert.IsType<BadRequestObjectResult>(firstResult);
        Assert.IsType<BadRequestObjectResult>(secondResult);
        Assert.Empty(jwtTokenService.CreateTokenCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExchangeCode_WhenUserIsMissingOrUnverified_ShouldConsumeCodeWithoutIssuingJwt(
        bool userExists)
    {
        FakeJwtTokenService jwtTokenService = new();
        FakeExtensionAuthenticationCodeService codeService = new();
        codeService.AddCode("valid-code", CreateCodeData(42));
        FakeUserRepository userRepository = userExists
            ? CreateUserRepository(CreateVerifiedUser(42, emailVerified: false))
            : CreateUserRepository();
        AuthenticationApiController controller = CreateController(
            jwtTokenService,
            codeService,
            userRepository);

        IActionResult firstResult = controller.ExchangeCode(
            new ExchangeAuthenticationCodeRequest { Code = "valid-code" });
        IActionResult secondResult = controller.ExchangeCode(
            new ExchangeAuthenticationCodeRequest { Code = "valid-code" });

        Assert.IsType<BadRequestObjectResult>(firstResult);
        Assert.IsType<BadRequestObjectResult>(secondResult);
        Assert.Empty(jwtTokenService.CreateTokenCalls);
        Assert.Equal(["valid-code", "valid-code"], codeService.ConsumeCodeCalls);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("unknown-code", false)]
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
            : new ExchangeAuthenticationCodeRequest { Code = code };

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

    private static AuthenticationApiController CreateController(
        FakeJwtTokenService? jwtTokenService = null,
        FakeExtensionAuthenticationCodeService? codeService = null,
        FakeUserRepository? userRepository = null,
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
            codeService ?? new FakeExtensionAuthenticationCodeService(),
            userRepository ?? CreateUserRepository())
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

    private static FakeUserRepository CreateUserRepository(params User[] users)
    {
        FakeUserRepository repository = new();
        repository.Users.AddRange(users);
        return repository;
    }

    private static User CreateVerifiedUser(
        int id,
        bool emailVerified = true,
        long securityVersion = 1)
    {
        return new User
        {
            Id = id,
            Email = "anca@example.com",
            EmailVerifiedAtUtc = emailVerified ? DateTime.UtcNow : null,
            SecurityVersion = securityVersion
        };
    }

    private static ExtensionAuthenticationCodeData CreateCodeData(
        int userId,
        long securityVersion = 1)
    {
        return new ExtensionAuthenticationCodeData
        {
            UserId = userId,
            SecurityVersion = securityVersion,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1)
        };
    }

    private static ClaimsPrincipal CreatePrincipal(
        string? userId,
        string? email,
        long? securityVersion = 1)
    {
        List<Claim> claims = [];

        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, email));
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        if (securityVersion.HasValue)
        {
            claims.Add(new Claim(
                AuthenticationClaimTypes.SecurityVersion,
                securityVersion.Value.ToString()));
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

        public List<(string UserId, string Email, long SecurityVersion)> CreateTokenCalls { get; } = [];

        public string CreateToken(string userId, string email, long securityVersion)
        {
            CreateTokenCalls.Add((userId, email, securityVersion));
            return TokenToReturn;
        }
    }

    private sealed class FakeExtensionAuthenticationCodeService
        : IExtensionAuthenticationCodeService
    {
        private readonly Dictionary<string, ExtensionAuthenticationCodeData> _codes = [];

        public string CodeToCreate { get; init; } = "created-code";

        public List<(int UserId, long SecurityVersion)> CreateCodeCalls { get; } = [];

        public List<string> ConsumeCodeCalls { get; } = [];

        public string CreateCode(int userId, long securityVersion)
        {
            CreateCodeCalls.Add((userId, securityVersion));
            return CodeToCreate;
        }

        public ExtensionAuthenticationCodeData? ConsumeCode(string code)
        {
            ConsumeCodeCalls.Add(code);
            return _codes.Remove(code, out ExtensionAuthenticationCodeData? data)
                ? data
                : null;
        }

        public void AddCode(string code, ExtensionAuthenticationCodeData codeData)
        {
            _codes.Add(code, codeData);
        }
    }
}
