using System.Text.Json;
using System.Linq;

public class JsonRecipeRepository : IRecipeRepository
{
    private readonly string _filePath;

    public JsonRecipeRepository(string filePath)
    {
        _filePath = filePath;
    }

    public List<Recipe> GetAllRecipes()
    {
        if (!File.Exists(_filePath))
        {
            return new List<Recipe>();
        }

        string json = File.ReadAllText(_filePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<Recipe>();
        }

        List<Recipe>? recipes = JsonSerializer.Deserialize<List<Recipe>>(json);

        return recipes ?? new List<Recipe>();
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        List<Recipe> recipes = GetAllRecipes();

        return recipes.FirstOrDefault(recipe => recipe.Id == recipeId);
    }

    public void AddRecipe(Recipe recipe)
    {
        List<Recipe> recipes = GetAllRecipes();

        if (recipe.Id == 0)
        {
            recipe.Id = GetNextRecipeId(recipes);
        }

        recipes.Add(recipe);

        SaveRecipes(recipes);
    }

    public void UpdateRecipe(Recipe recipe)
    {
        List<Recipe> recipes = GetAllRecipes();

        int recipeIndex = recipes.FindIndex(existingRecipe => existingRecipe.Id == recipe.Id);

        if (recipeIndex == -1)
        {
            return;
        }

        recipes[recipeIndex] = recipe;

        SaveRecipes(recipes);
    }

    public bool RecipeExistsBySourceUrl(string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return false;
        }

        List<Recipe> recipes = GetAllRecipes();

        return recipes.Any(existingRecipe =>
            existingRecipe.SourceUrl.Trim().Equals(
                sourceUrl.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    private void SaveRecipes(List<Recipe> recipes)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(recipes, options);

        File.WriteAllText(_filePath, json);
    }

    public void DeleteRecipe(int recipeId)
    {
        List<Recipe> recipes = GetAllRecipes();

        Recipe? recipeToDelete = recipes
            .FirstOrDefault(recipe => recipe.Id == recipeId);

        if (recipeToDelete == null)
        {
            return;
        }

        recipes.Remove(recipeToDelete);

        SaveRecipes(recipes);
    }

    private int GetNextRecipeId(List<Recipe> recipes)
    {
        if (recipes.Count == 0)
        {
            return 1;
        }

        return recipes.Max(recipe => recipe.Id) + 1;
    }
}