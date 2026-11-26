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
            Assert.Equal(1L, result.SecurityVersion);
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
            Assert.Equal(1L, result.SecurityVersion);
        });
    }

    [Fact]
    public void GetByEmailAndId_ShouldReturnStoredSecurityVersion()
    {
        WithRepository((repository, databasePath) =>
        {
            User user = CreateUser("anca@example.com");
            repository.Add(user);
            SetSecurityVersion(databasePath, user.Id, 5);

            Assert.Equal(5L, repository.GetByEmail(user.Email)?.SecurityVersion);
            Assert.Equal(5L, repository.GetById(user.Id)?.SecurityVersion);
        });
    }

    [Fact]
    public void GetSecurityVersion_ShouldReturnStoredVersionOrNullWhenTheUserDoesNotExist()
    {
        WithRepository((repository, databasePath) =>
        {
            User user = CreateUser("anca@example.com");
            repository.Add(user);
            SetSecurityVersion(databasePath, user.Id, 5);

            Assert.Equal(5L, repository.GetSecurityVersion(user.Id));
            Assert.Null(repository.GetSecurityVersion(user.Id + 1));
        });
    }

    [Fact]
    public void IncrementSecurityVersion_ShouldPersistOneAtomicIncrementAndReportMissingUsers()
    {
        WithRepository(repository =>
        {
            User user = CreateUser("anca@example.com");
            repository.Add(user);

            Assert.True(repository.IncrementSecurityVersion(user.Id));
            Assert.Equal(2L, repository.GetSecurityVersion(user.Id));
            Assert.False(repository.IncrementSecurityVersion(user.Id + 1));
        });
    }

    [Fact]
    public async Task IncrementSecurityVersion_WhenTwoCallsCompete_ShouldPersistBothIncrements()
    {
        await WithRepositoryAsync(async (repository, databasePath) =>
        {
            User user = CreateUser("anca@example.com");
            repository.Add(user);
            DatabaseUserRepository competingRepository = new DatabaseUserRepository(databasePath);

            Task<bool> firstIncrement = Task.Run(() =>
                repository.IncrementSecurityVersion(user.Id));
            Task<bool> competingIncrement = Task.Run(() =>
                competingRepository.IncrementSecurityVersion(user.Id));

            bool[] results = await Task.WhenAll(firstIncrement, competingIncrement);

            Assert.All(results, result => Assert.True(result));
            Assert.Equal(3L, repository.GetSecurityVersion(user.Id));
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
        WithRepository((repository, _) => test(repository));
    }

    private static void WithRepository(Action<DatabaseUserRepository, string> test)
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"users-{Guid.NewGuid()}.db");

        try
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();

            DatabaseUserRepository repository = new DatabaseUserRepository(databasePath);
            test(repository, databasePath);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static async Task WithRepositoryAsync(
        Func<DatabaseUserRepository, string, Task> test)
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"users-{Guid.NewGuid()}.db");

        try
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();

            DatabaseUserRepository repository = new DatabaseUserRepository(databasePath);
            await test(repository, databasePath);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static void SetSecurityVersion(string databasePath, int userId, long securityVersion)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET SecurityVersion = @SecurityVersion
            WHERE Id = @UserId;
            """;
        command.Parameters.AddWithValue("@SecurityVersion", securityVersion);
        command.Parameters.AddWithValue("@UserId", userId);
        command.ExecuteNonQuery();
    }
}
