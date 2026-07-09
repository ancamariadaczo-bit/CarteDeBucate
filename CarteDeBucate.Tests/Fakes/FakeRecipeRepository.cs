public class FakeRecipeRepository : IRecipeRepository
{
    public bool AddRecipeWasCalled { get; private set; }
    public bool UpdateRecipeWasCalled { get; private set; }
    public bool DeleteRecipeWasCalled { get; private set; }
    public bool DeleteRecipeForUserWasCalled { get; private set; }
    public bool HasRecipesWasCalled { get; private set; }
    public bool HasRecipesForUserWasCalled { get; private set; }
    public bool GetAllRecipeSummariesWasCalled { get; private set; }
    public bool GetRecipeSummariesByUserIdWasCalled { get; private set; }
    public bool GetRecipeSummariesPageWasCalled { get; private set; }
    public bool GetRecipeSummariesPageByUserIdWasCalled { get; private set; }
    public bool SearchRecipesWasCalled { get; private set; }
    public bool SearchRecipesPageWasCalled { get; private set; }
    public bool GetRecipeByIdWasCalled { get; private set; }
    public bool GetRecipeByIdAndUserIdWasCalled { get; private set; }

    public Recipe? AddedRecipe { get; private set; }
    public Recipe? UpdatedRecipe { get; private set; }
    public int? DeletedRecipeId { get; private set; }
    public int? UserIdPassedToDeleteRecipeForUser { get; private set; }
    public int? UserIdPassedToHasRecipesForUser { get; private set; }
    public int? UserIdPassedToGetRecipeSummariesByUserId { get; private set; }
    public int? UserIdPassedToGetRecipeSummariesPageByUserId { get; private set; }
    public int? PageNumberPassedToGetRecipeSummariesPage { get; private set; }
    public int? PageSizePassedToGetRecipeSummariesPage { get; private set; }
    public int? UserIdPassedToSearchRecipes { get; private set; }
    public int? UserIdPassedToSearchRecipesPage { get; private set; }
    public int? PageNumberPassedToSearchRecipesPage { get; private set; }
    public int? PageSizePassedToSearchRecipesPage { get; private set; }
    public string? SearchTextPassedToSearchRecipes { get; private set; }
    public string? SearchTextPassedToSearchRecipesPage { get; private set; }

    public bool SourceUrlExists { get; set; }

    public List<Recipe> Recipes { get; set; } = new List<Recipe>();

    public List<RecipeSummary> GetAllRecipeSummaries()
    {
        GetAllRecipeSummariesWasCalled = true;

        return Recipes
            .Select(ToRecipeSummary)
            .ToList();
    }

    public List<Recipe> GetAllRecipes()
    {
        return Recipes;
    }

    public PagedResult<RecipeSummary> GetRecipeSummariesPage(int pageNumber, int pageSize)
    {
        GetRecipeSummariesPageWasCalled = true;
        PageNumberPassedToGetRecipeSummariesPage = pageNumber;
        PageSizePassedToGetRecipeSummariesPage = pageSize;

        return CreateRecipeSummariesPage(Recipes, pageNumber, pageSize);
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        GetRecipeByIdWasCalled = true;

        return Recipes.FirstOrDefault(recipe => recipe.Id == recipeId);
    }

    public List<Recipe> GetRecipesByUserId(int userId)
    {
        return Recipes
            .Where(recipe => recipe.UserId == userId)
            .ToList();
    }

    public List<RecipeSummary> GetRecipeSummariesByUserId(int userId)
    {
        GetRecipeSummariesByUserIdWasCalled = true;
        UserIdPassedToGetRecipeSummariesByUserId = userId;

        return Recipes
            .Where(recipe => recipe.UserId == userId)
            .Select(ToRecipeSummary)
            .ToList();
    }

    public PagedResult<RecipeSummary> GetRecipeSummariesPageByUserId(int userId, int pageNumber, int pageSize)
    {
        GetRecipeSummariesPageByUserIdWasCalled = true;
        UserIdPassedToGetRecipeSummariesPageByUserId = userId;
        PageNumberPassedToGetRecipeSummariesPage = pageNumber;
        PageSizePassedToGetRecipeSummariesPage = pageSize;

        List<Recipe> recipesForUser = Recipes
            .Where(recipe => recipe.UserId == userId)
            .ToList();

        return CreateRecipeSummariesPage(recipesForUser, pageNumber, pageSize);
    }

    public List<RecipeSummary> SearchRecipes(string searchText, int? userId)
    {
        SearchRecipesWasCalled = true;
        SearchTextPassedToSearchRecipes = searchText;
        UserIdPassedToSearchRecipes = userId;

        return FilterRecipesBySearchText(searchText, userId)
            .Select(ToRecipeSummary)
            .ToList();
    }

    public PagedResult<RecipeSummary> SearchRecipesPage(
        string searchText,
        int? userId,
        int pageNumber,
        int pageSize)
    {
        SearchRecipesPageWasCalled = true;
        SearchTextPassedToSearchRecipesPage = searchText;
        UserIdPassedToSearchRecipesPage = userId;
        PageNumberPassedToSearchRecipesPage = pageNumber;
        PageSizePassedToSearchRecipesPage = pageSize;

        return CreateRecipeSummariesPage(
            FilterRecipesBySearchText(searchText, userId),
            pageNumber,
            pageSize);
    }

    public bool HasRecipes()
    {
        HasRecipesWasCalled = true;

        return Recipes.Count > 0;
    }

    public bool HasRecipesForUser(int userId)
    {
        HasRecipesForUserWasCalled = true;
        UserIdPassedToHasRecipesForUser = userId;

        return Recipes.Any(recipe => recipe.UserId == userId);
    }

    public Recipe? GetRecipeByIdAndUserId(int recipeId, int userId)
    {
        GetRecipeByIdAndUserIdWasCalled = true;

        return Recipes.FirstOrDefault(recipe =>
            recipe.Id == recipeId &&
            recipe.UserId == userId);
    }

    public void AddRecipe(Recipe recipe)
    {
        AddRecipeWasCalled = true;
        AddedRecipe = recipe;
        Recipes.Add(recipe);
    }

    public void UpdateRecipe(Recipe recipe)
    {
        UpdateRecipeWasCalled = true;
        UpdatedRecipe = recipe;

        Recipe? existingRecipe = Recipes.FirstOrDefault(r => r.Id == recipe.Id);

        if (existingRecipe != null)
        {
            Recipes.Remove(existingRecipe);
        }

        Recipes.Add(recipe);
    }

    public void UpdateRecipeForUser(Recipe recipe, int userId)
    {
        recipe.UserId = userId;
        UpdateRecipe(recipe);
    }

    public bool RecipeExistsBySourceUrl(string sourceUrl)
    {
        if (SourceUrlExists)
        {
            return true;
        }

        return Recipes.Any(recipe => recipe.SourceUrl == sourceUrl);
    }

    public bool RecipeExistsBySourceUrlForUser(string sourceUrl, int userId)
    {
        if (SourceUrlExists)
        {
            return true;
        }

        return Recipes.Any(recipe =>
            recipe.UserId == userId &&
            recipe.SourceUrl == sourceUrl);
    }

    public void DeleteRecipe(int recipeId)
    {
        DeleteRecipeWasCalled = true;
        DeletedRecipeId = recipeId;

        Recipe? existingRecipe = Recipes.FirstOrDefault(recipe => recipe.Id == recipeId);

        if (existingRecipe != null)
        {
            Recipes.Remove(existingRecipe);
        }
    }

    public void DeleteRecipeForUser(int recipeId, int userId)
    {
        DeleteRecipeForUserWasCalled = true;
        DeletedRecipeId = recipeId;
        UserIdPassedToDeleteRecipeForUser = userId;

        Recipe? existingRecipe = Recipes.FirstOrDefault(recipe =>
            recipe.Id == recipeId &&
            recipe.UserId == userId);

        if (existingRecipe != null)
        {
            Recipes.Remove(existingRecipe);
        }
    }

    private RecipeSummary ToRecipeSummary(Recipe recipe)
    {
        return new RecipeSummary
        {
            Id = recipe.Id,
            Name = recipe.Name,
            SourceUrl = recipe.SourceUrl,
            SavedAt = recipe.SavedAt,
            Status = recipe.Status,
            UserId = recipe.UserId
        };
    }

    private PagedResult<RecipeSummary> CreateRecipeSummariesPage(
        IEnumerable<Recipe> recipes,
        int pageNumber,
        int pageSize)
    {
        List<Recipe> orderedRecipes = recipes
            .OrderByDescending(recipe => recipe.SavedAt)
            .ToList();

        int totalItems = orderedRecipes.Count;
        int normalizedPageNumber = Math.Max(1, pageNumber);
        int normalizedPageSize = Math.Max(1, pageSize);

        List<RecipeSummary> items = orderedRecipes
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(ToRecipeSummary)
            .ToList();

        return new PagedResult<RecipeSummary>(
            items,
            pageNumber,
            pageSize,
            totalItems);
    }

    private IEnumerable<Recipe> FilterRecipesBySearchText(string searchText, int? userId)
    {
        return Recipes
            .Where(recipe =>
                (!userId.HasValue || recipe.UserId == userId.Value) &&
                ((recipe.Name ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (recipe.SourceUrl ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (recipe.Notes ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                recipe.Ingredients.Any(ingredient =>
                    ingredient.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                recipe.Steps.Any(step =>
                    step.Contains(searchText, StringComparison.OrdinalIgnoreCase))));
    }
}
