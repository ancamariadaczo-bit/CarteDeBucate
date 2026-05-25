public class FakeRecipeBackupService : IRecipeBackupService
{
    public RecipeBackupResult ExportResultToReturn { get; set; } = new()
    {
        IsSuccess = true,
        Message = "Backup exported successfully."
    };

    public RecipeBackupResult ImportResultToReturn { get; set; } = new()
    {
        IsSuccess = true,
        Message = "Backup import completed."
    };

    public bool ExportToJsonWasCalled { get; private set; }
    public bool ImportFromJsonWasCalled { get; private set; }

    public string? FilePathPassedToExportToJson { get; private set; }
    public string? FilePathPassedToImportFromJson { get; private set; }

    public RecipeBackupResult ExportToJson(string backupFilePath)
    {
        ExportToJsonWasCalled = true;
        FilePathPassedToExportToJson = backupFilePath;

        return ExportResultToReturn;
    }

    public RecipeBackupResult ImportFromJson(string backupFilePath)
    {
        ImportFromJsonWasCalled = true;
        FilePathPassedToImportFromJson = backupFilePath;

        return ImportResultToReturn;
    }
}