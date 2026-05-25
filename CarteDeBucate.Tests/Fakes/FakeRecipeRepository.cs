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

    public bool RecipeExistsBySourceUrl(string sourceUrl)
    {
        if (SourceUrlExists)
        {
            return true;
        }

        return Recipes.Any(recipe => recipe.SourceUrl == sourceUrl);
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
}
