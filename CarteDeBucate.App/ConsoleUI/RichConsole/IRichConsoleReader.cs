public interface IRichConsoleReader
{
    Recipe ReadRecipe();

    string ReadRecipeUrlToImport();

    string ReadSearchText();

    bool ConfirmKeepImportedIngredients();

    bool ConfirmKeepImportedSteps();

    List<string> ReadIngredients();

    List<string> ReadSteps();

    string ReadNotes();

    bool ConfirmSaveRecipe();

    Recipe SelectRecipe(List<Recipe> recipes, string title);

    Recipe ReadRecipeEdits(Recipe recipe);

    bool ConfirmDeleteRecipe(Recipe recipe);

    string ReadBackupFilePath();

    void WaitForContinue();
}
