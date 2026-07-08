public interface IRecipeConsoleWriter
{
    void Clear();
    void DisplayEmptyLine();
    void ShowMenu(List<MenuOption> options);
    void DisplayMessage(string message);
    void DisplayRecipes(List<RecipeSummary> recipesToDisplay);
    void DisplaySearchResults(List<RecipeSummary> foundRecipes);
    void DisplayRecipe(RecipeSummary recipe, int index);
    void DisplayImportedRecipe(Recipe recipe);
    void DisplayRecipeList(List<RecipeSummary> recipesToDisplay);
    void DisplayRecipeDetails(Recipe recipe);
}
