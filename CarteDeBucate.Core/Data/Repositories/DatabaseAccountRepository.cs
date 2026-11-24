using Microsoft.Data.Sqlite;
using System.Globalization;

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
        ArgumentNullException.ThrowIfNull(request.VerificationToken);

        string email = EmailNormalizer.Normalize(request.Email);
        EmailTokenIssuance verificationToken = request.VerificationToken;
        string targetEmail = ValidateAndNormalizeToken(verificationToken);

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

        TryCleanUpOldTokens();

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
        ArgumentNullException.ThrowIfNull(token);
        string targetEmail = ValidateAndNormalizeToken(token);

        TryCleanUpOldTokens();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            if (!UserExists(connection, transaction, userId))
            {
                transaction.Rollback();
                return new EmailTokenReplacementResult(
                    EmailTokenReplacementResultCode.UserNotFound);
            }

            RevokeActiveTokens(connection, transaction, userId, token.Purpose, token.CreatedAtUtc);
            InsertToken(connection, transaction, userId, token, targetEmail);

            transaction.Commit();
            return new EmailTokenReplacementResult(EmailTokenReplacementResultCode.Success);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public EmailVerificationReviewResult ReviewRegistrationEmail(
        byte[] tokenHash,
        DateTime reviewedAtUtc)
    {
        if (tokenHash is null || tokenHash.Length != 32)
        {
            return new EmailVerificationReviewResult(
                EmailVerificationReviewResultCode.InvalidToken);
        }

        DateTime reviewTime = reviewedAtUtc.ToUniversalTime();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();
        UserEmailTokenRecord? token = ReadToken(
            connection,
            transaction,
            tokenHash,
            EmailTokenPurpose.RegistrationVerification);

        transaction.Commit();

        if (token is null || token.UsedAtUtc is not null || token.RevokedAtUtc is not null)
        {
            return new EmailVerificationReviewResult(
                EmailVerificationReviewResultCode.InvalidToken);
        }

        if (token.ExpiresAtUtc <= reviewTime)
        {
            return new EmailVerificationReviewResult(
                EmailVerificationReviewResultCode.ExpiredToken);
        }

        return new EmailVerificationReviewResult(
            EmailVerificationReviewResultCode.Valid);
    }

    public EmailVerificationResult ConfirmRegistrationEmail(byte[] tokenHash, DateTime confirmedAtUtc)
    {
        if (tokenHash is null || tokenHash.Length != 32)
        {
            return new EmailVerificationResult(EmailVerificationResultCode.InvalidToken);
        }

        DateTime confirmationTime = confirmedAtUtc.ToUniversalTime();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            UserEmailTokenRecord? token = ReadToken(
                connection,
                transaction,
                tokenHash,
                EmailTokenPurpose.RegistrationVerification);

            if (token is null || token.UsedAtUtc is not null || token.RevokedAtUtc is not null)
            {
                transaction.Rollback();
                return new EmailVerificationResult(EmailVerificationResultCode.InvalidToken);
            }

            if (token.ExpiresAtUtc <= confirmationTime)
            {
                transaction.Rollback();
                return new EmailVerificationResult(EmailVerificationResultCode.ExpiredToken);
            }

            if (MarkTokenUsed(connection, transaction, token.Id, confirmationTime) != 1 ||
                MarkUserEmailVerified(
                    connection,
                    transaction,
                    token.UserId,
                    token.TargetEmail,
                    confirmationTime) != 1)
            {
                transaction.Rollback();
                return new EmailVerificationResult(EmailVerificationResultCode.InvalidToken);
            }

            transaction.Commit();
            return new EmailVerificationResult(EmailVerificationResultCode.Success);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public EmailChangeRequestResult RequestEmailChange(int userId, EmailTokenIssuance token)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (token.Purpose != EmailTokenPurpose.EmailChangeVerification)
        {
            throw new ArgumentException(
                "Email-change requests require an email-change verification token.",
                nameof(token));
        }

        string targetEmail = ValidateAndNormalizeToken(token);

        TryCleanUpOldTokens();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            if (!UserExists(connection, transaction, userId))
            {
                transaction.Rollback();
                return new EmailChangeRequestResult(EmailChangeRequestResultCode.UserNotFound);
            }

            RevokeActiveTokens(connection, transaction, userId, token.Purpose, token.CreatedAtUtc);
            InsertToken(connection, transaction, userId, token, targetEmail);

            transaction.Commit();
            return new EmailChangeRequestResult(EmailChangeRequestResultCode.Success);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public EmailChangeConfirmationResult ConfirmEmailChange(byte[] tokenHash, DateTime confirmedAtUtc)
    {
        if (tokenHash is null || tokenHash.Length != 32)
        {
            return new EmailChangeConfirmationResult(
                EmailChangeConfirmationResultCode.InvalidToken);
        }

        DateTime confirmationTime = confirmedAtUtc.ToUniversalTime();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            UserEmailTokenRecord? token = ReadToken(
                connection,
                transaction,
                tokenHash,
                EmailTokenPurpose.EmailChangeVerification);

            if (token is null || token.UsedAtUtc is not null || token.RevokedAtUtc is not null)
            {
                transaction.Rollback();
                return new EmailChangeConfirmationResult(
                    EmailChangeConfirmationResultCode.InvalidToken);
            }

            if (token.ExpiresAtUtc <= confirmationTime)
            {
                transaction.Rollback();
                return new EmailChangeConfirmationResult(
                    EmailChangeConfirmationResultCode.ExpiredToken);
            }

            if (EmailBelongsToDifferentUser(
                    connection,
                    transaction,
                    token.UserId,
                    token.TargetEmail))
            {
                transaction.Rollback();
                return new EmailChangeConfirmationResult(
                    EmailChangeConfirmationResultCode.DuplicateEmail);
            }

            if (UpdateUserEmail(connection, transaction, token.UserId, token.TargetEmail, confirmationTime) != 1 ||
                MarkTokenUsed(connection, transaction, token.Id, confirmationTime) != 1)
            {
                transaction.Rollback();
                return new EmailChangeConfirmationResult(
                    EmailChangeConfirmationResultCode.InvalidToken);
            }

            RevokeActiveTokens(
                connection,
                transaction,
                token.UserId,
                EmailTokenPurpose.EmailChangeVerification,
                confirmationTime);

            transaction.Commit();
            return new EmailChangeConfirmationResult(EmailChangeConfirmationResultCode.Success);
        }
        catch (SqliteException exception) when (IsDuplicateEmail(exception))
        {
            transaction.Rollback();
            return new EmailChangeConfirmationResult(
                EmailChangeConfirmationResultCode.DuplicateEmail);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void TryCleanUpOldTokens()
    {
        try
        {
            using SqliteConnection connection = new SqliteConnection(_connectionString);
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM UserEmailTokens
                WHERE UsedAtUtc < $cutoffUtc
                   OR RevokedAtUtc < $cutoffUtc
                   OR ExpiresAtUtc < $cutoffUtc;
                """;
            command.Parameters.AddWithValue(
                "$cutoffUtc",
                DateTime.UtcNow.AddDays(-30).ToString("O"));
            command.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Cleanup is best effort; token issuance must remain available.
        }
    }

    private static string ValidateAndNormalizeToken(EmailTokenIssuance token)
    {
        if (token.Purpose is not EmailTokenPurpose.RegistrationVerification
            and not EmailTokenPurpose.EmailChangeVerification)
        {
            throw new ArgumentException("The token purpose is invalid.", nameof(token));
        }

        if (token.TokenHash is null || token.TokenHash.Length != 32)
        {
            throw new ArgumentException(
                "The verification token hash must contain 32 bytes.",
                nameof(token));
        }

        if (string.IsNullOrWhiteSpace(token.Culture))
        {
            throw new ArgumentException("The token culture is required.", nameof(token));
        }

        return EmailNormalizer.Normalize(token.TargetEmail);
    }

    private static bool UserExists(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int userId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM Users WHERE Id = $userId);";
        command.Parameters.AddWithValue("$userId", userId);

        return Convert.ToInt64(command.ExecuteScalar()) == 1;
    }

    private static void RevokeActiveTokens(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int userId,
        EmailTokenPurpose purpose,
        DateTime revokedAtUtc)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE UserEmailTokens
            SET RevokedAtUtc = $revokedAtUtc
            WHERE UserId = $userId
              AND Purpose = $purpose
              AND UsedAtUtc IS NULL
              AND RevokedAtUtc IS NULL;
            """;
        command.Parameters.AddWithValue("$revokedAtUtc", revokedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$purpose", (int)purpose);
        command.ExecuteNonQuery();
    }

    private static UserEmailTokenRecord? ReadToken(
        SqliteConnection connection,
        SqliteTransaction transaction,
        byte[] tokenHash,
        EmailTokenPurpose purpose)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                tokens.Id,
                tokens.UserId,
                tokens.TargetEmail,
                tokens.ExpiresAtUtc,
                tokens.UsedAtUtc,
                tokens.RevokedAtUtc
            FROM UserEmailTokens AS tokens
            WHERE tokens.TokenHash = $tokenHash
              AND tokens.Purpose = $purpose;
            """;
        command.Parameters.AddWithValue("$tokenHash", tokenHash);
        command.Parameters.AddWithValue("$purpose", (int)purpose);

        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new UserEmailTokenRecord(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            DateTime.Parse(reader.GetString(3), null, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(4)
                ? null
                : DateTime.Parse(reader.GetString(4), null, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(5)
                ? null
                : DateTime.Parse(reader.GetString(5), null, DateTimeStyles.RoundtripKind));
    }

    private static int MarkTokenUsed(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int tokenId,
        DateTime usedAtUtc)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE UserEmailTokens
            SET UsedAtUtc = $usedAtUtc
            WHERE Id = $tokenId
              AND UsedAtUtc IS NULL
              AND RevokedAtUtc IS NULL;
            """;
        command.Parameters.AddWithValue("$usedAtUtc", usedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$tokenId", tokenId);
        return command.ExecuteNonQuery();
    }

    private static int MarkUserEmailVerified(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int userId,
        string targetEmail,
        DateTime verifiedAtUtc)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Users
            SET EmailVerifiedAtUtc = $verifiedAtUtc
            WHERE Id = $userId
              AND Email COLLATE NOCASE = $targetEmail;
            """;
        command.Parameters.AddWithValue("$verifiedAtUtc", verifiedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$targetEmail", targetEmail);
        return command.ExecuteNonQuery();
    }

    private static bool EmailBelongsToDifferentUser(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int userId,
        string email)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM Users
                WHERE Email COLLATE NOCASE = $email
                  AND Id <> $userId);
            """;
        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$userId", userId);

        return Convert.ToInt64(command.ExecuteScalar()) == 1;
    }

    private static int UpdateUserEmail(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int userId,
        string email,
        DateTime verifiedAtUtc)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Users
            SET Email = $email,
                EmailVerifiedAtUtc = $verifiedAtUtc
            WHERE Id = $userId;
            """;
        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$verifiedAtUtc", verifiedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$userId", userId);
        return command.ExecuteNonQuery();
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

    private sealed record UserEmailTokenRecord(
        int Id,
        int UserId,
        string TargetEmail,
        DateTime ExpiresAtUtc,
        DateTime? UsedAtUtc,
        DateTime? RevokedAtUtc);
}
