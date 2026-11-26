using System.Security.Claims;
using CarteDeBucate.Web.Services.Authentication;

public class AuthenticatedSessionValidatorTests
{
    [Fact]
    public void IsValid_WhenClaimsMatchThePersistedSecurityVersion_ShouldReturnTrue()
    {
        FakeUserRepository userRepository = CreateUserRepository(
            new User { Id = 7, SecurityVersion = 3 });
        AuthenticatedSessionValidator validator = new(userRepository);

        bool isValid = validator.IsValid(CreatePrincipal("7", "3"));

        Assert.True(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-id")]
    [InlineData("0")]
    [InlineData("-1")]
    public void IsValid_WhenUserIdClaimIsMissingOrInvalid_ShouldReturnFalse(string? userId)
    {
        AuthenticatedSessionValidator validator = new(CreateUserRepository());

        bool isValid = validator.IsValid(CreatePrincipal(userId, "1"));

        Assert.False(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-version")]
    [InlineData("0")]
    [InlineData("-1")]
    public void IsValid_WhenSecurityVersionClaimIsMissingOrInvalid_ShouldReturnFalse(
        string? securityVersion)
    {
        AuthenticatedSessionValidator validator = new(CreateUserRepository());

        bool isValid = validator.IsValid(CreatePrincipal("7", securityVersion));

        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WhenARequiredClaimIsDuplicated_ShouldReturnFalse()
    {
        AuthenticatedSessionValidator validator = new(CreateUserRepository());
        ClaimsPrincipal principal = CreatePrincipal("7", "1");
        ((ClaimsIdentity)principal.Identity!).AddClaim(
            new Claim(AuthenticationClaimTypes.SecurityVersion, "1"));

        bool isValid = validator.IsValid(principal);

        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WhenUserIdClaimIsDuplicated_ShouldReturnFalse()
    {
        AuthenticatedSessionValidator validator = new(CreateUserRepository());
        ClaimsPrincipal principal = CreatePrincipal("7", "1");
        ((ClaimsIdentity)principal.Identity!).AddClaim(
            new Claim(ClaimTypes.NameIdentifier, "7"));

        bool isValid = validator.IsValid(principal);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("4")]
    public void IsValid_WhenSecurityVersionIsStaleOrFuture_ShouldReturnFalse(
        string securityVersion)
    {
        FakeUserRepository userRepository = CreateUserRepository(
            new User { Id = 7, SecurityVersion = 3 });
        AuthenticatedSessionValidator validator = new(userRepository);

        bool isValid = validator.IsValid(CreatePrincipal("7", securityVersion));

        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WhenUserWasDeleted_ShouldReturnFalse()
    {
        AuthenticatedSessionValidator validator = new(CreateUserRepository());

        bool isValid = validator.IsValid(CreatePrincipal("7", "1"));

        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WhenTheDatabaseReadFails_ShouldPropagateTheException()
    {
        AuthenticatedSessionValidator validator = new(new ThrowingUserRepository());

        Assert.Throws<InvalidOperationException>(() =>
            validator.IsValid(CreatePrincipal("7", "1")));
    }

    private static FakeUserRepository CreateUserRepository(params User[] users)
    {
        FakeUserRepository repository = new();
        repository.Users.AddRange(users);
        return repository;
    }

    private static ClaimsPrincipal CreatePrincipal(
        string? userId,
        string? securityVersion)
    {
        List<Claim> claims = [];

        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (securityVersion is not null)
        {
            claims.Add(new Claim(
                AuthenticationClaimTypes.SecurityVersion,
                securityVersion));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private sealed class ThrowingUserRepository : IUserRepository
    {
        public void Add(User user) => throw new NotSupportedException();

        public User? GetByEmail(string email) => throw new NotSupportedException();

        public User? GetById(int userId) => throw new NotSupportedException();

        public long? GetSecurityVersion(int userId) =>
            throw new InvalidOperationException("The database is unavailable.");

        public bool IncrementSecurityVersion(int userId) =>
            throw new NotSupportedException();

        public bool UpdatePreferredCulture(int userId, string preferredCulture) =>
            throw new NotSupportedException();
    }
}
