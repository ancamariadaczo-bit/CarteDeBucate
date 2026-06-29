public interface IRecipeBackupService
{
    RecipeBackupResult ExportToJson(string backupFilePath);
    RecipeBackupExportResult ExportToJsonContent();
    RecipeBackupResult ImportFromJson(string backupFilePath);
    RecipeBackupResult ImportFromJsonContent(string json);
}
