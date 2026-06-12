using System.Text.Json;

public class RecipeBackupService : IRecipeBackupService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICurrentUserContext? _currentUserContext;

    public RecipeBackupService(IRecipeRepository recipeRepository)
        : this(recipeRepository, null)
    {
    }

    public RecipeBackupService(IRecipeRepository recipeRepository, ICurrentUserContext? currentUserContext)
    {
        _recipeRepository = recipeRepository;
        _currentUserContext = currentUserContext;
    }

    public RecipeBackupResult ExportToJson(string backupFilePath)
    {
        try
        {
            string? directoryPath = Path.GetDirectoryName(backupFilePath);

            if (string.IsNullOrWhiteSpace(backupFilePath) ||
                (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath)))
            {
                return new RecipeBackupResult
                {
                    IsSuccess = false,
                    Message = AppTexts.InvalidBackupFile
                };
            }

            List<Recipe> recipes = GetRecipesForCurrentContext();

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
                    && RecipeExistsBySourceUrl(recipe.SourceUrl);

                if (alreadyExists)
                {
                    skippedCount++;
                    continue;
                }

                recipe.Id = 0;
                if (CurrentUserId.HasValue)
                {
                    recipe.UserId = CurrentUserId.Value;
                }

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

    private int? CurrentUserId => _currentUserContext?.UserId;

    private List<Recipe> GetRecipesForCurrentContext()
    {
        if (CurrentUserId.HasValue)
        {
            return _recipeRepository.GetRecipesByUserId(CurrentUserId.Value);
        }

        return _recipeRepository.GetAllRecipes();
    }

    private bool RecipeExistsBySourceUrl(string sourceUrl)
    {
        if (CurrentUserId.HasValue)
        {
            return _recipeRepository.RecipeExistsBySourceUrlForUser(sourceUrl, CurrentUserId.Value);
        }

        return _recipeRepository.RecipeExistsBySourceUrl(sourceUrl);
    }
}
