using Microsoft.Data.Sqlite;

public class DatabaseInitializerTests
{
    [Fact]
    public void Initialize_ShouldAllowNullRecipeSourceUrl()
    {
        WithInitializedDatabase(databasePath =>
        {
            Dictionary<string, ColumnDefinition> columns = ReadColumns(databasePath, "Recipes");

            Assert.False(columns["SourceUrl"].IsNotNull);
        });
    }

    [Fact]
    public void Initialize_ShouldCreateRecipePhotosTable()
    {
        WithInitializedDatabase(databasePath =>
        {
            Assert.True(TableExists(databasePath, "RecipePhotos"));
        });
    }

    [Fact]
    public void Initialize_WhenCalledTwice_ShouldNotThrow()
    {
        WithTemporaryDatabase(databasePath =>
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);

            initializer.Initialize();
            initializer.Initialize();

            Assert.True(TableExists(databasePath, "RecipePhotos"));
        });
    }

    [Fact]
    public void Initialize_ShouldCreateExpectedUserColumns()
    {
        WithInitializedDatabase(databasePath =>
        {
            Dictionary<string, ColumnDefinition> columns = ReadColumns(databasePath, "Users");

            Assert.Equal(8, columns.Count);
            AssertColumn(columns, "Id", "INTEGER", false, null, 1);
            AssertColumn(columns, "Email", "TEXT", true, null, 0);
            AssertColumn(columns, "PasswordHash", "TEXT", true, null, 0);
            AssertColumn(columns, "PasswordSalt", "TEXT", true, null, 0);
            AssertColumn(columns, "CreatedAt", "TEXT", true, null, 0);
            AssertColumn(columns, "EmailVerifiedAtUtc", "TEXT", false, null, 0);
            AssertColumn(columns, "PreferredCulture", "TEXT", false, null, 0);
            AssertColumn(columns, "SecurityVersion", "INTEGER", true, "1", 0);
            Assert.DoesNotContain("Username", columns.Keys);
        });
    }

    [Fact]
    public void Initialize_ShouldDefaultNewUsersSecurityVersionToOne()
    {
        WithInitializedDatabase(databasePath =>
        {
            using SqliteConnection connection = OpenConnection(databasePath);
            int userId = InsertUserAndGetId(connection, "anca@example.com");

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SecurityVersion FROM Users WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", userId);

            Assert.Equal(1L, (long)command.ExecuteScalar()!);
        });
    }

    [Fact]
    public void Initialize_ShouldEnforceCaseInsensitiveUniqueEmail()
    {
        WithInitializedDatabase(databasePath =>
        {
            using SqliteConnection connection = OpenConnection(databasePath);

            Assert.True(IndexExists(connection, "IX_Users_Email_NoCase"));

            InsertUser(connection, "anca@example.com");

            Assert.Throws<SqliteException>(() => InsertUser(connection, "AnCa@Example.com"));
        });
    }

    [Fact]
    public void Initialize_ShouldCreateExpectedUserEmailTokenColumns()
    {
        WithInitializedDatabase(databasePath =>
        {
            Dictionary<string, ColumnDefinition> columns =
                ReadColumns(databasePath, "UserEmailTokens");

            Assert.Equal(10, columns.Count);
            AssertColumn(columns, "Id", "INTEGER", false, null, 1);
            AssertColumn(columns, "UserId", "INTEGER", true, null, 0);
            AssertColumn(columns, "Purpose", "INTEGER", true, null, 0);
            AssertColumn(columns, "TokenHash", "BLOB", true, null, 0);
            AssertColumn(columns, "TargetEmail", "TEXT", true, null, 0);
            AssertColumn(columns, "Culture", "TEXT", true, null, 0);
            AssertColumn(columns, "CreatedAtUtc", "TEXT", true, null, 0);
            AssertColumn(columns, "ExpiresAtUtc", "TEXT", true, null, 0);
            AssertColumn(columns, "UsedAtUtc", "TEXT", false, null, 0);
            AssertColumn(columns, "RevokedAtUtc", "TEXT", false, null, 0);
        });
    }

    [Fact]
    public void Initialize_ShouldCreateUserEmailTokenIndexes()
    {
        WithInitializedDatabase(databasePath =>
        {
            using SqliteConnection connection = OpenConnection(databasePath);

            Assert.True(IndexExists(connection, "IX_UserEmailTokens_TokenHash"));
            Assert.True(IndexExists(connection, "IX_UserEmailTokens_ActivePurpose"));
            Assert.True(IndexExists(connection, "IX_UserEmailTokens_UserPurposeExpiry"));
        });
    }

    [Fact]
    public void Initialize_ShouldEnforceUserEmailTokenConstraints()
    {
        WithInitializedDatabase(databasePath =>
        {
            using SqliteConnection connection = OpenConnection(databasePath);
            int userId = InsertUserAndGetId(connection, "anca@example.com");
            int secondUserId = InsertUserAndGetId(connection, "maria@example.com");
            byte[] tokenHash = CreateTokenHash(1);

            InsertUserEmailToken(connection, userId, 1, tokenHash);

            Assert.Throws<SqliteException>(() =>
                InsertUserEmailToken(connection, secondUserId, 2, tokenHash));
            Assert.Throws<SqliteException>(() =>
                InsertUserEmailToken(connection, userId, 1, CreateTokenHash(2)));
            Assert.Throws<SqliteException>(() =>
                InsertUserEmailToken(connection, userId, 3, CreateTokenHash(3)));
            Assert.Throws<SqliteException>(() =>
                InsertUserEmailToken(connection, userId, 2, new byte[31]));
        });
    }

    [Fact]
    public void Initialize_ShouldDeclareUserEmailTokenForeignKeyWithCascadeDelete()
    {
        WithInitializedDatabase(databasePath =>
        {
            using SqliteConnection connection = OpenConnection(databasePath);
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_key_list(UserEmailTokens);";

            using SqliteDataReader reader = command.ExecuteReader();

            Assert.True(reader.Read());
            Assert.Equal("Users", reader.GetString(reader.GetOrdinal("table")));
            Assert.Equal("UserId", reader.GetString(reader.GetOrdinal("from")));
            Assert.Equal("Id", reader.GetString(reader.GetOrdinal("to")));
            Assert.Equal("CASCADE", reader.GetString(reader.GetOrdinal("on_delete")));
            Assert.False(reader.Read());
        });
    }

    [Fact]
    public void Initialize_ShouldCreateExpectedRecipePhotosColumns()
    {
        WithInitializedDatabase(databasePath =>
        {
            Dictionary<string, ColumnDefinition> columns = ReadColumns(databasePath, "RecipePhotos");

            Assert.Equal(9, columns.Count);
            AssertColumn(columns, "Id", "INTEGER", false, null, 1);
            AssertColumn(columns, "RecipeId", "INTEGER", true, null, 0);
            AssertColumn(columns, "StorageFileName", "TEXT", true, null, 0);
            AssertColumn(columns, "OriginalFileName", "TEXT", true, null, 0);
            AssertColumn(columns, "ContentType", "TEXT", true, null, 0);
            AssertColumn(columns, "FileSize", "INTEGER", true, null, 0);
            AssertColumn(columns, "CreatedAtUtc", "TEXT", true, null, 0);
            AssertColumn(columns, "DisplayOrder", "INTEGER", true, "0", 0);
            AssertColumn(columns, "Origin", "INTEGER", true, "0", 0);
        });
    }

    [Fact]
    public void Initialize_ShouldCreateRecipePhotosIndex()
    {
        WithInitializedDatabase(databasePath =>
        {
            using SqliteConnection connection = OpenConnection(databasePath);

            using SqliteCommand indexCommand = connection.CreateCommand();
            indexCommand.CommandText = "PRAGMA index_list(RecipePhotos);";

            using SqliteDataReader indexReader = indexCommand.ExecuteReader();
            List<string> indexNames = new List<string>();

            while (indexReader.Read())
            {
                indexNames.Add(indexReader.GetString(indexReader.GetOrdinal("name")));
            }

            Assert.Contains("IX_RecipePhotos_RecipeId_DisplayOrder_Id", indexNames);
            indexReader.Close();

            using SqliteCommand columnsCommand = connection.CreateCommand();
            columnsCommand.CommandText =
                "PRAGMA index_info(IX_RecipePhotos_RecipeId_DisplayOrder_Id);";

            using SqliteDataReader columnsReader = columnsCommand.ExecuteReader();
            List<string> indexedColumns = new List<string>();

            while (columnsReader.Read())
            {
                indexedColumns.Add(columnsReader.GetString(columnsReader.GetOrdinal("name")));
            }

            Assert.Equal(new[] { "RecipeId", "DisplayOrder", "Id" }, indexedColumns);
        });
    }

    [Fact]
    public void Initialize_ShouldEnforceUniqueStorageFileName()
    {
        WithInitializedDatabase(databasePath =>
        {
            using SqliteConnection connection = OpenConnection(databasePath);
            int firstRecipeId = InsertRecipe(connection);
            int secondRecipeId = InsertRecipe(connection);

            InsertRecipePhoto(connection, firstRecipeId, "same-photo.jpg");

            Assert.Throws<SqliteException>(() =>
                InsertRecipePhoto(connection, secondRecipeId, "same-photo.jpg"));
        });
    }

    [Fact]
    public void Initialize_ShouldEnforceRecipePhotoValueLimits()
    {
        WithInitializedDatabase(databasePath =>
        {
            using SqliteConnection connection = OpenConnection(databasePath);
            int recipeId = InsertRecipe(connection);

            InsertRecipePhoto(
                connection,
                recipeId,
                "a",
                "b",
                "c",
                fileSize: 1,
                displayOrder: 0);

            InsertRecipePhoto(
                connection,
                recipeId,
                new string('s', 128),
                new string('o', 255),
                new string('c', 100));

            AssertInsertFails(connection, recipeId, " ", "photo.jpg", "image/jpeg");
            AssertInsertFails(connection, recipeId, new string('s', 129), "photo.jpg", "image/jpeg");
            AssertInsertFails(connection, recipeId, "invalid-original-min", " ", "image/jpeg");
            AssertInsertFails(
                connection,
                recipeId,
                "invalid-original-max",
                new string('o', 256),
                "image/jpeg");
            AssertInsertFails(connection, recipeId, "invalid-content-min", "photo.jpg", " ");
            AssertInsertFails(
                connection,
                recipeId,
                "invalid-content-max",
                "photo.jpg",
                new string('c', 101));
            AssertInsertFails(
                connection,
                recipeId,
                "invalid-file-size",
                "photo.jpg",
                "image/jpeg",
                fileSize: 0);
            AssertInsertFails(
                connection,
                recipeId,
                "invalid-display-order",
                "photo.jpg",
                "image/jpeg",
                displayOrder: -1);
        });
    }

    [Fact]
    public void Initialize_ShouldDeclareRecipeForeignKeyWithCascadeDelete()
    {
        WithInitializedDatabase(databasePath =>
        {
            using SqliteConnection connection = OpenConnection(databasePath);
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_key_list(RecipePhotos);";

            using SqliteDataReader reader = command.ExecuteReader();

            Assert.True(reader.Read());
            Assert.Equal("Recipes", reader.GetString(reader.GetOrdinal("table")));
            Assert.Equal("RecipeId", reader.GetString(reader.GetOrdinal("from")));
            Assert.Equal("Id", reader.GetString(reader.GetOrdinal("to")));
            Assert.Equal("CASCADE", reader.GetString(reader.GetOrdinal("on_delete")));
            Assert.False(reader.Read());
        });
    }

    private static void AssertColumn(
        Dictionary<string, ColumnDefinition> columns,
        string name,
        string type,
        bool isNotNull,
        string? defaultValue,
        int primaryKeyOrder)
    {
        Assert.True(columns.TryGetValue(name, out ColumnDefinition? column));
        Assert.NotNull(column);
        Assert.Equal(type, column.Type);
        Assert.Equal(isNotNull, column.IsNotNull);
        Assert.Equal(defaultValue, column.DefaultValue);
        Assert.Equal(primaryKeyOrder, column.PrimaryKeyOrder);
    }

    private static Dictionary<string, ColumnDefinition> ReadColumns(
        string databasePath,
        string tableName)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        using SqliteDataReader reader = command.ExecuteReader();
        Dictionary<string, ColumnDefinition> columns = new Dictionary<string, ColumnDefinition>();

        while (reader.Read())
        {
            string name = reader.GetString(reader.GetOrdinal("name"));
            int defaultValueOrdinal = reader.GetOrdinal("dflt_value");

            columns.Add(
                name,
                new ColumnDefinition(
                    reader.GetString(reader.GetOrdinal("type")),
                    reader.GetInt32(reader.GetOrdinal("notnull")) == 1,
                    reader.IsDBNull(defaultValueOrdinal)
                        ? null
                        : reader.GetString(defaultValueOrdinal),
                    reader.GetInt32(reader.GetOrdinal("pk"))));
        }

        return columns;
    }

    private static bool TableExists(string databasePath, string tableName)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
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

    private static int InsertRecipe(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Recipes (Name, SourceUrl, SavedAt, Notes, Status, UserId)
            VALUES (@Name, @SourceUrl, @SavedAt, NULL, 0, NULL);

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("@Name", "Test recipe");
        command.Parameters.AddWithValue("@SourceUrl", "https://example.com/recipe");
        command.Parameters.AddWithValue("@SavedAt", DateTime.UtcNow.ToString("O"));

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void InsertUser(SqliteConnection connection, string email)
    {
        InsertUserAndGetId(connection, email);
    }

    private static int InsertUserAndGetId(SqliteConnection connection, string email)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Email, PasswordHash, PasswordSalt, CreatedAt)
            VALUES (@Email, @PasswordHash, @PasswordSalt, @CreatedAt);

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("@Email", email);
        command.Parameters.AddWithValue("@PasswordHash", "hash");
        command.Parameters.AddWithValue("@PasswordSalt", "salt");
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void InsertUserEmailToken(
        SqliteConnection connection,
        int userId,
        int purpose,
        byte[] tokenHash)
    {
        using SqliteCommand command = connection.CreateCommand();
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
                @UserId,
                @Purpose,
                @TokenHash,
                @TargetEmail,
                @Culture,
                @CreatedAtUtc,
                @ExpiresAtUtc);
            """;
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Purpose", purpose);
        command.Parameters.AddWithValue("@TokenHash", tokenHash);
        command.Parameters.AddWithValue("@TargetEmail", "anca@example.com");
        command.Parameters.AddWithValue("@Culture", "ro-RO");
        command.Parameters.AddWithValue("@CreatedAtUtc", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("@ExpiresAtUtc", DateTime.UtcNow.AddDays(1).ToString("O"));
        command.ExecuteNonQuery();
    }

    private static byte[] CreateTokenHash(byte marker)
    {
        byte[] tokenHash = new byte[32];
        tokenHash[0] = marker;
        return tokenHash;
    }

    private static bool IndexExists(SqliteConnection connection, string indexName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'index'
            AND name = @IndexName;
            """;
        command.Parameters.AddWithValue("@IndexName", indexName);

        return (long)(command.ExecuteScalar() ?? 0) == 1;
    }

    private static void InsertRecipePhoto(
        SqliteConnection connection,
        int recipeId,
        string storageFileName,
        string originalFileName = "photo.jpg",
        string contentType = "image/jpeg",
        long fileSize = 100,
        int displayOrder = 0)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO RecipePhotos (
                RecipeId,
                StorageFileName,
                OriginalFileName,
                ContentType,
                FileSize,
                CreatedAtUtc,
                DisplayOrder,
                Origin)
            VALUES (
                @RecipeId,
                @StorageFileName,
                @OriginalFileName,
                @ContentType,
                @FileSize,
                @CreatedAtUtc,
                @DisplayOrder,
                @Origin);
            """;

        command.Parameters.AddWithValue("@RecipeId", recipeId);
        command.Parameters.AddWithValue("@StorageFileName", storageFileName);
        command.Parameters.AddWithValue("@OriginalFileName", originalFileName);
        command.Parameters.AddWithValue("@ContentType", contentType);
        command.Parameters.AddWithValue("@FileSize", fileSize);
        command.Parameters.AddWithValue("@CreatedAtUtc", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("@DisplayOrder", displayOrder);
        command.Parameters.AddWithValue("@Origin", (int)RecipePhotoOrigin.UserUpload);

        command.ExecuteNonQuery();
    }

    private static void AssertInsertFails(
        SqliteConnection connection,
        int recipeId,
        string storageFileName,
        string originalFileName,
        string contentType,
        long fileSize = 100,
        int displayOrder = 0)
    {
        Assert.Throws<SqliteException>(() => InsertRecipePhoto(
            connection,
            recipeId,
            storageFileName,
            originalFileName,
            contentType,
            fileSize,
            displayOrder));
    }

    private static SqliteConnection OpenConnection(string databasePath)
    {
        SqliteConnection connection =
            new SqliteConnection(SqliteConnectionStringFactory.Create(databasePath));
        connection.Open();

        return connection;
    }

    private static void WithInitializedDatabase(Action<string> assertion)
    {
        WithTemporaryDatabase(databasePath =>
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();

            assertion(databasePath);
        });
    }

    private static void WithTemporaryDatabase(Action<string> test)
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"recipes-initializer-{Guid.NewGuid()}.db");

        try
        {
            test(databasePath);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private sealed record ColumnDefinition(
        string Type,
        bool IsNotNull,
        string? DefaultValue,
        int PrimaryKeyOrder);
}
