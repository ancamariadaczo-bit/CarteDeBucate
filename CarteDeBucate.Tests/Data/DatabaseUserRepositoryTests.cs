using Microsoft.Data.Sqlite;

public class DatabaseUserRepositoryTests
{
    [Fact]
    public void GetByEmail_WithDifferentCasing_ShouldReturnUser()
    {
        WithRepository(repository =>
        {
            User user = CreateUser("anca@example.com");
            repository.Add(user);

            User? result = repository.GetByEmail("AnCa@Example.com");

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal("anca@example.com", result.Email);
            Assert.Equal(user.EmailVerifiedAtUtc, result.EmailVerifiedAtUtc);
            Assert.Equal(user.PreferredCulture, result.PreferredCulture);
        });
    }

    [Fact]
    public void Add_WhenEmailDiffersOnlyByCasing_ShouldThrow()
    {
        WithRepository(repository =>
        {
            repository.Add(CreateUser("anca@example.com"));

            Assert.Throws<SqliteException>(() => repository.Add(CreateUser("AnCa@example.com")));
        });
    }

    [Fact]
    public void Login_WithDifferentEmailCasing_ShouldSucceed()
    {
        WithRepository(repository =>
        {
            CurrentUserContext currentUserContext = new CurrentUserContext();
            AuthenticationService authenticationService =
                new AuthenticationService(repository, currentUserContext);
            PasswordHasher passwordHasher = new PasswordHasher();
            (string hash, string salt) = passwordHasher.HashPassword("secret-password");
            repository.Add(new User
            {
                Email = "anca@example.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow,
                EmailVerifiedAtUtc = DateTime.UtcNow
            });

            AuthenticationResult result =
                authenticationService.Login("AnCa@Example.com", "secret-password");

            Assert.True(result.IsSuccess);
            Assert.Equal("anca@example.com", result.User?.Email);
            Assert.Equal("anca@example.com", currentUserContext.Email);
        });
    }

    [Fact]
    public void GetById_ShouldReturnUser()
    {
        WithRepository(repository =>
        {
            User user = CreateUser("anca@example.com");
            repository.Add(user);

            User? result = repository.GetById(user.Id);

            Assert.NotNull(result);
            Assert.Equal(user.Email, result.Email);
        });
    }

    [Fact]
    public void UpdatePreferredCulture_ShouldPersistTheSupportedCulture()
    {
        WithRepository(repository =>
        {
            User user = CreateUser("anca@example.com");
            repository.Add(user);

            bool wasUpdated = repository.UpdatePreferredCulture(user.Id, "en-US");

            Assert.True(wasUpdated);
            Assert.Equal("en-US", repository.GetById(user.Id)?.PreferredCulture);
        });
    }

    private static User CreateUser(string email)
    {
        return new User
        {
            Email = email,
            PasswordHash = "hash",
            PasswordSalt = "salt",
            CreatedAt = DateTime.UtcNow,
            EmailVerifiedAtUtc = DateTime.UtcNow,
            PreferredCulture = "ro-RO"
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
