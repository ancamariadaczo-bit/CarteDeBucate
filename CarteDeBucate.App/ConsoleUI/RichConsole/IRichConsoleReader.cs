public interface IRichConsoleReader
{
    Recipe? ReadRecipe();

    string? ReadRecipeUrlToImport();

    string? ReadSearchText();

    bool ConfirmKeepImportedIngredients();

    bool ConfirmKeepImportedSteps();

    List<string> ReadIngredients();

    List<string> ReadSteps();

    string ReadNotes();

    bool ConfirmSaveRecipe();

    bool ConfirmImportAnotherRecipe();

    bool ConfirmSearchAnotherRecipe();

    RecipeSummary? SelectRecipe(List<RecipeSummary> recipes, string title);

    Recipe ReadRecipeEdits(Recipe recipe);

    bool ConfirmDeleteRecipe(RecipeSummary recipe);

    string? ReadBackupFilePath();

    void WaitForContinue();
}
