using Microsoft.Data.Sqlite;

public class DatabaseAccountRepositoryTests
{
    [Fact]
    public void Register_ShouldCreateUnverifiedUserAndVerificationToken()
    {
        WithRepository((repository, databasePath) =>
        {
            AccountRegistrationRequest request = CreateRequest("Anca@Example.com", 1);

            AccountRegistrationResult result = repository.Register(request);

            Assert.Equal(AccountRegistrationResultCode.Success, result.Code);
            Assert.NotNull(result.UserId);

            using SqliteConnection connection = OpenConnection(databasePath);
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    users.Email,
                    users.EmailVerifiedAtUtc,
                    users.PreferredCulture,
                    tokens.UserId,
                    tokens.Purpose,
                    tokens.TargetEmail,
                    tokens.TokenHash
                FROM Users AS users
                INNER JOIN UserEmailTokens AS tokens ON tokens.UserId = users.Id;
                """;

            using SqliteDataReader reader = command.ExecuteReader();

            Assert.True(reader.Read());
            Assert.Equal("anca@example.com", reader.GetString(0));
            Assert.True(reader.IsDBNull(1));
            Assert.Equal("ro-RO", reader.GetString(2));
            Assert.Equal(result.UserId.Value, reader.GetInt32(3));
            Assert.Equal((int)EmailTokenPurpose.RegistrationVerification, reader.GetInt32(4));
            Assert.Equal("anca@example.com", reader.GetString(5));
            Assert.Equal(CreateTokenHash(1), (byte[])reader[6]);
            Assert.False(reader.Read());
        });
    }

    [Fact]
    public void Register_WhenEmailAlreadyExists_ShouldReturnDuplicateEmail()
    {
        WithRepository((repository, databasePath) =>
        {
            repository.Register(CreateRequest("anca@example.com", 1));

            AccountRegistrationResult result =
                repository.Register(CreateRequest("AnCa@Example.com", 2));

            Assert.Equal(AccountRegistrationResultCode.DuplicateEmail, result.Code);
            Assert.Null(result.UserId);
            Assert.Equal(1, CountRows(databasePath, "Users"));
            Assert.Equal(1, CountRows(databasePath, "UserEmailTokens"));
        });
    }

    [Fact]
    public void Register_WhenTokenInsertFails_ShouldRollBackUserCreation()
    {
        WithRepository((repository, databasePath) =>
        {
            repository.Register(CreateRequest("anca@example.com", 1));

            Assert.Throws<SqliteException>(() =>
                repository.Register(CreateRequest("maria@example.com", 1)));

            Assert.Equal(1, CountRows(databasePath, "Users"));
            Assert.Equal(1, CountRows(databasePath, "UserEmailTokens"));
        });
    }

    private static AccountRegistrationRequest CreateRequest(string email, byte tokenHashMarker)
    {
        DateTime now = DateTime.UtcNow;

        return new AccountRegistrationRequest(
            email,
            "password-hash",
            "password-salt",
            now,
            "ro-RO",
            new EmailTokenIssuance(
                EmailTokenPurpose.RegistrationVerification,
                CreateTokenHash(tokenHashMarker),
                email,
                "ro-RO",
                now,
                now.AddDays(1)));
    }

    private static byte[] CreateTokenHash(byte marker)
    {
        byte[] tokenHash = new byte[32];
        tokenHash[0] = marker;
        return tokenHash;
    }

    private static int CountRows(string databasePath, string tableName)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static SqliteConnection OpenConnection(string databasePath)
    {
        SqliteConnection connection =
            new SqliteConnection(SqliteConnectionStringFactory.Create(databasePath));
        connection.Open();
        return connection;
    }

    private static void WithRepository(Action<DatabaseAccountRepository, string> test)
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"accounts-{Guid.NewGuid()}.db");

        try
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();

            DatabaseAccountRepository repository = new DatabaseAccountRepository(databasePath);
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
}
