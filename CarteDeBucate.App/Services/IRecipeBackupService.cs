public interface IRecipeBackupService
{
    RecipeSaveResult ExportToJson(string backupFilePath);
    RecipeSaveResult ImportFromJson(string backupFilePath);
}