using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class RecipeLibraryService : IRecipeLibraryService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeCacheService? _recipeCacheService;
    private readonly ICurrentUserContext? _currentUserContext;
    private readonly IRecipePhotoDeletionCoordinator? _photoDeletionCoordinator;
    private readonly ILogger<RecipeLibraryService> _logger;

    public RecipeLibraryService(IRecipeRepository recipeRepository)
        : this(
            recipeRepository,
            null,
            null,
            null,
            NullLogger<RecipeLibraryService>.Instance)
    {
    }

    public RecipeLibraryService(
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext)
        : this(
            recipeRepository,
            null,
            currentUserContext,
            null,
            NullLogger<RecipeLibraryService>.Instance)
    {
    }

    internal RecipeLibraryService(
        IRecipeRepository recipeRepository,
        IRecipeCacheService? recipeCacheService,
        ICurrentUserContext? currentUserContext,
        IRecipePhotoDeletionCoordinator? photoDeletionCoordinator,
        ILogger<RecipeLibraryService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _recipeRepository = recipeRepository;
        _recipeCacheService = recipeCacheService;
        _currentUserContext = currentUserContext;
        _photoDeletionCoordinator = photoDeletionCoordinator;
        _logger = logger;
    }

    public List<RecipeSummary> GetRecipeSummaries()
    {
        if (CurrentUserId.HasValue)
        {
            return _recipeRepository.GetRecipeSummariesByUserId(CurrentUserId.Value);
        }

        return _recipeRepository.GetAllRecipeSummaries();
    }

    public PagedResult<RecipeSummary> GetRecipeSummariesPage(int pageNumber, int pageSize)
    {
        if (CurrentUserId.HasValue)
        {
            return _recipeRepository.GetRecipeSummariesPageByUserId(
                CurrentUserId.Value,
                pageNumber,
                pageSize);
        }

        return _recipeRepository.GetRecipeSummariesPage(pageNumber, pageSize);
    }

    public bool HasRecipesInCurrentContext()
    {
        if (CurrentUserId.HasValue)
        {
            return _recipeRepository.HasRecipesForUser(CurrentUserId.Value);
        }

        return _recipeRepository.HasRecipes();
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        if (recipeId <= 0)
        {
            return null;
        }

        if (CurrentUserId.HasValue)
        {
            return _recipeRepository.GetRecipeByIdAndUserId(recipeId, CurrentUserId.Value);
        }

        return _recipeRepository.GetRecipeById(recipeId);
    }

    public async Task<Recipe?> GetRecipeByIdAsync(int recipeId)
    {
        if (recipeId <= 0)
        {
            return null;
        }

        int? userId = CurrentUserId;

        if (!userId.HasValue)
        {
            return GetRecipeById(recipeId);
        }

        if (_recipeCacheService != null)
        {
            try
            {
                Recipe? cachedRecipe = await _recipeCacheService.GetAsync(recipeId, userId.Value);

                if (cachedRecipe != null)
                {
                    return cachedRecipe;
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to read recipe from Redis cache.");
            }
        }

        Recipe? recipe = _recipeRepository.GetRecipeByIdAndUserId(recipeId, userId.Value);

        if (recipe != null && _recipeCacheService != null)
        {
            try
            {
                await _recipeCacheService.SetAsync(recipe, userId.Value);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to cache recipe {RecipeId} for user {UserId}.",
                    recipeId,
                    userId.Value
                );
            }
        }

        return recipe;
    }

    public List<RecipeSummary> SearchRecipes(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new List<RecipeSummary>();
        }

        return _recipeRepository.SearchRecipes(searchText, CurrentUserId);
    }

    public PagedResult<RecipeSummary> SearchRecipesPage(
        string searchText,
        int pageNumber,
        int pageSize)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new PagedResult<RecipeSummary>(
                new List<RecipeSummary>(),
                pageNumber,
                pageSize,
                0);
        }

        return _recipeRepository.SearchRecipesPage(
            searchText,
            CurrentUserId,
            pageNumber,
            pageSize);
    }

    public bool RecipeExistsBySourceUrl(string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return false;
        }

        string normalizedSourceUrl = sourceUrl.Trim();

        if (CurrentUserId.HasValue)
        {
            return _recipeRepository.RecipeExistsBySourceUrlForUser(
                normalizedSourceUrl,
                CurrentUserId.Value);
        }

        return _recipeRepository.RecipeExistsBySourceUrl(normalizedSourceUrl);
    }

    public RecipeSaveResult SaveRecipe(Recipe recipe)
    {
        NormalizeSourceUrl(recipe);

        RecipeValidationResult validationResult = RecipeValidator.ValidateForSave(recipe);

        if (!validationResult.IsValid)
        {
            string message = string.Join(Environment.NewLine, validationResult.Errors);
            return RecipeSaveResult.Fail(message);
        }

        if (!string.IsNullOrWhiteSpace(recipe.SourceUrl) &&
            RecipeExistsBySourceUrl(recipe.SourceUrl))
        {
            return RecipeSaveResult.Fail(AppTexts.RecipeAlreadyExists);
        }

        if (CurrentUserId.HasValue)
        {
            recipe.UserId = CurrentUserId.Value;
        }

        _recipeRepository.AddRecipe(recipe);

        return RecipeSaveResult.Success(AppTexts.RecipeAdded, recipe);
    }

    public RecipeSaveResult UpdateRecipe(Recipe recipe)
    {
        NormalizeSourceUrl(recipe);

        Recipe? existingRecipe = GetRecipeById(recipe.Id);

        if (existingRecipe == null)
        {
            return RecipeSaveResult.Fail(AppTexts.RecipeNotFound);
        }

        RecipeValidationResult validationResult = RecipeValidator.ValidateForSave(recipe);

        if (!validationResult.IsValid)
        {
            string message = string.Join(Environment.NewLine, validationResult.Errors);
            return RecipeSaveResult.Fail(message);
        }

        if (CurrentUserId.HasValue)
        {
            _recipeRepository.UpdateRecipeForUser(recipe, CurrentUserId.Value);
        }
        else
        {
            _recipeRepository.UpdateRecipe(recipe);
        }

        return RecipeSaveResult.Success(AppTexts.RecipeUpdated, recipe);
    }

    public async Task<RecipeSaveResult> UpdateRecipeAsync(Recipe recipe)
    {
        RecipeSaveResult result = UpdateRecipe(recipe);

        if (!result.IsSuccess)
        {
            return result;
        }

        await InvalidateRecipeCacheAsync(recipe.Id);

        return result;
    }

    public RecipeSaveResult DeleteRecipe(int recipeId)
    {
        if (recipeId <= 0)
        {
            return RecipeSaveResult.Fail(AppTexts.InvalidRecipeId);
        }

        RecipeSummary? recipe = GetRecipeSummaries()
            .FirstOrDefault(recipe => recipe.Id == recipeId);

        if (recipe == null)
        {
            return RecipeSaveResult.Fail(AppTexts.RecipeNotFound);
        }

        if (_photoDeletionCoordinator is null)
        {
            DeleteRecipeFromRepository(recipeId);

            return RecipeSaveResult.Success(AppTexts.RecipeDeleted);
        }

        PreparedRecipePhotoDeletionBatch? batch;

        try
        {
            batch = _photoDeletionCoordinator.PrepareRecipeDeletion(recipeId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to prepare photo content for recipe {RecipeId} deletion.",
                recipeId);

            return RecipeSaveResult.Fail(AppTexts.RecipeDeletionFailed);
        }

        if (batch is null)
        {
            _logger.LogError(
                "Photo content could not be prepared for recipe {RecipeId} deletion.",
                recipeId);

            return RecipeSaveResult.Fail(AppTexts.RecipeDeletionFailed);
        }

        try
        {
            DeleteRecipeFromRepository(recipeId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to delete recipe {RecipeId} metadata.",
                recipeId);

            try
            {
                _photoDeletionCoordinator.RestoreRecipeDeletion(batch);
            }
            catch (Exception restoreException)
            {
                _logger.LogError(
                    restoreException,
                    "Failed to restore photo content for recipe {RecipeId}.",
                    recipeId);
            }

            return RecipeSaveResult.Fail(AppTexts.RecipeDeletionFailed);
        }

        try
        {
            _photoDeletionCoordinator.ConfirmRecipeDeletion(batch);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Photo cleanup is pending for deleted recipe {RecipeId}.",
                recipeId);
        }

        return RecipeSaveResult.Success(AppTexts.RecipeDeleted);
    }

    public async Task<RecipeSaveResult> DeleteRecipeAsync(int recipeId)
    {
        RecipeSaveResult result = DeleteRecipe(recipeId);

        if (!result.IsSuccess)
        {
            return result;
        }

        await InvalidateRecipeCacheAsync(recipeId);

        return result;
    }

    private int? CurrentUserId => _currentUserContext?.UserId;

    private void DeleteRecipeFromRepository(int recipeId)
    {
        if (CurrentUserId.HasValue)
        {
            _recipeRepository.DeleteRecipeForUser(
                recipeId,
                CurrentUserId.Value);
        }
        else
        {
            _recipeRepository.DeleteRecipe(recipeId);
        }
    }

    private static void NormalizeSourceUrl(Recipe recipe)
    {
        recipe.SourceUrl = recipe.SourceUrl?.Trim() ?? string.Empty;
    }

    private async Task InvalidateRecipeCacheAsync(int recipeId)
    {
        if (_recipeCacheService == null || !CurrentUserId.HasValue)
        {
            return;
        }

        try
        {
            await _recipeCacheService.RemoveAsync(recipeId, CurrentUserId.Value);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to invalidate cache for recipe {RecipeId}.",
                recipeId
            );
        }
    }
}
