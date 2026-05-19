public interface IRecipeRepository
{
    List<Recipe> GetAllRecipes();

    Recipe? GetRecipeById(int recipeId);

    void AddRecipe(Recipe recipe);

    void UpdateRecipe(Recipe recipe);

    bool RecipeExistsBySourceUrl(string sourceUrl);

    void DeleteRecipe(int recipeId);
}