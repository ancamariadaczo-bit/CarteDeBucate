using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenServiceTests
{
    private const string TestIssuer = "CarteDeBucate.Tests";
    private const string TestAudience = "CarteDeBucate.ChromeExtension.Tests";
    private const int TestExpirationMinutes = 37;
    private const string TestKey =
        "test-only-signing-key-that-is-long-enough-for-hs256-1234567890";

    [Fact]
    public void CreateToken_ShouldUseTheConfiguredIssuerAudienceAndValidSignature()
    {
        JwtTokenService service = CreateService();
        string token = service.CreateToken("user-42", "chef@example.com", 7);
        JwtSecurityTokenHandler tokenHandler = new();

        ClaimsPrincipal principal = tokenHandler.ValidateToken(
            token,
            CreateValidationParameters(),
            out SecurityToken validatedToken);

        JwtSecurityToken jwtToken = Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal(TestIssuer, jwtToken.Issuer);
        Assert.Contains(TestAudience, jwtToken.Audiences);
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwtToken.Header.Alg);
        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
    }

    [Fact]
    public void CreateToken_ShouldContainUserIdAndEmailClaims()
    {
        JwtTokenService service = CreateService();
        string token = service.CreateToken("user-42", "anca@example.com", 7);
        JwtSecurityToken jwtToken = new JwtSecurityTokenHandler()
            .ReadJwtToken(token);

        Assert.Contains(
            jwtToken.Claims,
            claim => claim.Type == JwtRegisteredClaimNames.Sub &&
                claim.Value == "user-42");
        Assert.Contains(
            jwtToken.Claims,
            claim => claim.Type == ClaimTypes.NameIdentifier &&
                claim.Value == "user-42");
        Assert.Contains(
            jwtToken.Claims,
            claim => claim.Type == ClaimTypes.Name &&
                claim.Value == "anca@example.com");
        Assert.Contains(
            jwtToken.Claims,
            claim => claim.Type == ClaimTypes.Email &&
                claim.Value == "anca@example.com");
        Claim securityVersionClaim = Assert.Single(
            jwtToken.Claims.Where(
                claim => claim.Type == AuthenticationClaimTypes.SecurityVersion));
        Assert.Equal("7", securityVersionClaim.Value);
    }

    [Fact]
    public void CreateToken_ShouldUseTheConfiguredExpiration()
    {
        JwtTokenService service = CreateService();
        DateTime beforeCreation = DateTime.UtcNow;

        string token = service.CreateToken("user-42", "chef@example.com", 7);

        DateTime afterCreation = DateTime.UtcNow;
        JwtSecurityToken jwtToken = new JwtSecurityTokenHandler()
            .ReadJwtToken(token);
        Assert.InRange(
            jwtToken.ValidTo,
            beforeCreation.AddMinutes(TestExpirationMinutes).AddSeconds(-1),
            afterCreation.AddMinutes(TestExpirationMinutes).AddSeconds(1));
    }

    private static JwtTokenService CreateService()
    {
        Dictionary<string, string?> settings = new()
        {
            ["Jwt:Key"] = TestKey,
            ["Jwt:Issuer"] = TestIssuer,
            ["Jwt:Audience"] = TestAudience,
            ["Jwt:ExpirationMinutes"] = TestExpirationMinutes.ToString()
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new JwtTokenService(configuration);
    }

    private static TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(TestKey))
        };
    }
}
