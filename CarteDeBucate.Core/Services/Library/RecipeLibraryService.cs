public class RecipeLibraryService : IRecipeLibraryService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICurrentUserContext? _currentUserContext;

    public RecipeLibraryService(IRecipeRepository recipeRepository)
        : this(recipeRepository, null)
    {
    }

    public RecipeLibraryService(
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext)
    {
        _recipeRepository = recipeRepository;
        _currentUserContext = currentUserContext;
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

    public RecipeSaveResult SaveRecipe(Recipe recipe)
    {
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

        if (CurrentUserId.HasValue)
        {
            _recipeRepository.DeleteRecipeForUser(recipeId, CurrentUserId.Value);
        }
        else
        {
            _recipeRepository.DeleteRecipe(recipeId);
        }

        return RecipeSaveResult.Success(AppTexts.RecipeDeleted);
    }

    private int? CurrentUserId => _currentUserContext?.UserId;

    private bool RecipeExistsBySourceUrl(string sourceUrl)
    {
        if (CurrentUserId.HasValue)
        {
            return _recipeRepository.RecipeExistsBySourceUrlForUser(sourceUrl, CurrentUserId.Value);
        }

        return _recipeRepository.RecipeExistsBySourceUrl(sourceUrl);
    }
}
