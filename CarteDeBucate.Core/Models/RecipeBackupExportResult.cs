public class RecipeBackupExportResult
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;

    public int ExportedCount { get; set; }

    public string Json { get; set; } = string.Empty;
}
