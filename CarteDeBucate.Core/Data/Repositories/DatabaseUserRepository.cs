using Microsoft.Data.Sqlite;

public class DatabaseUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public DatabaseUserRepository(string databasePath)
    {
        _connectionString = SqliteConnectionStringFactory.Create(databasePath);
    }

    public void Add(User user)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Username, PasswordHash, PasswordSalt, CreatedAt)
            VALUES ($username, $hash, $salt, $createdAt);

            SELECT last_insert_rowid();
        """;

        command.Parameters.AddWithValue("$username", user.Username);
        command.Parameters.AddWithValue("$hash", user.PasswordHash);
        command.Parameters.AddWithValue("$salt", user.PasswordSalt);
        command.Parameters.AddWithValue("$createdAt", user.CreatedAt.ToString("O"));

        long userId = (long)(command.ExecuteScalar() ?? 0);
        user.Id = (int)userId;
    }

    public User? GetByUsername(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Username, PasswordHash, PasswordSalt, CreatedAt
            FROM Users
            WHERE Username = $username;
        """;

        command.Parameters.AddWithValue("$username", username);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            PasswordSalt = reader.GetString(3),
            CreatedAt = DateTime.Parse(reader.GetString(4))
        };
    }
}
