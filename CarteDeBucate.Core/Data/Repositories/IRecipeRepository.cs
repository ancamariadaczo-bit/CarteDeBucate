public interface IRecipeRepository
{
    List<RecipeSummary> GetAllRecipeSummaries();
    List<RecipeSummary> GetRecipeSummariesByUserId(int userId);
    PagedResult<RecipeSummary> GetRecipeSummariesPage(int pageNumber, int pageSize);
    PagedResult<RecipeSummary> GetRecipeSummariesPageByUserId(int userId, int pageNumber, int pageSize);

    List<Recipe> GetAllRecipes();
    List<Recipe> GetRecipesByUserId(int userId);

    List<RecipeSummary> SearchRecipes(string searchText, int? userId);
    PagedResult<RecipeSummary> SearchRecipesPage(string searchText, int? userId, int pageNumber, int pageSize);

    bool HasRecipes();
    bool HasRecipesForUser(int userId);

    Recipe? GetRecipeById(int recipeId);
    Recipe? GetRecipeByIdAndUserId(int recipeId, int userId);

    void AddRecipe(Recipe recipe);

    void UpdateRecipe(Recipe recipe);
    void UpdateRecipeForUser(Recipe recipe, int userId);

    void DeleteRecipe(int recipeId);
    void DeleteRecipeForUser(int recipeId, int userId);

    bool RecipeExistsBySourceUrl(string sourceUrl);
    bool RecipeExistsBySourceUrlForUser(string sourceUrl, int userId);
}
