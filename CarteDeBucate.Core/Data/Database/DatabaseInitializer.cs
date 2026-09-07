using Microsoft.Data.Sqlite;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string databasePath)
    {
        _connectionString = SqliteConnectionStringFactory.Create(databasePath);
    }

    public void Initialize()
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (string script in DatabaseScripts.CreateTables)
        {
            ExecuteNonQuery(connection, script);
        }
    }

    private void ExecuteNonQuery(SqliteConnection connection, string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }
}
