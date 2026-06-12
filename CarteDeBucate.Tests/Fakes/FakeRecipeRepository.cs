public class FakeRecipeRepository : IRecipeRepository
{
    public bool AddRecipeWasCalled { get; private set; }
    public bool UpdateRecipeWasCalled { get; private set; }
    public bool DeleteRecipeWasCalled { get; private set; }

    public Recipe? AddedRecipe { get; private set; }
    public Recipe? UpdatedRecipe { get; private set; }
    public int? DeletedRecipeId { get; private set; }

    public bool SourceUrlExists { get; set; }

    public List<Recipe> Recipes { get; set; } = new List<Recipe>();

    public List<Recipe> GetAllRecipes()
    {
        return Recipes;
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        return Recipes.FirstOrDefault(recipe => recipe.Id == recipeId);
    }

    public List<Recipe> GetRecipesByUserId(int userId)
    {
        return Recipes
            .Where(recipe => recipe.UserId == userId)
            .ToList();
    }

    public Recipe? GetRecipeByIdAndUserId(int recipeId, int userId)
    {
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
        Recipe? existingRecipe = Recipes.FirstOrDefault(recipe =>
            recipe.Id == recipeId &&
            recipe.UserId == userId);

        if (existingRecipe != null)
        {
            DeleteRecipe(existingRecipe.Id);
        }
    }
}
