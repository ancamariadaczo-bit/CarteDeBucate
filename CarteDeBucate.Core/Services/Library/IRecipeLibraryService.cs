public interface IRecipeLibraryService
{
    List<RecipeSummary> GetRecipeSummaries();
    PagedResult<RecipeSummary> GetRecipeSummariesPage(int pageNumber, int pageSize);

    bool HasRecipesInCurrentContext();

    Recipe? GetRecipeById(int recipeId);

    List<RecipeSummary> SearchRecipes(string searchText);
    PagedResult<RecipeSummary> SearchRecipesPage(string searchText, int pageNumber, int pageSize);

    RecipeSaveResult SaveRecipe(Recipe recipe);

    RecipeSaveResult UpdateRecipe(Recipe recipe);

    RecipeSaveResult DeleteRecipe(int recipeId);
}
