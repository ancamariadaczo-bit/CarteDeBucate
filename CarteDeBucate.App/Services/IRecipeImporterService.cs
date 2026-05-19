public interface IRecipeService
{
    List<Recipe> GetAllRecipes();

    Recipe? GetRecipeById(int recipeId);

    List<Recipe> SearchRecipes(string searchText);

    Task<RecipeImportResult> ImportRecipeFromUrlAsync(string url);

    Task<RecipeSaveResult> ImportFromUrlAndSaveAsync(string url);

    RecipeSaveResult SaveRecipe(Recipe recipe);

    RecipeSaveResult UpdateRecipe(Recipe recipe);

    RecipeSaveResult DeleteRecipe(int recipeId);
}