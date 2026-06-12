public interface IRecipeRepository
{
    List<Recipe> GetAllRecipes();
    List<Recipe> GetRecipesByUserId(int userId);

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