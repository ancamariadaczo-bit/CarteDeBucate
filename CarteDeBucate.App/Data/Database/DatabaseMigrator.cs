using Microsoft.Data.Sqlite;

public class DatabaseMigrator
{
    private readonly string _connectionString;

    public DatabaseMigrator(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public void ApplyMigrations()
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        EnsureSchemaMigrationsTableExists(connection);

        foreach (DatabaseMigration migration in GetMigrations())
        {
            if (WasMigrationApplied(connection, migration.Version))
            {
                continue;
            }

            if (IsMigrationAlreadyPresentInSchema(connection, migration))
            {
                SaveMigration(connection, migration);
                continue;
            }

            ApplyMigration(connection, migration);
        }
    }

    private static List<DatabaseMigration> GetMigrations()
    {
        return new List<DatabaseMigration>
        {
            new DatabaseMigration(1, "Add recipe status column", DatabaseScripts.AddRecipeStatusColumn)
        };
    }

    private static void EnsureSchemaMigrationsTableExists(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = DatabaseScripts.CreateSchemaMigrationsTable;
        command.ExecuteNonQuery();
    }

    private static bool WasMigrationApplied(SqliteConnection connection, int version)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM SchemaMigrations
            WHERE Version = @Version;
            """;

        command.Parameters.AddWithValue("@Version", version);

        long count = (long)command.ExecuteScalar()!;

        return count > 0;
    }

    private static bool IsMigrationAlreadyPresentInSchema(SqliteConnection connection, DatabaseMigration migration)
    {
        return migration.Version switch
        {
            1 => ColumnExists(connection, "Recipes", "Status"),
            _ => false
        };
    }

    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            string existingColumnName = reader.GetString(reader.GetOrdinal("name"));

            if (existingColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyMigration(SqliteConnection connection, DatabaseMigration migration)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            using SqliteCommand migrationCommand = connection.CreateCommand();
            migrationCommand.Transaction = transaction;
            migrationCommand.CommandText = migration.Sql;
            migrationCommand.ExecuteNonQuery();

            SaveMigration(connection, migration, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void SaveMigration(
        SqliteConnection connection,
        DatabaseMigration migration,
        SqliteTransaction? transaction = null)
    {
        using SqliteCommand saveMigrationCommand = connection.CreateCommand();
        saveMigrationCommand.Transaction = transaction;
        saveMigrationCommand.CommandText = """
            INSERT INTO SchemaMigrations (Version, Name, AppliedAt)
            VALUES (@Version, @Name, @AppliedAt);
            """;

        saveMigrationCommand.Parameters.AddWithValue("@Version", migration.Version);
        saveMigrationCommand.Parameters.AddWithValue("@Name", migration.Name);
        saveMigrationCommand.Parameters.AddWithValue("@AppliedAt", DateTime.UtcNow.ToString("O"));

        saveMigrationCommand.ExecuteNonQuery();
    }
}
