public interface IRecipeConsoleWriter
{
    void DisplayEmptyLine();
    void ShowMenu(List<MenuOption> options);
    void DisplayMessage(string message);
    void DisplayRecipes(List<Recipe> recipesToDisplay);
    void DisplaySearchResults(List<Recipe> foundRecipes);
    void DisplayRecipe(Recipe recipe, int index);
    void DisplayImportedRecipe(Recipe recipe);
    void DisplayRecipeList(List<Recipe> recipesToDisplay);
    void DisplayRecipeDetails(Recipe recipe);
}