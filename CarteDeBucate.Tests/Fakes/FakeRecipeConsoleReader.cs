public class FakeRecipeConsoleReader : IRecipeConsoleReader
{
    public bool SaveConfirmationResult { get; set; }

    public Recipe? RecipeToReturnFromRead { get; set; }
    public Recipe? RecipeToReturnFromEdit { get; set; }

    public int RecipeIdToDelete { get; set; }
    public int RecipeIdToEdit { get; set; }
    public int RecipeIdToView { get; set; }

    public string RecipeUrlToImport { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public string BackupFilePath { get; set; } = string.Empty;

    public bool AskForSaveConfirmationWasCalled { get; private set; }
    public bool CompleteImportedRecipeFromConsoleWasCalled { get; private set; }
    public bool ReadRecipeEditsFromConsoleWasCalled { get; private set; }
    public bool ReadRecipeFromConsoleWasCalled { get; private set; }
    public bool ReadRecipeIdToDeleteWasCalled { get; private set; }
    public bool ReadRecipeIdToEditWasCalled { get; private set; }
    public bool ReadRecipeIdToViewWasCalled { get; private set; }
    public bool ReadRecipeUrlToImportWasCalled { get; private set; }
    public bool ReadSearchTextWasCalled { get; private set; }
    public bool ReadBackupFilePathWasCalled { get; private set; }

    public Recipe? RecipePassedToCompleteImportedRecipe { get; private set; }
    public Recipe? RecipePassedToReadEdits { get; private set; }
    public Queue<string> MenuOptionsToReturn { get; } = new();

    public bool ReadMenuOptionWasCalled { get; private set; }

    public string ReadMenuOption()
    {
        ReadMenuOptionWasCalled = true;

        return MenuOptionsToReturn.Count > 0
            ? MenuOptionsToReturn.Dequeue()
            : MenuKeys.Exit;
    }

    public bool AskForSaveConfirmation()
    {
        AskForSaveConfirmationWasCalled = true;
        return SaveConfirmationResult;
    }

    public void CompleteImportedRecipeFromConsole(Recipe recipe)
    {
        CompleteImportedRecipeFromConsoleWasCalled = true;
        RecipePassedToCompleteImportedRecipe = recipe;
    }

    public Recipe? ReadRecipeEditsFromConsole(Recipe recipe)
    {
        ReadRecipeEditsFromConsoleWasCalled = true;
        RecipePassedToReadEdits = recipe;

        return RecipeToReturnFromEdit ?? recipe;
    }

    public Recipe? ReadRecipeFromConsole()
    {
        ReadRecipeFromConsoleWasCalled = true;

        return RecipeToReturnFromRead
            ?? throw new InvalidOperationException("No recipe was configured for ReadRecipeFromConsole.");
    }

    public int? ReadRecipeIdToDelete()
    {
        ReadRecipeIdToDeleteWasCalled = true;
        return RecipeIdToDelete;
    }

    public int? ReadRecipeIdToEdit()
    {
        ReadRecipeIdToEditWasCalled = true;
        return RecipeIdToEdit;
    }

    public int? ReadRecipeIdToView()
    {
        ReadRecipeIdToViewWasCalled = true;
        return RecipeIdToView;
    }

    public string ReadRecipeUrlToImport()
    {
        ReadRecipeUrlToImportWasCalled = true;
        return RecipeUrlToImport;
    }

    public string ReadSearchText()
    {
        ReadSearchTextWasCalled = true;
        return SearchText;
    }

    public string ReadBackupFilePath()
    {
        ReadBackupFilePathWasCalled = true;
        return BackupFilePath;
    }
}
