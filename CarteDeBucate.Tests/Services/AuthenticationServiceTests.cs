public class AuthenticationServiceTests
{
    [Fact]
    public void Login_WithVerifiedEmailAndValidPassword_ShouldLogInUser()
    {
        FakeUserRepository userRepository = new FakeUserRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        PasswordHasher passwordHasher = new PasswordHasher();
        (string hash, string salt) = passwordHasher.HashPassword("secret-password");
        userRepository.Users.Add(new User
        {
            Id = 1,
            Email = "anca@example.com",
            PasswordHash = hash,
            PasswordSalt = salt,
            EmailVerifiedAtUtc = DateTime.UtcNow
        });
        AuthenticationService authenticationService = new AuthenticationService(
            userRepository,
            currentUserContext,
            passwordHasher);

        AuthenticationResult result = authenticationService.Login(
            "AnCa@Example.com",
            "secret-password");

        Assert.True(result.IsSuccess);
        Assert.Equal("Login successful.", result.Message);
        Assert.Equal("anca@example.com", result.User?.Email);
        Assert.True(authenticationService.IsLoggedIn);
        Assert.True(currentUserContext.IsAuthenticated);
        Assert.Equal(1, currentUserContext.UserId);
        Assert.Equal("anca@example.com", currentUserContext.Email);
    }

    [Fact]
    public void Login_WithUnverifiedEmail_ShouldFailWithTheGenericCredentialsMessage()
    {
        FakeUserRepository userRepository = new FakeUserRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        PasswordHasher passwordHasher = new PasswordHasher();
        (string hash, string salt) = passwordHasher.HashPassword("secret-password");
        userRepository.Users.Add(new User { Id = 1, Email = "anca@example.com", PasswordHash = hash, PasswordSalt = salt });

        AuthenticationService authenticationService = new AuthenticationService(
            userRepository,
            currentUserContext,
            passwordHasher);

        AuthenticationResult result = authenticationService.Login(
            "anca@example.com",
            "secret-password");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.Message);
        Assert.False(authenticationService.IsLoggedIn);
        Assert.False(currentUserContext.IsAuthenticated);
    }

    [Fact]
    public void Login_WithWrongPassword_ShouldFailWithTheGenericCredentialsMessage()
    {
        FakeUserRepository userRepository = new FakeUserRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        PasswordHasher passwordHasher = new PasswordHasher();
        (string hash, string salt) = passwordHasher.HashPassword("secret-password");
        userRepository.Users.Add(new User
        {
            Id = 1,
            Email = "anca@example.com",
            PasswordHash = hash,
            PasswordSalt = salt,
            EmailVerifiedAtUtc = DateTime.UtcNow
        });
        AuthenticationService authenticationService = new AuthenticationService(
            userRepository,
            currentUserContext,
            passwordHasher);

        AuthenticationResult result = authenticationService.Login(
            "anca@example.com",
            "wrong-password");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.Message);
        Assert.False(authenticationService.IsLoggedIn);
        Assert.False(currentUserContext.IsAuthenticated);
        Assert.Null(currentUserContext.UserId);
        Assert.Null(currentUserContext.Email);
    }
}
