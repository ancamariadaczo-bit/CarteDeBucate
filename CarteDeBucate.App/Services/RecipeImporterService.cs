using System.Linq;

public class RecipeImporterService : IRecipeImporterService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipeImporter _recipeImporter;

    public RecipeImporterService(
        IRecipeRepository recipeRepository,
        IRecipeImporter recipeImporter)
    {
        _recipeRepository = recipeRepository;
        _recipeImporter = recipeImporter;
    }

    public List<Recipe> GetAllRecipes()
    {
        return _recipeRepository.GetAllRecipes();
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        if (recipeId <= 0)
        {
            return null;
        }

        return _recipeRepository.GetRecipeById(recipeId);
    }

    public List<Recipe> SearchRecipes(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new List<Recipe>();
        }

        List<Recipe> recipes = _recipeRepository.GetAllRecipes();

        return recipes
            .Where(recipe =>
                (recipe.Name ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (recipe.SourceUrl ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (recipe.Notes ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                recipe.Ingredients.Any(ingredient =>
                    ingredient.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                recipe.Steps.Any(step =>
                    step.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task<RecipeImportResult> ImportRecipeFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new RecipeImportResult
            {
                Success = false,
                Message = AppTexts.EmptyUrl
            };
        }

        return await _recipeImporter.ImportFromUrlAsync(url);
    }

    public async Task<RecipeSaveResult> ImportFromUrlAndSaveAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return RecipeSaveResult.Fail(AppTexts.EmptyUrl);
        }

        RecipeImportResult importResult = await _recipeImporter.ImportFromUrlAsync(url);

        if (!importResult.Success || importResult.Recipe == null)
        {
            return RecipeSaveResult.Fail(importResult.Message);
        }

        return SaveRecipe(importResult.Recipe);
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
            _recipeRepository.RecipeExistsBySourceUrl(recipe.SourceUrl))
        {
            return RecipeSaveResult.Fail(AppTexts.RecipeAlreadyExists);
        }

        _recipeRepository.AddRecipe(recipe);

        return RecipeSaveResult.Success(AppTexts.RecipeAdded, recipe);
    }

    public RecipeSaveResult UpdateRecipe(Recipe recipe)
    {
        Recipe? existingRecipe = _recipeRepository.GetRecipeById(recipe.Id);

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

        _recipeRepository.UpdateRecipe(recipe);

        return RecipeSaveResult.Success(AppTexts.RecipeUpdated, recipe);
    }

    public RecipeSaveResult DeleteRecipe(int recipeId)
    {
        if (recipeId <= 0)
        {
            return RecipeSaveResult.Fail(AppTexts.InvalidRecipeId);
        }

        Recipe? recipe = _recipeRepository.GetRecipeById(recipeId);

        if (recipe == null)
        {
            return RecipeSaveResult.Fail(AppTexts.RecipeNotFound);
        }

        _recipeRepository.DeleteRecipe(recipeId);

        return RecipeSaveResult.Success(AppTexts.RecipeDeleted, recipe);
    }
}