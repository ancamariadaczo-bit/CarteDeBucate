public interface IRecipeBackupService
{
    RecipeBackupResult ExportToJson(string backupFilePath);
    RecipeBackupResult ImportFromJson(string backupFilePath);
}