public interface IRecipeConsoleReader
{
    string ReadMenuOption();
    bool AskForSaveConfirmation();
    void CompleteImportedRecipeFromConsole(Recipe recipe);
    Recipe ReadRecipeEditsFromConsole(Recipe recipe);
    Recipe ReadRecipeFromConsole();
    int ReadRecipeIdToDelete();
    int ReadRecipeIdToEdit();
    int ReadRecipeIdToView();
    string ReadRecipeUrlToImport();
    string ReadSearchText();
}