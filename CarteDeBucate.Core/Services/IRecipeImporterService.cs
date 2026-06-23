public interface IRecipeImporterService
{
    List<RecipeSummary> GetRecipeSummaries();

    bool HasRecipesInCurrentContext();

    Recipe? GetRecipeById(int recipeId);

    List<RecipeSummary> SearchRecipes(string searchText);

    Task<RecipeImportResult> ImportRecipeFromUrlAsync(string url);

    Task<RecipeSaveResult> ImportFromUrlAndSaveAsync(string url);

    RecipeSaveResult SaveRecipe(Recipe recipe);

    RecipeSaveResult UpdateRecipe(Recipe recipe);

    RecipeSaveResult DeleteRecipe(int recipeId);
}
