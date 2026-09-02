public class RecipeBackupDocument
{
    public int Version { get; set; } = RecipeBackupVersions.Current;
    public DateTime ExportedAtUtc { get; set; }
    public List<RecipeBackupItem> Recipes { get; set; } = new();
}
