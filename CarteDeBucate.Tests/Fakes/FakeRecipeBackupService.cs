public class FakeRecipeBackupService : IRecipeBackupService
{
    public RecipeBackupResult ExportResultToReturn { get; set; } = new()
    {
        IsSuccess = true,
        Message = "Backup exported successfully."
    };

    public RecipeBackupExportResult ExportContentResultToReturn { get; set; } = new()
    {
        IsSuccess = true,
        Message = "Backup exported successfully.",
        Json = "[]"
    };

    public RecipeBackupResult ImportResultToReturn { get; set; } = new()
    {
        IsSuccess = true,
        Message = "Backup import completed."
    };

    public bool ExportToJsonWasCalled { get; private set; }
    public bool ExportToJsonContentWasCalled { get; private set; }
    public bool ImportFromJsonWasCalled { get; private set; }
    public bool ImportFromJsonContentWasCalled { get; private set; }

    public string? FilePathPassedToExportToJson { get; private set; }
    public string? FilePathPassedToImportFromJson { get; private set; }
    public string? JsonPassedToImportFromJsonContent { get; private set; }

    public RecipeBackupResult ExportToJson(string backupFilePath)
    {
        ExportToJsonWasCalled = true;
        FilePathPassedToExportToJson = backupFilePath;

        return ExportResultToReturn;
    }

    public RecipeBackupExportResult ExportToJsonContent()
    {
        ExportToJsonContentWasCalled = true;

        return ExportContentResultToReturn;
    }

    public RecipeBackupResult ImportFromJson(string backupFilePath)
    {
        ImportFromJsonWasCalled = true;
        FilePathPassedToImportFromJson = backupFilePath;

        return ImportResultToReturn;
    }

    public RecipeBackupResult ImportFromJsonContent(string json)
    {
        ImportFromJsonContentWasCalled = true;
        JsonPassedToImportFromJsonContent = json;

        return ImportResultToReturn;
    }
}
