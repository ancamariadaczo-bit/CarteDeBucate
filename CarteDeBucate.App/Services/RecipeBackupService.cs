using System.Text.Json;

public class RecipeBackupService : IRecipeBackupService
{
    private readonly IRecipeRepository _recipeRepository;

    public RecipeBackupService(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public RecipeBackupResult ExportToJson(string backupFilePath)
    {
        try
        {
            List<Recipe> recipes = _recipeRepository.GetAllRecipes();

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(recipes, options);

            File.WriteAllText(backupFilePath, json);

            return new RecipeBackupResult
            {
                IsSuccess = true,
                Message = string.Format(AppTexts.BackupExportCompleted, recipes.Count),
                ExportedCount = recipes.Count
            };
        }
        catch (Exception exception)
        {
            return new RecipeBackupResult
            {
                IsSuccess = false,
                Message = string.Format(AppTexts.BackupExportFailed, exception.Message)
            };
        }
    }

    public RecipeBackupResult ImportFromJson(string backupFilePath)
    {
        try
        {
            if (!File.Exists(backupFilePath))
            {
                return new RecipeBackupResult
                {
                    IsSuccess = false,
                    Message = AppTexts.BackupFileNotFound
                };
            }

            string json = File.ReadAllText(backupFilePath);

            List<Recipe>? recipes = JsonSerializer.Deserialize<List<Recipe>>(json);

            if (recipes == null)
            {
                return new RecipeBackupResult
                {
                    IsSuccess = false,
                    Message = AppTexts.InvalidBackupFile
                };
            }

            int importedCount = 0;
            int skippedCount = 0;

            foreach (Recipe recipe in recipes)
            {
                bool alreadyExists =
                    !string.IsNullOrWhiteSpace(recipe.SourceUrl)
                    && _recipeRepository.RecipeExistsBySourceUrl(recipe.SourceUrl);

                if (alreadyExists)
                {
                    skippedCount++;
                    continue;
                }

                recipe.Id = 0;
                _recipeRepository.AddRecipe(recipe);
                importedCount++;

            }

            return new RecipeBackupResult
            {
                IsSuccess = true,
                Message = string.Format(AppTexts.BackupImportCompleted, importedCount, skippedCount),
                ImportedCount = importedCount,
                SkippedCount = skippedCount
            };
        }
        catch (Exception exception)
        {
            return new RecipeBackupResult
            {
                IsSuccess = false,
                Message = string.Format(AppTexts.BackupImportFailed, exception.Message)
            };
        }
    }
}