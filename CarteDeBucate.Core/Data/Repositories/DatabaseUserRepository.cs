using Microsoft.Data.Sqlite;
using System.Globalization;

public class DatabaseUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public DatabaseUserRepository(string databasePath)
    {
        _connectionString = SqliteConnectionStringFactory.Create(databasePath);
    }

    public void Add(User user)
    {
        user.Email = EmailNormalizer.Normalize(user.Email);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (
                Email,
                PasswordHash,
                PasswordSalt,
                CreatedAt,
                EmailVerifiedAtUtc,
                PreferredCulture)
            VALUES ($email, $hash, $salt, $createdAt, $emailVerifiedAtUtc, $preferredCulture);

            SELECT last_insert_rowid();
        """;

        command.Parameters.AddWithValue("$email", user.Email);
        command.Parameters.AddWithValue("$hash", user.PasswordHash);
        command.Parameters.AddWithValue("$salt", user.PasswordSalt);
        command.Parameters.AddWithValue("$createdAt", user.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue(
            "$emailVerifiedAtUtc",
            user.EmailVerifiedAtUtc?.ToUniversalTime().ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(
            "$preferredCulture",
            user.PreferredCulture ?? (object)DBNull.Value);

        long userId = (long)(command.ExecuteScalar() ?? 0);
        user.Id = (int)userId;
    }

    public User? GetByEmail(string email)
    {
        string normalizedEmail = EmailNormalizer.Normalize(email);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Email, PasswordHash, PasswordSalt, CreatedAt, EmailVerifiedAtUtc, PreferredCulture,
                   SecurityVersion
            FROM Users
            WHERE Email COLLATE NOCASE = $email;
        """;

        command.Parameters.AddWithValue("$email", normalizedEmail);

        using var reader = command.ExecuteReader();

        return ReadUser(reader);
    }

    public User? GetById(int userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Email, PasswordHash, PasswordSalt, CreatedAt, EmailVerifiedAtUtc, PreferredCulture,
                   SecurityVersion
            FROM Users
            WHERE Id = $userId;
        """;
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();

        return ReadUser(reader);
    }

    public long? GetSecurityVersion(int userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT SecurityVersion
            FROM Users
            WHERE Id = $userId;
        """;
        command.Parameters.AddWithValue("$userId", userId);

        object? result = command.ExecuteScalar();

        return result is null ? null : (long)result;
    }

    public bool IncrementSecurityVersion(int userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET SecurityVersion = SecurityVersion + 1
            WHERE Id = $userId;
        """;
        command.Parameters.AddWithValue("$userId", userId);

        return command.ExecuteNonQuery() == 1;
    }

    public bool UpdatePreferredCulture(int userId, string preferredCulture)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET PreferredCulture = $preferredCulture
            WHERE Id = $userId;
        """;
        command.Parameters.AddWithValue("$preferredCulture", preferredCulture);
        command.Parameters.AddWithValue("$userId", userId);

        return command.ExecuteNonQuery() == 1;
    }

    private static User? ReadUser(SqliteDataReader reader)
    {
        if (!reader.Read())
        {
            return null;
        }

        return new User
        {
            Id = reader.GetInt32(0),
            Email = EmailNormalizer.Normalize(reader.GetString(1)),
            PasswordHash = reader.GetString(2),
            PasswordSalt = reader.GetString(3),
            CreatedAt = DateTime.Parse(reader.GetString(4), null, DateTimeStyles.RoundtripKind),
            EmailVerifiedAtUtc = reader.IsDBNull(5)
                ? null
                : DateTime.Parse(reader.GetString(5), null, DateTimeStyles.RoundtripKind),
            PreferredCulture = reader.IsDBNull(6) ? null : reader.GetString(6),
            SecurityVersion = reader.GetInt64(7)
        };
    }
}
