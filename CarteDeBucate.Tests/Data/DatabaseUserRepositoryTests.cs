using Microsoft.Data.Sqlite;

public class DatabaseUserRepositoryTests
{
    [Fact]
    public void GetByUsername_WithDifferentCasing_ShouldReturnUser()
    {
        WithRepository(repository =>
        {
            User user = CreateUser("anca");
            repository.Add(user);

            User? result = repository.GetByUsername("AnCa");

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal("anca", result.Username);
        });
    }

    [Fact]
    public void Add_WhenUsernameDiffersOnlyByCasing_ShouldThrow()
    {
        WithRepository(repository =>
        {
            repository.Add(CreateUser("anca"));

            Assert.Throws<SqliteException>(() => repository.Add(CreateUser("Anca")));
        });
    }

    [Fact]
    public void Register_WhenUsernameDiffersOnlyByCasing_ShouldReturnDuplicateMessage()
    {
        WithRepository(repository =>
        {
            AuthenticationService authenticationService = new AuthenticationService(repository);
            AuthenticationResult firstResult = authenticationService.Register("anca", "secret-password");

            AuthenticationResult duplicateResult =
                authenticationService.Register("Anca", "another-password");

            Assert.True(firstResult.IsSuccess);
            Assert.False(duplicateResult.IsSuccess);
            Assert.Equal("This username already exists.", duplicateResult.Message);
        });
    }

    [Fact]
    public void Login_WithDifferentUsernameCasing_ShouldSucceed()
    {
        WithRepository(repository =>
        {
            CurrentUserContext currentUserContext = new CurrentUserContext();
            AuthenticationService authenticationService =
                new AuthenticationService(repository, currentUserContext);
            authenticationService.Register("anca", "secret-password");
            authenticationService.Logout();

            AuthenticationResult result =
                authenticationService.Login("AnCa", "secret-password");

            Assert.True(result.IsSuccess);
            Assert.Equal("anca", result.User?.Username);
            Assert.Equal("anca", currentUserContext.Username);
        });
    }

    private static User CreateUser(string username)
    {
        return new User
        {
            Username = username,
            PasswordHash = "hash",
            PasswordSalt = "salt",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void WithRepository(Action<DatabaseUserRepository> test)
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"users-{Guid.NewGuid()}.db");

        try
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();

            DatabaseUserRepository repository = new DatabaseUserRepository(databasePath);
            test(repository);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
