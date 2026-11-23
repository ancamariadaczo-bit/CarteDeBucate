using Microsoft.Data.Sqlite;

public class DatabaseAccountRepository : IAccountRepository
{
    private readonly string _connectionString;

    public DatabaseAccountRepository(string databasePath)
    {
        _connectionString = SqliteConnectionStringFactory.Create(databasePath);
    }

    public AccountRegistrationResult Register(AccountRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string email = EmailNormalizer.Normalize(request.Email);
        EmailTokenIssuance verificationToken = request.VerificationToken;
        string targetEmail = EmailNormalizer.Normalize(verificationToken.TargetEmail);

        if (verificationToken.Purpose != EmailTokenPurpose.RegistrationVerification)
        {
            throw new ArgumentException(
                "Registration requires a registration-verification token.",
                nameof(request));
        }

        if (!string.Equals(email, targetEmail, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The registration email must match the verification token target email.",
                nameof(request));
        }

        if (verificationToken.TokenHash.Length != 32)
        {
            throw new ArgumentException(
                "The verification token hash must contain 32 bytes.",
                nameof(request));
        }

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            if (EmailExists(connection, transaction, email))
            {
                transaction.Rollback();
                return new AccountRegistrationResult(AccountRegistrationResultCode.DuplicateEmail, null);
            }

            int userId = InsertUnverifiedUser(connection, transaction, request, email);
            InsertToken(connection, transaction, userId, verificationToken, targetEmail);

            transaction.Commit();
            return new AccountRegistrationResult(AccountRegistrationResultCode.Success, userId);
        }
        catch (SqliteException exception) when (IsDuplicateEmail(exception))
        {
            transaction.Rollback();
            return new AccountRegistrationResult(AccountRegistrationResultCode.DuplicateEmail, null);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public EmailTokenReplacementResult ReplaceEmailToken(int userId, EmailTokenIssuance token)
    {
        throw new NotSupportedException("Email-token replacement is not implemented yet.");
    }

    public EmailVerificationResult ConfirmRegistrationEmail(byte[] tokenHash, DateTime confirmedAtUtc)
    {
        throw new NotSupportedException("Registration confirmation is not implemented yet.");
    }

    public EmailChangeRequestResult RequestEmailChange(int userId, EmailTokenIssuance token)
    {
        throw new NotSupportedException("Email-change requests are not implemented yet.");
    }

    public EmailChangeConfirmationResult ConfirmEmailChange(byte[] tokenHash, DateTime confirmedAtUtc)
    {
        throw new NotSupportedException("Email-change confirmation is not implemented yet.");
    }

    private static bool EmailExists(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string email)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM Users
                WHERE Email COLLATE NOCASE = $email);
            """;
        command.Parameters.AddWithValue("$email", email);

        return Convert.ToInt64(command.ExecuteScalar()) == 1;
    }

    private static int InsertUnverifiedUser(
        SqliteConnection connection,
        SqliteTransaction transaction,
        AccountRegistrationRequest request,
        string email)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Users (
                Email,
                PasswordHash,
                PasswordSalt,
                CreatedAt,
                EmailVerifiedAtUtc,
                PreferredCulture)
            VALUES (
                $email,
                $passwordHash,
                $passwordSalt,
                $createdAt,
                NULL,
                $preferredCulture);

            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$passwordHash", request.PasswordHash);
        command.Parameters.AddWithValue("$passwordSalt", request.PasswordSalt);
        command.Parameters.AddWithValue("$createdAt", request.CreatedAt.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$preferredCulture", request.PreferredCulture ?? (object)DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void InsertToken(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int userId,
        EmailTokenIssuance token,
        string targetEmail)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO UserEmailTokens (
                UserId,
                Purpose,
                TokenHash,
                TargetEmail,
                Culture,
                CreatedAtUtc,
                ExpiresAtUtc)
            VALUES (
                $userId,
                $purpose,
                $tokenHash,
                $targetEmail,
                $culture,
                $createdAtUtc,
                $expiresAtUtc);
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$purpose", (int)token.Purpose);
        command.Parameters.AddWithValue("$tokenHash", token.TokenHash);
        command.Parameters.AddWithValue("$targetEmail", targetEmail);
        command.Parameters.AddWithValue("$culture", token.Culture);
        command.Parameters.AddWithValue("$createdAtUtc", token.CreatedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$expiresAtUtc", token.ExpiresAtUtc.ToUniversalTime().ToString("O"));
        command.ExecuteNonQuery();
    }

    private static bool IsDuplicateEmail(SqliteException exception)
    {
        return exception.SqliteErrorCode == 19 &&
            exception.Message.Contains("Users.Email", StringComparison.Ordinal);
    }
}
