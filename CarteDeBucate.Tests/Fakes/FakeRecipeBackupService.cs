public class FakeRecipeBackupService : IRecipeBackupService
{
    public RecipeSaveResult ExportResultToReturn { get; set; } = new()
    {
        IsSuccess = true,
        Message = "Backup exported successfully."
    };

    public RecipeSaveResult ImportResultToReturn { get; set; } = new()
    {
        IsSuccess = true,
        Message = "Backup import completed."
    };

    public bool ExportToJsonWasCalled { get; private set; }
    public bool ImportFromJsonWasCalled { get; private set; }

    public string? FilePathPassedToExportToJson { get; private set; }
    public string? FilePathPassedToImportFromJson { get; private set; }

    public RecipeSaveResult ExportToJson(string backupFilePath)
    {
        ExportToJsonWasCalled = true;
        FilePathPassedToExportToJson = backupFilePath;

        return ExportResultToReturn;
    }

    public RecipeSaveResult ImportFromJson(string backupFilePath)
    {
        ImportFromJsonWasCalled = true;
        FilePathPassedToImportFromJson = backupFilePath;

        return ImportResultToReturn;
    }
}