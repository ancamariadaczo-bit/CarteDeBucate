public class FakeRichConsoleReader : IRichConsoleReader
{
    public Recipe RecipeToReturn { get; set; } = new();
    public string RecipeUrlToImport { get; set; } = "";
    public string SearchText { get; set; } = "";
    public bool KeepImportedIngredients { get; set; } = true;
    public bool KeepImportedSteps { get; set; } = true;
    public List<string> IngredientsToReturn { get; set; } = new();
    public List<string> StepsToReturn { get; set; } = new();
    public string NotesToReturn { get; set; } = "";
    public bool SaveRecipeConfirmation { get; set; } = true;
    public bool ImportAnotherRecipeConfirmation { get; set; }
    public bool SearchAnotherRecipeConfirmation { get; set; }
    public RecipeSummary? SelectedRecipe { get; set; }
    public PaginationAction PaginationActionToReturn { get; set; } = PaginationAction.BackToMenu;
    public Queue<PaginationAction> PaginationActionsToReturn { get; } = new();
    public Recipe? EditedRecipe { get; set; }
    public bool DeleteRecipeConfirmation { get; set; } = true;
    public string BackupFilePath { get; set; } = "";

    public bool ReadRecipeWasCalled { get; private set; }
    public bool ReadRecipeUrlToImportWasCalled { get; private set; }
    public bool ReadSearchTextWasCalled { get; private set; }
    public bool ConfirmKeepImportedIngredientsWasCalled { get; private set; }
    public bool ConfirmKeepImportedStepsWasCalled { get; private set; }
    public bool ConfirmSaveRecipeWasCalled { get; private set; }
    public bool ConfirmImportAnotherRecipeWasCalled { get; private set; }
    public bool ConfirmSearchAnotherRecipeWasCalled { get; private set; }
    public bool SelectRecipeWasCalled { get; private set; }
    public bool ReadPaginationActionWasCalled { get; private set; }
    public bool ReadRecipeEditsWasCalled { get; private set; }
    public bool ConfirmDeleteRecipeWasCalled { get; private set; }
    public bool ReadBackupFilePathWasCalled { get; private set; }
    public bool WaitForContinueWasCalled { get; private set; }

    public Recipe? RecipePassedToReadRecipeEdits { get; private set; }
    public RecipeSummary? RecipePassedToConfirmDeleteRecipe { get; private set; }

    public Recipe? ReadRecipe()
    {
        ReadRecipeWasCalled = true;
        return RecipeToReturn;
    }

    public string? ReadRecipeUrlToImport()
    {
        ReadRecipeUrlToImportWasCalled = true;
        return RecipeUrlToImport;
    }

    public string? ReadSearchText()
    {
        ReadSearchTextWasCalled = true;
        return SearchText;
    }

    public bool ConfirmKeepImportedIngredients()
    {
        ConfirmKeepImportedIngredientsWasCalled = true;
        return KeepImportedIngredients;
    }

    public bool ConfirmKeepImportedSteps()
    {
        ConfirmKeepImportedStepsWasCalled = true;
        return KeepImportedSteps;
    }

    public List<string> ReadIngredients()
    {
        return IngredientsToReturn;
    }

    public List<string> ReadSteps()
    {
        return StepsToReturn;
    }

    public string ReadNotes()
    {
        return NotesToReturn;
    }

    public bool ConfirmSaveRecipe()
    {
        ConfirmSaveRecipeWasCalled = true;
        return SaveRecipeConfirmation;
    }

    public bool ConfirmImportAnotherRecipe()
    {
        ConfirmImportAnotherRecipeWasCalled = true;
        return ImportAnotherRecipeConfirmation;
    }

    public bool ConfirmSearchAnotherRecipe()
    {
        ConfirmSearchAnotherRecipeWasCalled = true;
        return SearchAnotherRecipeConfirmation;
    }

    public RecipeSummary? SelectRecipe(List<RecipeSummary> recipes, string title)
    {
        SelectRecipeWasCalled = true;
        return SelectedRecipe ?? recipes[0];
    }

    public PaginationAction ReadPaginationAction()
    {
        ReadPaginationActionWasCalled = true;

        return PaginationActionsToReturn.Count > 0
            ? PaginationActionsToReturn.Dequeue()
            : PaginationActionToReturn;
    }

    public Recipe ReadRecipeEdits(Recipe recipe)
    {
        ReadRecipeEditsWasCalled = true;
        RecipePassedToReadRecipeEdits = recipe;
        return EditedRecipe ?? recipe;
    }

    public bool ConfirmDeleteRecipe(RecipeSummary recipe)
    {
        ConfirmDeleteRecipeWasCalled = true;
        RecipePassedToConfirmDeleteRecipe = recipe;
        return DeleteRecipeConfirmation;
    }

    public string? ReadBackupFilePath()
    {
        ReadBackupFilePathWasCalled = true;
        return BackupFilePath;
    }

    public void WaitForContinue()
    {
        WaitForContinueWasCalled = true;
    }
}
