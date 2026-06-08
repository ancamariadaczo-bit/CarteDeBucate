using Microsoft.Data.Sqlite;

public class DatabaseMigratorTests
{
    [Fact]
    public void ApplyMigrations_WhenStatusColumnAlreadyExists_ShouldRecordMigration()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();

            DatabaseMigrator migrator = new DatabaseMigrator(databasePath);
            migrator.ApplyMigrations();

            Assert.True(MigrationWasRecorded(databasePath, 1));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void ApplyMigrations_WhenStatusColumnIsMissing_ShouldAddStatusColumn()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            CreateOldRecipesTable(databasePath);

            DatabaseMigrator migrator = new DatabaseMigrator(databasePath);
            migrator.ApplyMigrations();

            Assert.True(ColumnExists(databasePath, "Recipes", "Status"));
            Assert.True(MigrationWasRecorded(databasePath, 1));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void Constructor_ShouldStoreMigrationValues()
    {
        DatabaseMigration migration = new DatabaseMigration(7, "Test migration", "SELECT 1;");

        Assert.Equal(7, migration.Version);
        Assert.Equal("Test migration", migration.Name);
        Assert.Equal("SELECT 1;", migration.Sql);
    }

    private static void CreateOldRecipesTable(string databasePath)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Recipes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                SourceUrl TEXT NOT NULL,
                SavedAt TEXT NOT NULL,
                Notes TEXT NULL
            );
            """;

        command.ExecuteNonQuery();
    }

    private static bool MigrationWasRecorded(string databasePath, int version)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM SchemaMigrations
            WHERE Version = @Version;
            """;
        command.Parameters.AddWithValue("@Version", version);

        long count = (long)(command.ExecuteScalar() ?? 0);

        return count == 1;
    }

    private static bool ColumnExists(string databasePath, string tableName, string columnName)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            if (reader.GetString(reader.GetOrdinal("name")) == columnName)
            {
                return true;
            }
        }

        return false;
    }

    private static string CreateTemporaryDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"recipes-{Guid.NewGuid()}.db");
    }

    private static void DeleteDatabaseFile(string databasePath)
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
