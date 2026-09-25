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
        catch (Exception)
        {
            return new RecipeBackupResult
            {
                IsSuccess = false,
                Message = AppTexts.BackupImportFailed
            };
        }
    }

    public RecipeBackupResult ImportFromJsonContent(string json)
    {
        try
        {
            List<Recipe>? recipes = ReadRecipes(json);

            if (recipes == null)
            {
                return new RecipeBackupResult
                {
                    IsSuccess = false,
                    Message = AppTexts.InvalidBackupFile
                };
            }

            for (int recipeIndex = 0; recipeIndex < recipes.Count; recipeIndex++)
            {
                Recipe recipe = recipes[recipeIndex];
                recipe.SourceUrl = recipe.SourceUrl?.Trim() ?? string.Empty;

                RecipeValidationResult validationResult = RecipeValidator.ValidateForSave(recipe);

                if (!validationResult.IsValid)
                {
                    return new RecipeBackupResult
                    {
                        IsSuccess = false,
                        Message = string.Format(
                            AppTexts.BackupImportInvalidRecipe,
                            recipeIndex + 1,
                            string.Join(" ", validationResult.Errors))
                    };
                }
            }

            List<Recipe> recipesToImport = new List<Recipe>();
            HashSet<string> sourceUrlsInDocument =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int skippedCount = 0;

            foreach (Recipe recipe in recipes)
            {
                bool isDuplicateInDocument = !sourceUrlsInDocument.Add(recipe.SourceUrl);
                bool alreadyExists =
                    isDuplicateInDocument || RecipeExistsBySourceUrl(recipe.SourceUrl);

                if (alreadyExists)
                {
                    skippedCount++;
                    continue;
                }

                recipe.Id = 0;
                recipe.UserId = CurrentUserId;
                recipesToImport.Add(recipe);
            }

            if (recipesToImport.Count > 0)
            {
                _recipeRepository.AddRecipes(recipesToImport);
            }

            return new RecipeBackupResult
            {
                IsSuccess = true,
                Message = string.Format(
                    AppTexts.BackupImportCompleted,
                    recipesToImport.Count,
                    skippedCount),
                ImportedCount = recipesToImport.Count,
                SkippedCount = skippedCount
            };
        }
        catch (JsonException)
        {
            return new RecipeBackupResult
            {
                IsSuccess = false,
                Message = AppTexts.BackupImportInvalidDocument
            };
        }
        catch (Exception)
        {
            return new RecipeBackupResult
            {
                IsSuccess = false,
                Message = AppTexts.BackupImportFailed
            };
        }
    }

    private static List<Recipe>? ReadRecipes(string json)
    {
        int version = RecipeBackupDocumentReader.DetectVersion(json);

        if (version == RecipeBackupVersions.Current)
        {
            RecipeBackupDocument backupDocument =
                RecipeBackupDocumentReader.ReadCurrentDocument(json);

            if (backupDocument.Recipes.Any(recipe => recipe is null))
            {
                throw new JsonException();
            }

            return backupDocument.Recipes
                .Select(RecipeBackupMapper.ToRecipe)
                .ToList();
        }

        List<Recipe>? legacyRecipes = JsonSerializer.Deserialize<List<Recipe>>(json);

        if (legacyRecipes?.Any(recipe => recipe is null) == true)
        {
            throw new JsonException();
        }

        return legacyRecipes?
            .Select(RecipeBackupMapper.ToBackupItem)
            .Select(RecipeBackupMapper.ToRecipe)
            .ToList();
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
