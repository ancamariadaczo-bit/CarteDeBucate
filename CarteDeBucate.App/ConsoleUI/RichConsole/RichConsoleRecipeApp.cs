using Spectre.Console;

public class RichConsoleRecipeApp : IRecipeApp
{
    private readonly IRecipeImporterService _importerService;
    private readonly IRecipeBackupService _backupService;
    private readonly IRichConsoleMenu _menu;
    private readonly IRichConsoleDisplay _display;
    private readonly IRichConsoleReader _reader;

    public RichConsoleRecipeApp(
        IRecipeImporterService importerService,
        IRecipeBackupService backupService)
        : this(
            importerService,
            backupService,
            new RichConsoleMenu(),
            new RichConsoleDisplay(),
            new RichConsoleReader())
    {
    }

    public RichConsoleRecipeApp(
        IRecipeImporterService importerService,
        IRecipeBackupService backupService,
        IRichConsoleMenu menu,
        IRichConsoleDisplay display,
        IRichConsoleReader reader)
    {
        _importerService = importerService;
        _backupService = backupService;
        _menu = menu;
        _display = display;
        _reader = reader;
    }

    public async Task RunAsync()
    {
        bool shouldExit = false;

        while (!shouldExit)
        {
            _display.ShowTitle();

            MainMenuOption selectedOption = _menu.ShowMainMenu();

            _display.Clear();

            switch (selectedOption)
            {
                case MainMenuOption.AddRecipe:
                    AddRecipe();
                    break;

                case MainMenuOption.ImportRecipeFromUrl:
                    await ImportFromUrlAsync();
                    break;

                case MainMenuOption.ShowRecipes:
                    ShowAllRecipes();
                    break;

                case MainMenuOption.SearchRecipe:
                    SearchRecipe();
                    break;

                case MainMenuOption.ViewRecipeDetails:
                    ViewRecipeDetails();
                    break;

                case MainMenuOption.EditRecipe:
                    EditRecipe();
                    break;

                case MainMenuOption.DeleteRecipe:
                    DeleteRecipe();
                    break;

                case MainMenuOption.ExportBackup:
                    ExportBackup();
                    break;

                case MainMenuOption.ImportBackup:
                    ImportBackup();
                    break;

                case MainMenuOption.Exit:
                    shouldExit = true;
                    _display.ShowInfo(AppTexts.AppClosed);
                    break;
            }

            if (!shouldExit)
            {
                _reader.WaitForContinue();
            }
        }
    }

    private void AddRecipe()
    {
        Recipe recipe = _reader.ReadRecipe();
        recipe.SavedAt = DateTime.Now;

        RecipeSaveResult result = _importerService.SaveRecipe(recipe);

        ShowSaveResult(result);
    }

    private async Task ImportFromUrlAsync()
    {
        string url = _reader.ReadRecipeUrlToImport();

        if (string.IsNullOrWhiteSpace(url))
        {
            _display.ShowError(AppTexts.EmptyUrl);
            return;
        }

        RecipeImportResult importResult = await AnsiConsole.Status()
            .StartAsync(AppTexts.ImportingRecipe, async _ =>
                await _importerService.ImportRecipeFromUrlAsync(url));

        if (importResult.Success)
        {
            _display.ShowSpacedSuccess(importResult.Message);
        }
        else
        {
            _display.ShowError(importResult.Message);
        }

        if (!importResult.Success || importResult.Recipe == null)
        {
            return;
        }

        Recipe importedRecipe = importResult.Recipe;

        _display.ShowImportedRecipe(importedRecipe);

        if (!_reader.ConfirmKeepImportedIngredients())
        {
            importedRecipe.Ingredients = _reader.ReadIngredients();
        }

        if (!_reader.ConfirmKeepImportedSteps())
        {
            importedRecipe.Steps = _reader.ReadSteps();
        }

        importedRecipe.Notes = _reader.ReadNotes();
        importedRecipe.SavedAt = DateTime.Now;

        if (!_reader.ConfirmSaveRecipe())
        {
            _display.ShowInfo(AppTexts.RecipeNotSaved);
            return;
        }

        RecipeSaveResult saveResult = _importerService.SaveRecipe(importedRecipe);
        ShowSaveResult(saveResult);

        if (!saveResult.IsSuccess)
        {
            _display.ShowInfo(AppTexts.RecipeNotSaved);
        }
    }

