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

    [Fact]
    public void ReplaceEmailToken_ShouldRevokeTheActiveTokenBeforeCreatingItsReplacement()
    {
        WithRepository((repository, databasePath) =>
        {
            AccountRegistrationResult registration =
                repository.Register(CreateRequest("anca@example.com", 1));
            DateTime replacementCreatedAtUtc = DateTime.UtcNow;

            EmailTokenReplacementResult result = repository.ReplaceEmailToken(
                registration.UserId!.Value,
                CreateToken("anca@example.com", 2, replacementCreatedAtUtc));

            Assert.Equal(EmailTokenReplacementResultCode.Success, result.Code);

            using SqliteConnection connection = OpenConnection(databasePath);
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT TokenHash, RevokedAtUtc
                FROM UserEmailTokens
                ORDER BY Id;
                """;

            using SqliteDataReader reader = command.ExecuteReader();

            Assert.True(reader.Read());
            Assert.Equal(CreateTokenHash(1), (byte[])reader[0]);
            Assert.False(reader.IsDBNull(1));
            Assert.True(reader.Read());
            Assert.Equal(CreateTokenHash(2), (byte[])reader[0]);
            Assert.True(reader.IsDBNull(1));
            Assert.False(reader.Read());

            EmailVerificationResult revokedTokenResult = repository.ConfirmRegistrationEmail(
                CreateTokenHash(1),
                replacementCreatedAtUtc);

            Assert.Equal(EmailVerificationResultCode.InvalidToken, revokedTokenResult.Code);
        });
    }

    [Fact]
    public void ResendRegistrationEmail_ShouldReplaceTheActiveToken()
    {
        WithRepository((repository, databasePath) =>
        {
            DateTime now = DateTime.UtcNow;
            AccountRegistrationResult registration = repository.Register(
                CreateRequest(
                    "anca@example.com",
                    1,
                    now.AddMinutes(-2),
                    now.AddDays(1)));

            AccountRegistrationResendResult result =
                repository.ResendRegistrationEmail(
                    CreateRegistrationResendRequest(
                        "anca@example.com",
                        2,
                        now));

            Assert.Equal(AccountRegistrationResendResultCode.Issued, result.Code);
            Assert.NotNull(registration.UserId);
            Assert.NotNull(ReadTokenRevokedAtUtc(databasePath, CreateTokenHash(1)));
            Assert.Null(ReadTokenRevokedAtUtc(databasePath, CreateTokenHash(2)));

            EmailVerificationResult formerTokenResult =
                repository.ConfirmRegistrationEmail(CreateTokenHash(1), now);
            Assert.Equal(EmailVerificationResultCode.InvalidToken, formerTokenResult.Code);
        });
    }

    [Fact]
    public void ResendRegistrationEmail_WhenWithinCooldown_ShouldNotReplaceTheActiveToken()
    {
        WithRepository((repository, databasePath) =>
        {
            DateTime now = DateTime.UtcNow;
            repository.Register(CreateRequest(
                "anca@example.com",
                1,
                now.AddSeconds(-30),
                now.AddDays(1)));

            AccountRegistrationResendResult result =
                repository.ResendRegistrationEmail(
                    CreateRegistrationResendRequest(
                        "anca@example.com",
                        2,
                        now));

            Assert.Equal(AccountRegistrationResendResultCode.Cooldown, result.Code);
            Assert.Null(ReadTokenRevokedAtUtc(databasePath, CreateTokenHash(1)));
            Assert.False(TokenExists(databasePath, CreateTokenHash(2)));
        });
    }

    [Fact]
    public void ResendRegistrationEmail_WhenDailyLimitIsReached_ShouldNotReplaceTheActiveToken()
    {
        WithRepository((repository, databasePath) =>
        {
            DateTime now = DateTime.UtcNow;
            repository.Register(CreateRequest(
                "anca@example.com",
                1,
                now.AddMinutes(-10),
                now.AddDays(1)));

            for (byte marker = 2; marker <= 5; marker++)
            {
                DateTime issuedAtUtc = now.AddMinutes(-10 + (marker - 1) * 2);
                AccountRegistrationResendResult replacementResult =
                    repository.ResendRegistrationEmail(
                        CreateRegistrationResendRequest(
                            "anca@example.com",
                            marker,
                            issuedAtUtc));

                Assert.Equal(
                    AccountRegistrationResendResultCode.Issued,
                    replacementResult.Code);
            }

            AccountRegistrationResendResult result =
                repository.ResendRegistrationEmail(
                    CreateRegistrationResendRequest(
                        "anca@example.com",
                        6,
                        now));

            Assert.Equal(AccountRegistrationResendResultCode.DailyLimit, result.Code);
            Assert.Null(ReadTokenRevokedAtUtc(databasePath, CreateTokenHash(5)));
            Assert.False(TokenExists(databasePath, CreateTokenHash(6)));
        });
    }

    [Fact]
    public void ResendRegistrationEmail_ForUnknownOrVerifiedEmail_ShouldNotIssueAToken()
    {
        WithRepository((repository, databasePath) =>
        {
            DateTime now = DateTime.UtcNow;

            AccountRegistrationResendResult unknownResult =
                repository.ResendRegistrationEmail(
                    CreateRegistrationResendRequest(
                        "unknown@example.com",
                        1,
                        now));

            repository.Register(CreateRequest(
                "anca@example.com",
                2,
                now.AddMinutes(-2),
                now.AddDays(1)));
            repository.ConfirmRegistrationEmail(CreateTokenHash(2), now);

            AccountRegistrationResendResult verifiedResult =
                repository.ResendRegistrationEmail(
                    CreateRegistrationResendRequest(
                        "anca@example.com",
                        3,
                        now.AddMinutes(1)));

            Assert.Equal(AccountRegistrationResendResultCode.NotEligible, unknownResult.Code);
            Assert.Equal(AccountRegistrationResendResultCode.NotEligible, verifiedResult.Code);
            Assert.False(TokenExists(databasePath, CreateTokenHash(1)));
            Assert.False(TokenExists(databasePath, CreateTokenHash(3)));
        });
    }

    [Fact]
    public void ConfirmRegistrationEmail_WhenTokenIsUsed_ShouldRejectReuse()
    {
        WithRepository((repository, databasePath) =>
        {
            repository.Register(CreateRequest("anca@example.com", 1));
            DateTime confirmationTime = DateTime.UtcNow;

            EmailVerificationResult firstResult =
                repository.ConfirmRegistrationEmail(CreateTokenHash(1), confirmationTime);
            EmailVerificationResult secondResult =
                repository.ConfirmRegistrationEmail(CreateTokenHash(1), confirmationTime);

            Assert.Equal(EmailVerificationResultCode.Success, firstResult.Code);
            Assert.Equal(EmailVerificationResultCode.InvalidToken, secondResult.Code);
            Assert.NotNull(ReadUserEmailVerifiedAtUtc(databasePath));
            Assert.NotNull(ReadTokenUsedAtUtc(databasePath, CreateTokenHash(1)));
        });
    }

    [Fact]
    public void ReviewRegistrationEmail_WithValidToken_ShouldNotConsumeIt()
    {
        WithRepository((repository, databasePath) =>
        {
            repository.Register(CreateRequest("anca@example.com", 1));
            DateTime reviewTime = DateTime.UtcNow;

            EmailVerificationReviewResult reviewResult =
                repository.ReviewRegistrationEmail(
                    CreateTokenHash(1),
                    reviewTime);

            Assert.Equal(EmailVerificationReviewResultCode.Valid, reviewResult.Code);
            Assert.Null(ReadUserEmailVerifiedAtUtc(databasePath));
            Assert.Null(ReadTokenUsedAtUtc(databasePath, CreateTokenHash(1)));

            EmailVerificationResult confirmationResult =
                repository.ConfirmRegistrationEmail(
                    CreateTokenHash(1),
                    reviewTime);

            Assert.Equal(EmailVerificationResultCode.Success, confirmationResult.Code);
        });
    }

    [Fact]
    public void ReviewRegistrationEmail_WithExpiredToken_ShouldReturnExpired()
    {
        WithRepository((repository, databasePath) =>
        {
            DateTime reviewTime = new DateTime(
                2026,
                11,
                24,
                12,
                0,
                0,
                DateTimeKind.Utc);
            repository.Register(CreateRequest(
                "anca@example.com",
                1,
                reviewTime.AddDays(-2),
                reviewTime.AddDays(-1)));

            EmailVerificationReviewResult result =
                repository.ReviewRegistrationEmail(
                    CreateTokenHash(1),
                    reviewTime);

            Assert.Equal(EmailVerificationReviewResultCode.ExpiredToken, result.Code);
            Assert.Null(ReadUserEmailVerifiedAtUtc(databasePath));
            Assert.Null(ReadTokenUsedAtUtc(databasePath, CreateTokenHash(1)));
        });
    }

    [Fact]
    public void ConfirmRegistrationEmail_WhenTokenIsExpired_ShouldNotVerifyUser()
    {
        WithRepository((repository, databasePath) =>
        {
            DateTime confirmationTime = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
            repository.Register(CreateRequest(
                "anca@example.com",
                1,
                confirmationTime.AddDays(-2),
                confirmationTime.AddDays(-1)));

            EmailVerificationResult result =
                repository.ConfirmRegistrationEmail(CreateTokenHash(1), confirmationTime);

            Assert.Equal(EmailVerificationResultCode.ExpiredToken, result.Code);
            Assert.Null(ReadUserEmailVerifiedAtUtc(databasePath));
            Assert.Null(ReadTokenUsedAtUtc(databasePath, CreateTokenHash(1)));
        });
    }

    [Fact]
    public void ConfirmRegistrationEmail_WhenTokenHasWrongPurpose_ShouldRejectIt()
    {
        WithRepository((repository, _) =>
        {
            AccountRegistrationResult registration =
                repository.Register(CreateRequest("anca@example.com", 1));
            DateTime tokenCreatedAtUtc = DateTime.UtcNow;

            repository.ReplaceEmailToken(
                registration.UserId!.Value,
                new EmailTokenIssuance(
                    EmailTokenPurpose.EmailChangeVerification,
                    CreateTokenHash(2),
                    "new-address@example.com",
                    "ro-RO",
                    tokenCreatedAtUtc,
                    tokenCreatedAtUtc.AddDays(1)));

            EmailVerificationResult result = repository.ConfirmRegistrationEmail(
                CreateTokenHash(2),
                tokenCreatedAtUtc);

            Assert.Equal(EmailVerificationResultCode.InvalidToken, result.Code);
        });
    }

    [Fact]
    public async Task ConfirmRegistrationEmail_WhenTwoCallsCompete_ShouldAllowOnlyOneSuccess()
    {
        await WithRepositoryAsync(async (repository, databasePath) =>
        {
            repository.Register(CreateRequest("anca@example.com", 1));
            DateTime confirmationTime = DateTime.UtcNow;
            DatabaseAccountRepository competingRepository = new DatabaseAccountRepository(databasePath);

            Task<EmailVerificationResult> firstConfirmation = Task.Run(() =>
                repository.ConfirmRegistrationEmail(CreateTokenHash(1), confirmationTime));
            Task<EmailVerificationResult> competingConfirmation = Task.Run(() =>
                competingRepository.ConfirmRegistrationEmail(CreateTokenHash(1), confirmationTime));

            EmailVerificationResult[] results = await Task.WhenAll(
                firstConfirmation,
                competingConfirmation);

            Assert.Single(results, result => result.Code == EmailVerificationResultCode.Success);
            Assert.Single(results, result => result.Code == EmailVerificationResultCode.InvalidToken);
        });
    }

    [Fact]
    public void RequestEmailChange_ShouldKeepTheCurrentEmailUntilConfirmation()
    {
        WithRepository((repository, databasePath) =>
        {
            AccountRegistrationResult registration =
                repository.Register(CreateRequest("anca@example.com", 1));
            DateTime tokenCreatedAtUtc = DateTime.UtcNow;

            EmailChangeRequestResult result = repository.RequestEmailChange(
                registration.UserId!.Value,
                CreateEmailChangeToken("new-address@example.com", 2, tokenCreatedAtUtc));

            Assert.Equal(EmailChangeRequestResultCode.Success, result.Code);
            Assert.Equal(
                "anca@example.com",
                ReadUserEmail(databasePath, registration.UserId.Value));
            Assert.Equal(
                "new-address@example.com",
                ReadTokenTargetEmail(databasePath, CreateTokenHash(2)));
        });
    }

    [Fact]
    public void RequestEmailChange_ShouldRevokeThePreviousEmailChangeToken()
    {
        WithRepository((repository, databasePath) =>
        {
            AccountRegistrationResult registration =
                repository.Register(CreateRequest("anca@example.com", 1));
            DateTime firstTokenCreatedAtUtc = DateTime.UtcNow;

            repository.RequestEmailChange(
                registration.UserId!.Value,
                CreateEmailChangeToken("first-new-address@example.com", 2, firstTokenCreatedAtUtc));

            DateTime replacementTokenCreatedAtUtc = firstTokenCreatedAtUtc.AddMinutes(1);
            EmailChangeRequestResult replacementResult = repository.RequestEmailChange(
                registration.UserId.Value,
                CreateEmailChangeToken(
                    "replacement-new-address@example.com",
                    3,
                    replacementTokenCreatedAtUtc));

            Assert.Equal(EmailChangeRequestResultCode.Success, replacementResult.Code);
            Assert.NotNull(ReadTokenRevokedAtUtc(databasePath, CreateTokenHash(2)));
            Assert.Equal(
                "replacement-new-address@example.com",
                ReadTokenTargetEmail(databasePath, CreateTokenHash(3)));
        });
    }

    [Fact]
    public void ConfirmEmailChange_WhenTargetEmailAlreadyBelongsToAnotherUser_ShouldReturnDuplicate()
    {
        WithRepository((repository, databasePath) =>
        {
            repository.Register(CreateRequest("anca@example.com", 1));
            AccountRegistrationResult registration =
                repository.Register(CreateRequest("maria@example.com", 2));
            DateTime tokenCreatedAtUtc = DateTime.UtcNow;

            repository.RequestEmailChange(
                registration.UserId!.Value,
                CreateEmailChangeToken("anca@example.com", 3, tokenCreatedAtUtc));

            EmailChangeConfirmationResult result = repository.ConfirmEmailChange(
                CreateTokenHash(3),
                tokenCreatedAtUtc);

            Assert.Equal(EmailChangeConfirmationResultCode.DuplicateEmail, result.Code);
            Assert.Equal(
                "maria@example.com",
                ReadUserEmail(databasePath, registration.UserId.Value));
            Assert.Null(ReadTokenUsedAtUtc(databasePath, CreateTokenHash(3)));
        });
    }

    [Fact]
    public void ConfirmEmailChange_ShouldUpdateAndVerifyTheTargetEmail()
    {
        WithRepository((repository, databasePath) =>
        {
            AccountRegistrationResult registration =
                repository.Register(CreateRequest("anca@example.com", 1));
            DateTime tokenCreatedAtUtc = DateTime.UtcNow;
            repository.RequestEmailChange(
                registration.UserId!.Value,
                CreateEmailChangeToken("new-address@example.com", 2, tokenCreatedAtUtc));

            EmailChangeConfirmationResult result = repository.ConfirmEmailChange(
                CreateTokenHash(2),
                tokenCreatedAtUtc);

            Assert.Equal(EmailChangeConfirmationResultCode.Success, result.Code);
            Assert.Equal(
                "new-address@example.com",
                ReadUserEmail(databasePath, registration.UserId.Value));
            Assert.NotNull(ReadUserEmailVerifiedAtUtc(databasePath));
            Assert.NotNull(ReadTokenUsedAtUtc(databasePath, CreateTokenHash(2)));
        });
    }

    [Fact]
    public void ConfirmEmailChange_WhenTokenHasRegistrationPurpose_ShouldRejectIt()
    {
        WithRepository((repository, _) =>
        {
            repository.Register(CreateRequest("anca@example.com", 1));

            EmailChangeConfirmationResult result = repository.ConfirmEmailChange(
                CreateTokenHash(1),
                DateTime.UtcNow);

            Assert.Equal(EmailChangeConfirmationResultCode.InvalidToken, result.Code);
        });
    }

    [Fact]
    public async Task ConfirmEmailChange_WhenTwoUsersClaimTheSameTarget_ShouldAllowOneSuccess()
    {
        await WithRepositoryAsync(async (repository, databasePath) =>
        {
            AccountRegistrationResult firstRegistration =
                repository.Register(CreateRequest("anca@example.com", 1));
            AccountRegistrationResult secondRegistration =
                repository.Register(CreateRequest("maria@example.com", 2));
            DateTime tokenCreatedAtUtc = DateTime.UtcNow;

            repository.RequestEmailChange(
                firstRegistration.UserId!.Value,
                CreateEmailChangeToken("shared@example.com", 3, tokenCreatedAtUtc));
            repository.RequestEmailChange(
                secondRegistration.UserId!.Value,
                CreateEmailChangeToken("shared@example.com", 4, tokenCreatedAtUtc));

            DatabaseAccountRepository competingRepository = new DatabaseAccountRepository(databasePath);
            Task<EmailChangeConfirmationResult> firstConfirmation = Task.Run(() =>
                repository.ConfirmEmailChange(CreateTokenHash(3), tokenCreatedAtUtc));
            Task<EmailChangeConfirmationResult> competingConfirmation = Task.Run(() =>
                competingRepository.ConfirmEmailChange(CreateTokenHash(4), tokenCreatedAtUtc));

            EmailChangeConfirmationResult[] results = await Task.WhenAll(
                firstConfirmation,
                competingConfirmation);

            Assert.Single(results, result => result.Code == EmailChangeConfirmationResultCode.Success);
            Assert.Single(
                results,
                result => result.Code == EmailChangeConfirmationResultCode.DuplicateEmail);
        });
    }

    [Fact]
    public void TokenIssuance_ShouldRemoveOnlyTokensCompletedOrExpiredMoreThanThirtyDaysAgo()
    {
        WithRepository((repository, databasePath) =>
        {
            DateTime now = DateTime.UtcNow;
            DateTime oldCreatedAtUtc = now.AddDays(-32);

            repository.Register(
                CreateRequest(
                    "used-token@example.com",
                    5,
                    oldCreatedAtUtc,
                    oldCreatedAtUtc.AddDays(1)));
            repository.ConfirmRegistrationEmail(
                CreateTokenHash(5),
                oldCreatedAtUtc.AddMinutes(1));

            repository.Register(CreateRequest("cleanup-trigger@example.com", 6, now));

            Assert.False(TokenExists(databasePath, CreateTokenHash(5)));

            repository.Register(
                CreateRequest(
                    "expired-token@example.com",
                    7,
                    oldCreatedAtUtc,
                    oldCreatedAtUtc.AddDays(1)));
            repository.Register(CreateRequest("active-token@example.com", 8, now));

            Assert.False(TokenExists(databasePath, CreateTokenHash(7)));
            Assert.True(TokenExists(databasePath, CreateTokenHash(8)));

            repository.ConfirmRegistrationEmail(CreateTokenHash(8), now);
            repository.Register(CreateRequest("recent-token@example.com", 9, now));

            Assert.True(TokenExists(databasePath, CreateTokenHash(8)));

            AccountRegistrationResult emailChangeRegistration =
                repository.Register(CreateRequest("revoked-token@example.com", 10, now));
            repository.RequestEmailChange(
                emailChangeRegistration.UserId!.Value,
                CreateEmailChangeToken(
                    "first-target@example.com",
                    11,
                    oldCreatedAtUtc,
                    now.AddDays(1)));
            repository.RequestEmailChange(
                emailChangeRegistration.UserId.Value,
                CreateEmailChangeToken(
                    "replacement-target@example.com",
                    12,
                    now.AddDays(-31),
                    now.AddDays(1)));

            repository.Register(CreateRequest("revocation-cleanup-trigger@example.com", 13, now));

            Assert.False(TokenExists(databasePath, CreateTokenHash(11)));
            Assert.True(TokenExists(databasePath, CreateTokenHash(12)));
        });
    }

    private static AccountRegistrationRequest CreateRequest(
        string email,
        byte tokenHashMarker,
        DateTime? createdAtUtc = null,
        DateTime? expiresAtUtc = null)
    {
        DateTime createdAt = createdAtUtc ?? DateTime.UtcNow;

        return new AccountRegistrationRequest(
            email,
            "password-hash",
            "password-salt",
            createdAt,
            "ro-RO",
            CreateToken(email, tokenHashMarker, createdAt, expiresAtUtc));
    }

    private static EmailTokenIssuance CreateToken(
        string email,
        byte tokenHashMarker,
        DateTime createdAtUtc,
        DateTime? expiresAtUtc = null)
    {
        return new EmailTokenIssuance(
            EmailTokenPurpose.RegistrationVerification,
            CreateTokenHash(tokenHashMarker),
            email,
            "ro-RO",
            createdAtUtc,
            expiresAtUtc ?? createdAtUtc.AddDays(1));
    }

    private static AccountRegistrationResendRequest CreateRegistrationResendRequest(
        string email,
        byte tokenHashMarker,
        DateTime issuedAtUtc)
    {
        return new AccountRegistrationResendRequest(
            email,
            CreateToken(email, tokenHashMarker, issuedAtUtc));
    }

    private static EmailTokenIssuance CreateEmailChangeToken(
        string targetEmail,
        byte tokenHashMarker,
        DateTime createdAtUtc,
        DateTime? expiresAtUtc = null)
    {
        return new EmailTokenIssuance(
            EmailTokenPurpose.EmailChangeVerification,
            CreateTokenHash(tokenHashMarker),
            targetEmail,
            "ro-RO",
            createdAtUtc,
            expiresAtUtc ?? createdAtUtc.AddDays(1));
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

    private static DateTime? ReadUserEmailVerifiedAtUtc(string databasePath)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT EmailVerifiedAtUtc FROM Users;";

        object? value = command.ExecuteScalar();
        return value is null or DBNull
            ? null
            : DateTime.Parse((string)value, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    private static string ReadUserEmail(string databasePath, int userId)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Email FROM Users WHERE Id = @UserId;";
        command.Parameters.AddWithValue("@UserId", userId);
        return (string)command.ExecuteScalar()!;
    }

    private static DateTime? ReadTokenUsedAtUtc(string databasePath, byte[] tokenHash)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT UsedAtUtc
            FROM UserEmailTokens
            WHERE TokenHash = @TokenHash;
            """;
        command.Parameters.AddWithValue("@TokenHash", tokenHash);

        object? value = command.ExecuteScalar();
        return value is null or DBNull
            ? null
            : DateTime.Parse((string)value, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    private static DateTime? ReadTokenRevokedAtUtc(string databasePath, byte[] tokenHash)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT RevokedAtUtc
            FROM UserEmailTokens
            WHERE TokenHash = @TokenHash;
            """;
        command.Parameters.AddWithValue("@TokenHash", tokenHash);

        object? value = command.ExecuteScalar();
        return value is null or DBNull
            ? null
            : DateTime.Parse((string)value, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    private static string ReadTokenTargetEmail(string databasePath, byte[] tokenHash)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT TargetEmail
            FROM UserEmailTokens
            WHERE TokenHash = @TokenHash;
            """;
        command.Parameters.AddWithValue("@TokenHash", tokenHash);
        return (string)command.ExecuteScalar()!;
    }

    private static bool TokenExists(string databasePath, byte[] tokenHash)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM UserEmailTokens
                WHERE TokenHash = @TokenHash);
            """;
        command.Parameters.AddWithValue("@TokenHash", tokenHash);
        return Convert.ToInt64(command.ExecuteScalar()) == 1;
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

    private static async Task WithRepositoryAsync(
        Func<DatabaseAccountRepository, string, Task> test)
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"accounts-{Guid.NewGuid()}.db");

        try
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();

            DatabaseAccountRepository repository = new DatabaseAccountRepository(databasePath);
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
}
