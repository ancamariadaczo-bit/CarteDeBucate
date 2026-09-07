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
            Assert.True(MigrationWasRecorded(databasePath, 2));
            Assert.True(MigrationWasRecorded(databasePath, 3));
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
            Assert.True(ColumnExists(databasePath, "Recipes", "UserId"));
            Assert.True(TableExists(databasePath, "Users"));
            Assert.True(MigrationWasRecorded(databasePath, 1));
            Assert.True(MigrationWasRecorded(databasePath, 2));
            Assert.True(MigrationWasRecorded(databasePath, 3));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void ApplyMigrations_WhenOldDatabaseHasNoRecipePhotos_ShouldCreateTableAndIndex()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            CreateOldRecipesTable(databasePath);

            DatabaseMigrator migrator = new DatabaseMigrator(databasePath);
            migrator.ApplyMigrations();

            Assert.True(TableExists(databasePath, "RecipePhotos"));
            Assert.True(IndexExists(
                databasePath,
                "IX_RecipePhotos_RecipeId_DisplayOrder_Id"));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void ApplyMigrations_WhenRecipePhotosTableAlreadyExists_ShouldRecordMigration4()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();

            DatabaseMigrator migrator = new DatabaseMigrator(databasePath);
            migrator.ApplyMigrations();

            Assert.True(MigrationWasRecorded(databasePath, 4));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void ApplyMigrations_WhenRunTwice_ShouldRecordMigration4Once()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            CreateOldRecipesTable(databasePath);

            DatabaseMigrator migrator = new DatabaseMigrator(databasePath);
            migrator.ApplyMigrations();
            migrator.ApplyMigrations();

            Assert.Equal(1L, MigrationRecordCount(databasePath, 4));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void ApplyMigrations_WhenRecipesExist_ShouldPreserveExistingRecipes()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            CreateOldRecipesTable(databasePath);
            int recipeId = InsertOldRecipe(databasePath);

            DatabaseMigrator migrator = new DatabaseMigrator(databasePath);
            migrator.ApplyMigrations();

            AssertOldRecipeWasPreserved(databasePath, recipeId);
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

    private static int InsertOldRecipe(string databasePath)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Recipes (Name, SourceUrl, SavedAt, Notes)
            VALUES (@Name, @SourceUrl, @SavedAt, @Notes);

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("@Name", "Existing recipe");
        command.Parameters.AddWithValue("@SourceUrl", "https://example.com/existing-recipe");
        command.Parameters.AddWithValue("@SavedAt", "2026-09-01T10:00:00.0000000Z");
        command.Parameters.AddWithValue("@Notes", "Existing notes");

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void AssertOldRecipeWasPreserved(string databasePath, int recipeId)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Name, SourceUrl, SavedAt, Notes
            FROM Recipes
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", recipeId);

        using SqliteDataReader reader = command.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal("Existing recipe", reader.GetString(0));
        Assert.Equal("https://example.com/existing-recipe", reader.GetString(1));
        Assert.Equal("2026-09-01T10:00:00.0000000Z", reader.GetString(2));
        Assert.Equal("Existing notes", reader.GetString(3));
        Assert.False(reader.Read());
    }

    private static bool MigrationWasRecorded(string databasePath, int version)
    {
        return MigrationRecordCount(databasePath, version) == 1;
    }

    private static long MigrationRecordCount(string databasePath, int version)
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

        return count;
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

    private static bool TableExists(string databasePath, string tableName)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table'
            AND name = @TableName;
            """;
        command.Parameters.AddWithValue("@TableName", tableName);

        long count = (long)(command.ExecuteScalar() ?? 0);

        return count == 1;
    }

    private static bool IndexExists(string databasePath, string indexName)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'index'
            AND name = @IndexName;
            """;
        command.Parameters.AddWithValue("@IndexName", indexName);

        long count = (long)(command.ExecuteScalar() ?? 0);

        return count == 1;
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