    private void ShowAllRecipes()
    {
        List<Recipe> recipes = _importerService.GetAllRecipes();

        _display.ShowRecipes(recipes);
    }

    private void SearchRecipe()
    {
        List<Recipe> recipes = _importerService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _display.ShowInfo(AppTexts.NoRecipes);
            return;
        }

        string searchText = _reader.ReadSearchText();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            _display.ShowError(AppTexts.InvalidOption);
            return;
        }

        List<Recipe> foundRecipes = _importerService.SearchRecipes(searchText);

        _display.ShowRecipes(foundRecipes, AppTexts.SearchResults, AppTexts.NoSearchResults);
    }

    private void ViewRecipeDetails()
    {
        Recipe? selectedRecipe = SelectExistingRecipe(RichConsoleTexts.SelectRecipeToView);

        if (selectedRecipe == null)
        {
            return;
        }

        Recipe? recipe = _importerService.GetRecipeById(selectedRecipe.Id);

        if (recipe == null)
        {
            _display.ShowError(AppTexts.RecipeNotFound);
            return;
        }

        _display.ShowRecipeDetails(recipe);
    }

    private void EditRecipe()
    {
        Recipe? selectedRecipe = SelectExistingRecipe(RichConsoleTexts.SelectRecipeToEdit);

        if (selectedRecipe == null)
        {
            return;
        }

        Recipe? recipe = _importerService.GetRecipeById(selectedRecipe.Id);

        if (recipe == null)
        {
            _display.ShowError(AppTexts.RecipeNotFound);
            return;
        }

        _display.ShowRecipeDetails(recipe, AppTexts.EditRecipeTitle);

        Recipe editedRecipe = _reader.ReadRecipeEdits(recipe);

        RecipeSaveResult result = _importerService.UpdateRecipe(editedRecipe);

        ShowSaveResult(result);
    }

    private void DeleteRecipe()
    {
        Recipe? selectedRecipe = SelectExistingRecipe(RichConsoleTexts.SelectRecipeToDelete);

        if (selectedRecipe == null)
        {
            return;
        }

        Recipe? recipe = _importerService.GetRecipeById(selectedRecipe.Id);

        if (recipe == null)
        {
            _display.ShowError(AppTexts.RecipeNotFound);
            return;
        }

        if (!_reader.ConfirmDeleteRecipe(recipe))
        {
            _display.ShowInfo(AppTexts.RecipeNotSaved);
            return;
        }

        RecipeSaveResult result = _importerService.DeleteRecipe(recipe.Id);

        ShowSaveResult(result);
    }

    private void ExportBackup()
    {
        _display.ShowInfo(AppTexts.EnterExportBackupFilePath);

        string backupFilePath = _reader.ReadBackupFilePath();

        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            _display.ShowError(AppTexts.InvalidBackupFile);
            return;
        }

        RecipeBackupResult result = _backupService.ExportToJson(backupFilePath);

        ShowBackupResult(result);
    }

    private void ImportBackup()
    {
        _display.ShowInfo(AppTexts.EnterImportBackupFilePath);

        string backupFilePath = _reader.ReadBackupFilePath();

        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            _display.ShowError(AppTexts.InvalidBackupFile);
            return;
        }

        RecipeBackupResult result = _backupService.ImportFromJson(backupFilePath);

        ShowBackupResult(result);
    }

    private Recipe? SelectExistingRecipe(string title)
    {
        List<Recipe> recipes = _importerService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _display.ShowInfo(AppTexts.NoRecipes);
            return null;
        }

        return _reader.SelectRecipe(recipes, title);
    }

    private void ShowSaveResult(RecipeSaveResult result)
    {
        if (result.IsSuccess)
        {
            _display.ShowSuccess(result.Message);
        }
        else
        {
            _display.ShowError(result.Message);
        }
    }

    private void ShowBackupResult(RecipeBackupResult result)
    {
        if (result.IsSuccess)
        {
            _display.ShowSuccess(result.Message);
        }
        else
        {
            _display.ShowError(result.Message);
        }
    }
}
