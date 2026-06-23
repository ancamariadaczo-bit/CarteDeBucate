public class DatabaseMigration
{
    public int Version { get; }
    public string Name { get; }
    public string Sql { get; }

    public DatabaseMigration(int version, string name, string sql)
    {
        Version = version;
        Name = name;
        Sql = sql;
    }
}