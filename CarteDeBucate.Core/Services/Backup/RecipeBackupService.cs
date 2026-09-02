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

            RecipeBackupExportResult exportResult = ExportToJsonContent();
            if (!exportResult.IsSuccess)
            {
                return new RecipeBackupResult
                {
                    IsSuccess = false,
                    Message = exportResult.Message,
                    ExportedCount = exportResult.ExportedCount
                };
            }

            File.WriteAllText(backupFilePath, exportResult.Json);

            return new RecipeBackupResult
            {
                IsSuccess = true,
                Message = exportResult.Message,
                ExportedCount = exportResult.ExportedCount
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

    public RecipeBackupExportResult ExportToJsonContent()
    {
        try
        {
            List<Recipe> recipes = GetRecipesForCurrentContext();
            RecipeBackupDocument backupDocument = new RecipeBackupDocument
            {
                ExportedAtUtc = DateTime.UtcNow,
                Recipes = recipes.Select(RecipeBackupMapper.ToBackupItem).ToList()
            };
            string json = JsonSerializer.Serialize(
                backupDocument,
                RecipeBackupJson.SerializerOptions);

            return new RecipeBackupExportResult
            {
                IsSuccess = true,
                Message = string.Format(AppTexts.BackupExportCompleted, recipes.Count),
                ExportedCount = recipes.Count,
                Json = json
            };
        }
        catch (Exception exception)
        {
            return new RecipeBackupExportResult
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

            return ImportFromJsonContent(json);
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

    public RecipeBackupResult ImportFromJsonContent(string json)
    {
        try
        {
            int version = RecipeBackupDocumentReader.DetectVersion(json);
            List<Recipe>? recipes;

            if (version == RecipeBackupVersions.Current)
            {
                RecipeBackupDocument backupDocument =
                    RecipeBackupDocumentReader.ReadCurrentDocument(json);
                recipes = backupDocument.Recipes
                    .Select(RecipeBackupMapper.ToRecipe)
                    .ToList();
            }
            else
            {
                List<Recipe>? legacyRecipes = JsonSerializer.Deserialize<List<Recipe>>(json);

                if (legacyRecipes?.Any(recipe => recipe is null) == true)
                {
                    throw new JsonException();
                }

                recipes = legacyRecipes?
                    .Select(RecipeBackupMapper.ToBackupItem)
                    .Select(RecipeBackupMapper.ToRecipe)
                    .ToList();
            }

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
                recipe.UserId = CurrentUserId;

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
