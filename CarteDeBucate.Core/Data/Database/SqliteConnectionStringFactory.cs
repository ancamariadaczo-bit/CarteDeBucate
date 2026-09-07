using Microsoft.Data.Sqlite;

public static class SqliteConnectionStringFactory
{
    public static string Create(string databasePath)
    {
        SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true
        };

        return builder.ToString();
    }
}
