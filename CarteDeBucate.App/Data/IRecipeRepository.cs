public interface IRecipeRepository
{
    List<Recipe> GetAllRecipes();

    Recipe? GetRecipeById(int recipeId);

    void AddRecipe(Recipe recipe);

    void UpdateRecipe(Recipe recipe);

    void DeleteRecipe(int recipeId);

    bool RecipeExistsBySourceUrl(string sourceUrl);
}