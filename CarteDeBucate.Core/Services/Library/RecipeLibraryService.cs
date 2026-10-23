using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class RecipeLibraryService : IRecipeLibraryService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICurrentUserContext? _currentUserContext;
    private readonly IRecipePhotoDeletionCoordinator? _photoDeletionCoordinator;
    private readonly ILogger<RecipeLibraryService> _logger;

    public RecipeLibraryService(IRecipeRepository recipeRepository)
        : this(
            recipeRepository,
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
            currentUserContext,
            null,
            NullLogger<RecipeLibraryService>.Instance)
    {
    }

    internal RecipeLibraryService(
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        IRecipePhotoDeletionCoordinator? photoDeletionCoordinator,
        ILogger<RecipeLibraryService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _recipeRepository = recipeRepository;
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

}
