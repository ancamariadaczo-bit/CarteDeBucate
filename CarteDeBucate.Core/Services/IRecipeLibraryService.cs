public interface IRecipeLibraryService
{
    List<RecipeSummary> GetRecipeSummaries();

    bool HasRecipesInCurrentContext();

    Recipe? GetRecipeById(int recipeId);

    List<RecipeSummary> SearchRecipes(string searchText);

    RecipeSaveResult SaveRecipe(Recipe recipe);

    RecipeSaveResult UpdateRecipe(Recipe recipe);

    RecipeSaveResult DeleteRecipe(int recipeId);
}
