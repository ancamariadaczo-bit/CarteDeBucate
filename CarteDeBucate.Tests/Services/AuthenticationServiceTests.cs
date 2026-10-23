public class AuthenticationServiceTests
{
    [Fact]
    public void Register_WithValidCredentials_ShouldAddUserAndLogIn()
    {
        FakeUserRepository userRepository = new FakeUserRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        AuthenticationService authenticationService = new AuthenticationService(userRepository, currentUserContext);

        AuthenticationResult result = authenticationService.Register("anca", "secret-password");

        Assert.True(result.IsSuccess);
        Assert.Equal("User registered successfully.", result.Message);
        Assert.NotNull(result.User);
        Assert.True(userRepository.AddWasCalled);
        Assert.NotNull(userRepository.AddedUser);
        Assert.Equal("anca", userRepository.AddedUser.Username);
        Assert.False(string.IsNullOrWhiteSpace(userRepository.AddedUser.PasswordHash));
        Assert.False(string.IsNullOrWhiteSpace(userRepository.AddedUser.PasswordSalt));
        Assert.NotEqual("secret-password", userRepository.AddedUser.PasswordHash);
        Assert.True(authenticationService.IsLoggedIn);
        Assert.True(currentUserContext.IsAuthenticated);
        Assert.Equal(userRepository.AddedUser.Id, currentUserContext.UserId);
        Assert.Equal("anca", currentUserContext.Username);
    }

    [Fact]
    public void Register_WhenUsernameAlreadyExists_ShouldNotAddUser()
    {
        FakeUserRepository userRepository = new FakeUserRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        userRepository.Users.Add(new User
        {
            Id = 1,
            Username = "anca",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        });

        AuthenticationService authenticationService = new AuthenticationService(userRepository, currentUserContext);

        AuthenticationResult result = authenticationService.Register("Anca", "secret-password");

        Assert.False(result.IsSuccess);
        Assert.Equal("This username already exists.", result.Message);
        Assert.False(userRepository.AddWasCalled);
        Assert.False(authenticationService.IsLoggedIn);
        Assert.False(currentUserContext.IsAuthenticated);
    }

    [Fact]
    public void Login_WithRegisteredCredentials_ShouldLogInUser()
    {
        FakeUserRepository userRepository = new FakeUserRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        AuthenticationService authenticationService = new AuthenticationService(userRepository, currentUserContext);
        authenticationService.Register("anca", "secret-password");
        authenticationService.Logout();

        AuthenticationResult result = authenticationService.Login("anca", "secret-password");

        Assert.True(result.IsSuccess);
        Assert.Equal("Login successful.", result.Message);
        Assert.True(authenticationService.IsLoggedIn);
        Assert.True(currentUserContext.IsAuthenticated);
        Assert.Equal("anca", currentUserContext.Username);
    }

    [Fact]
    public void Login_WithWrongPassword_ShouldFailAndKeepUserLoggedOut()
    {
        FakeUserRepository userRepository = new FakeUserRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        AuthenticationService authenticationService = new AuthenticationService(userRepository, currentUserContext);
        authenticationService.Register("anca", "secret-password");
        authenticationService.Logout();

        AuthenticationResult result = authenticationService.Login("anca", "wrong-password");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid username or password.", result.Message);
        Assert.False(authenticationService.IsLoggedIn);
        Assert.False(currentUserContext.IsAuthenticated);
        Assert.Null(currentUserContext.UserId);
        Assert.Null(currentUserContext.Username);
    }
}
