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
    public bool SearchRecipesWasCalled { get; private set; }
    public bool GetRecipeByIdWasCalled { get; private set; }
    public bool GetRecipeByIdAndUserIdWasCalled { get; private set; }

    public Recipe? AddedRecipe { get; private set; }
    public Recipe? UpdatedRecipe { get; private set; }
    public int? DeletedRecipeId { get; private set; }
    public int? UserIdPassedToDeleteRecipeForUser { get; private set; }
    public int? UserIdPassedToHasRecipesForUser { get; private set; }
    public int? UserIdPassedToGetRecipeSummariesByUserId { get; private set; }
    public int? UserIdPassedToSearchRecipes { get; private set; }
    public string? SearchTextPassedToSearchRecipes { get; private set; }

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

    public List<RecipeSummary> SearchRecipes(string searchText, int? userId)
    {
        SearchRecipesWasCalled = true;
        SearchTextPassedToSearchRecipes = searchText;
        UserIdPassedToSearchRecipes = userId;

        return Recipes
            .Where(recipe =>
                (!userId.HasValue || recipe.UserId == userId.Value) &&
                ((recipe.Name ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (recipe.SourceUrl ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (recipe.Notes ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                recipe.Ingredients.Any(ingredient =>
                    ingredient.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                recipe.Steps.Any(step =>
                    step.Contains(searchText, StringComparison.OrdinalIgnoreCase))))
            .Select(ToRecipeSummary)
            .ToList();
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
}
