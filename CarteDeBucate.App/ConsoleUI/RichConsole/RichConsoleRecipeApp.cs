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
            bool shouldWaitForContinue = true;

            switch (selectedOption)
            {
                case MainMenuOption.AddRecipe:
                    shouldWaitForContinue = AddRecipe();
                    break;

                case MainMenuOption.ImportRecipeFromUrl:
                    shouldWaitForContinue = await ImportFromUrlAsync();
                    break;

                case MainMenuOption.ShowRecipes:
                    ShowAllRecipes();
                    break;

                case MainMenuOption.SearchRecipe:
                    shouldWaitForContinue = SearchRecipe();
                    break;

                case MainMenuOption.ViewRecipeDetails:
                    shouldWaitForContinue = ViewRecipeDetails();
                    break;

                case MainMenuOption.EditRecipe:
                    shouldWaitForContinue = EditRecipe();
                    break;

                case MainMenuOption.DeleteRecipe:
                    shouldWaitForContinue = DeleteRecipe();
                    break;

                case MainMenuOption.ExportBackup:
                    shouldWaitForContinue = ExportBackup();
                    break;

                case MainMenuOption.ImportBackup:
                    shouldWaitForContinue = ImportBackup();
                    break;

                case MainMenuOption.Exit:
                    shouldExit = true;
                    _display.ShowInfo(AppTexts.AppClosed);
                    break;
            }

            if (!shouldExit && shouldWaitForContinue)
            {
                _reader.WaitForContinue();
            }
        }
    }

    private bool AddRecipe()
    {
        Recipe? recipe = _reader.ReadRecipe();
        if (recipe == null)
        {
            return false;
        }

        recipe.SavedAt = DateTime.Now;

        RecipeSaveResult result = _importerService.SaveRecipe(recipe);

        ShowSaveResult(result);
        return true;
    }

    private async Task<bool> ImportFromUrlAsync()
    {
        while (true)
        {
            string? url = _reader.ReadRecipeUrlToImport();

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
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

            if (importResult.Success && importResult.Recipe != null)
            {
                CompleteImportedRecipe(importResult.Recipe);
            }

            if (!_reader.ConfirmImportAnotherRecipe())
            {
                return false;
            }

            _display.Clear();
        }
    }

    private void CompleteImportedRecipe(Recipe importedRecipe)
    {
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

        if (_reader.ConfirmSaveRecipe())
        {
            RecipeSaveResult saveResult = _importerService.SaveRecipe(importedRecipe);
            ShowSaveResult(saveResult);

            if (!saveResult.IsSuccess)
            {
                _display.ShowInfo(AppTexts.RecipeNotSaved);
            }

            return;
        }

        _display.ShowInfo(AppTexts.RecipeNotSaved);
    }

    private void ShowAllRecipes()
    {
        List<Recipe> recipes = _importerService.GetAllRecipes();

        _display.ShowRecipes(recipes);
    }

    private bool SearchRecipe()
    {
        while (true)
        {
            List<Recipe> recipes = _importerService.GetAllRecipes();

            if (recipes.Count == 0)
            {
                _display.ShowInfo(AppTexts.NoRecipes);
                return true;
            }

            string? searchText = _reader.ReadSearchText();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return false;
            }

            List<Recipe> foundRecipes = _importerService.SearchRecipes(searchText);

            _display.ShowRecipes(foundRecipes, AppTexts.SearchResults, AppTexts.NoSearchResults);

            if (!_reader.ConfirmSearchAnotherRecipe())
            {
                return false;
            }

            _display.Clear();
        }
    }

    private bool ViewRecipeDetails()
    {
        Recipe? selectedRecipe = SelectExistingRecipe(RichConsoleTexts.SelectRecipeToView, out bool returnedToMainMenu);

        if (selectedRecipe == null)
        {
            return !returnedToMainMenu;
        }

        Recipe? recipe = _importerService.GetRecipeById(selectedRecipe.Id);

        if (recipe == null)
        {
            _display.ShowError(AppTexts.RecipeNotFound);
            return true;
        }

        _display.ShowRecipeDetails(recipe);
        return true;
    }

    private bool EditRecipe()
    {
        Recipe? selectedRecipe = SelectExistingRecipe(RichConsoleTexts.SelectRecipeToEdit, out bool returnedToMainMenu);

        if (selectedRecipe == null)
        {
            return !returnedToMainMenu;
        }

        Recipe? recipe = _importerService.GetRecipeById(selectedRecipe.Id);

        if (recipe == null)
        {
            _display.ShowError(AppTexts.RecipeNotFound);
            return true;
        }

        _display.ShowRecipeDetails(recipe, AppTexts.EditRecipeTitle);

        Recipe editedRecipe = _reader.ReadRecipeEdits(recipe);

        if (!_reader.ConfirmSaveRecipe())
        {
            _display.ShowInfo(AppTexts.RecipeNotSaved);
            return true;
        }

        RecipeSaveResult result = _importerService.UpdateRecipe(editedRecipe);

        ShowSaveResult(result);
        return true;
    }

    private bool DeleteRecipe()
    {
        Recipe? selectedRecipe = SelectExistingRecipe(RichConsoleTexts.SelectRecipeToDelete, out bool returnedToMainMenu);

        if (selectedRecipe == null)
        {
            return !returnedToMainMenu;
        }

        Recipe? recipe = _importerService.GetRecipeById(selectedRecipe.Id);

        if (recipe == null)
        {
            _display.ShowError(AppTexts.RecipeNotFound);
            return true;
        }

        if (!_reader.ConfirmDeleteRecipe(recipe))
        {
            _display.ShowInfo(AppTexts.RecipeNotDeleted);
            return true;
        }

        RecipeSaveResult result = _importerService.DeleteRecipe(recipe.Id);

        ShowSaveResult(result);
        return true;
    }

    private bool ExportBackup()
    {
        _display.ShowInfo(AppTexts.EnterExportBackupFilePath);

        string? backupFilePath = _reader.ReadBackupFilePath();

        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            return false;
        }

        RecipeBackupResult result = _backupService.ExportToJson(backupFilePath);

        ShowBackupResult(result);
        return true;
    }

    private bool ImportBackup()
    {
        _display.ShowInfo(AppTexts.EnterImportBackupFilePath);

        string? backupFilePath = _reader.ReadBackupFilePath();

        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            return false;
        }

        RecipeBackupResult result = _backupService.ImportFromJson(backupFilePath);

        ShowBackupResult(result);
        return true;
    }

    private Recipe? SelectExistingRecipe(string title, out bool returnedToMainMenu)
    {
        returnedToMainMenu = false;
        List<Recipe> recipes = _importerService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _display.ShowInfo(AppTexts.NoRecipes);
            return null;
        }

        Recipe? selectedRecipe = _reader.SelectRecipe(recipes, title);

        if (selectedRecipe == null)
        {
            returnedToMainMenu = true;
        }

        return selectedRecipe;
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
