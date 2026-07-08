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

    public List<RecipeSummary> GetAllRecipeSummaries()
    {
        List<Recipe> recipes = GetAllRecipes();

        return recipes
            .OrderByDescending(recipe => recipe.SavedAt)
            .Select(ToRecipeSummary)
            .ToList();
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        List<Recipe> recipes = GetAllRecipes();

        return recipes.FirstOrDefault(recipe => recipe.Id == recipeId);
    }

    public List<Recipe> GetRecipesByUserId(int userId)
    {
        List<Recipe> recipes = GetAllRecipes();

        return recipes
            .Where(recipe => recipe.UserId == userId)
            .ToList();
    }

    public List<RecipeSummary> GetRecipeSummariesByUserId(int userId)
    {
        List<Recipe> recipes = GetAllRecipes();

        return recipes
            .Where(recipe => recipe.UserId == userId)
            .OrderByDescending(recipe => recipe.SavedAt)
            .Select(ToRecipeSummary)
            .ToList();
    }

    public PagedResult<RecipeSummary> GetRecipeSummariesPage(int pageNumber, int pageSize)
    {
        List<Recipe> recipes = GetAllRecipes();

        return CreateRecipeSummariesPage(recipes, pageNumber, pageSize);
    }

    public PagedResult<RecipeSummary> GetRecipeSummariesPageByUserId(
        int userId,
        int pageNumber,
        int pageSize)
    {
        List<Recipe> recipes = GetAllRecipes()
            .Where(recipe => recipe.UserId == userId)
            .ToList();

        return CreateRecipeSummariesPage(recipes, pageNumber, pageSize);
    }

    public List<RecipeSummary> SearchRecipes(string searchText, int? userId)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new List<RecipeSummary>();
        }

        List<Recipe> recipes = GetAllRecipes();

        return recipes
            .Where(recipe =>
                (!userId.HasValue || recipe.UserId == userId.Value) &&
                ((recipe.Name ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (recipe.SourceUrl ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (recipe.Notes ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                recipe.Ingredients.Any(ingredient =>
                    ingredient.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                recipe.Steps.Any(step =>
                    step.Contains(searchText, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(recipe => recipe.SavedAt)
            .Select(ToRecipeSummary)
            .ToList();
    }

    public bool HasRecipes()
    {
        List<Recipe> recipes = GetAllRecipes();

        return recipes.Count > 0;
    }

    public bool HasRecipesForUser(int userId)
    {
        List<Recipe> recipes = GetAllRecipes();

        return recipes.Any(recipe => recipe.UserId == userId);
    }

    public Recipe? GetRecipeByIdAndUserId(int recipeId, int userId)
    {
        List<Recipe> recipes = GetAllRecipes();

        return recipes.FirstOrDefault(recipe =>
            recipe.Id == recipeId &&
            recipe.UserId == userId);
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

    public void UpdateRecipeForUser(Recipe recipe, int userId)
    {
        List<Recipe> recipes = GetAllRecipes();

        int recipeIndex = recipes.FindIndex(existingRecipe =>
            existingRecipe.Id == recipe.Id &&
            existingRecipe.UserId == userId);

        if (recipeIndex == -1)
        {
            return;
        }

        recipe.UserId = userId;
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

    public bool RecipeExistsBySourceUrlForUser(string sourceUrl, int userId)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return false;
        }

        List<Recipe> recipes = GetAllRecipes();

        return recipes.Any(existingRecipe =>
            existingRecipe.UserId == userId &&
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

    public void DeleteRecipeForUser(int recipeId, int userId)
    {
        List<Recipe> recipes = GetAllRecipes();

        Recipe? recipeToDelete = recipes
            .FirstOrDefault(recipe =>
                recipe.Id == recipeId &&
                recipe.UserId == userId);

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
}
