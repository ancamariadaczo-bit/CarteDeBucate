public class RecipeBackupResult
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;

    public int ExportedCount { get; set; }

    public int ImportedCount { get; set; }

    public int SkippedCount { get; set; }
}