public interface IRichConsoleDisplay
{
    void Clear();

    void ShowTitle();

    void ShowRecipes(List<RecipeSummary> recipes, string? title = null, string? emptyMessage = null);

    void ShowRecipeDetails(Recipe recipe, string? title = null);

    void ShowImportedRecipe(Recipe recipe);

    void ShowSuccess(string message);

    void ShowSpacedSuccess(string message);

    void ShowError(string message);

    void ShowInfo(string message);
}
