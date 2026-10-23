using Spectre.Console;

public class RichConsoleRecipeApp : IRecipeApp
{
    private const int PageSize = 10;

    private readonly IRecipeImporterService _importerService;
    private readonly IRecipeLibraryService _recipeLibraryService;
    private readonly IRecipeBackupService _backupService;
    private readonly IRichConsoleMenu _menu;
    private readonly IRichConsoleDisplay _display;
    private readonly IRichConsoleReader _reader;

    public RichConsoleRecipeApp(
        IRecipeImporterService importerService,
        IRecipeLibraryService recipeLibraryService,
        IRecipeBackupService backupService)
        : this(
            importerService,
            recipeLibraryService,
            backupService,
            new RichConsoleMenu(),
            new RichConsoleDisplay(),
            new RichConsoleReader())
    {
    }

    public RichConsoleRecipeApp(
        IRecipeImporterService importerService,
        IRecipeLibraryService recipeLibraryService,
        IRecipeBackupService backupService,
        IRichConsoleMenu menu,
        IRichConsoleDisplay display,
        IRichConsoleReader reader)
    {
        _importerService = importerService;
        _recipeLibraryService = recipeLibraryService;
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
                    shouldWaitForContinue = ShowAllRecipes();
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

        RecipeSaveResult result = _recipeLibraryService.SaveRecipe(recipe);

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
            RecipeSaveResult saveResult = _recipeLibraryService.SaveRecipe(importedRecipe);
            ShowSaveResult(saveResult);

            if (!saveResult.IsSuccess)
            {
                _display.ShowInfo(AppTexts.RecipeNotSaved);
            }

            return;
        }

        _display.ShowInfo(AppTexts.RecipeNotSaved);
    }

    private bool ShowAllRecipes()
    {
        int pageNumber = 1;

        while (true)
        {
            PagedResult<RecipeSummary> recipesPage =
                _recipeLibraryService.GetRecipeSummariesPage(pageNumber, PageSize);

            if (recipesPage.Items.Count == 0)
            {
                _display.ShowInfo(AppTexts.NoRecipes);
                return true;
            }

            _display.ShowRecipes(recipesPage.Items);
            _display.ShowInfo(string.Format(
                AppTexts.PaginationStatus,
                recipesPage.PageNumber,
                recipesPage.TotalPages));

            PaginationAction paginationAction = _reader.ReadPaginationAction();

            if (paginationAction == PaginationAction.BackToMenu)
            {
                return false;
            }

            if (paginationAction == PaginationAction.NextPage)
            {
                if (recipesPage.HasNextPage)
                {
                    pageNumber = recipesPage.PageNumber + 1;
                }

                _display.Clear();
                continue;
            }

            if (paginationAction == PaginationAction.PreviousPage)
            {
                if (recipesPage.HasPreviousPage)
                {
                    pageNumber = recipesPage.PageNumber - 1;
                }

                _display.Clear();
                continue;
            }
        }
    }

    private bool SearchRecipe()
    {
        while (true)
        {
            if (!_recipeLibraryService.HasRecipesInCurrentContext())
            {
                _display.ShowInfo(AppTexts.NoRecipes);
                return true;
            }

            string? searchText = _reader.ReadSearchText();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return false;
            }

            ShowSearchResults(searchText);

            if (!_reader.ConfirmSearchAnotherRecipe())
            {
                return false;
            }

            _display.Clear();
        }
    }

    private void ShowSearchResults(string searchText)
    {
        int pageNumber = 1;

        while (true)
        {
            PagedResult<RecipeSummary> searchResultsPage =
                _recipeLibraryService.SearchRecipesPage(searchText, pageNumber, PageSize);

            _display.ShowRecipes(searchResultsPage.Items, AppTexts.SearchResults, AppTexts.NoSearchResults);
            _display.ShowInfo(string.Format(
                AppTexts.SearchResultsTotal,
                searchResultsPage.TotalItems));

            if (searchResultsPage.TotalItems == 0)
            {
                return;
            }

            _display.ShowInfo(string.Format(
                AppTexts.PaginationStatus,
                searchResultsPage.PageNumber,
                searchResultsPage.TotalPages));

            PaginationAction paginationAction = _reader.ReadPaginationAction();

            if (paginationAction == PaginationAction.BackToMenu)
            {
                return;
            }

            if (paginationAction == PaginationAction.NextPage)
            {
                if (searchResultsPage.HasNextPage)
                {
                    pageNumber = searchResultsPage.PageNumber + 1;
                }

                _display.Clear();
                continue;
            }

            if (paginationAction == PaginationAction.PreviousPage)
            {
                if (searchResultsPage.HasPreviousPage)
                {
                    pageNumber = searchResultsPage.PageNumber - 1;
                }

                _display.Clear();
                continue;
            }
        }
    }

    private bool ViewRecipeDetails()
    {
        RecipeSummary? selectedRecipe = SelectExistingRecipe(RichConsoleTexts.SelectRecipeToView, out bool returnedToMainMenu);

        if (selectedRecipe == null)
        {
            return !returnedToMainMenu;
        }

        Recipe? recipe = _recipeLibraryService.GetRecipeById(selectedRecipe.Id);

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
        RecipeSummary? selectedRecipe = SelectExistingRecipe(RichConsoleTexts.SelectRecipeToEdit, out bool returnedToMainMenu);

        if (selectedRecipe == null)
        {
            return !returnedToMainMenu;
        }

        Recipe? recipe = _recipeLibraryService.GetRecipeById(selectedRecipe.Id);

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

        RecipeSaveResult result = _recipeLibraryService.UpdateRecipe(editedRecipe);

        ShowSaveResult(result);
        return true;
    }

    private bool DeleteRecipe()
    {
        RecipeSummary? selectedRecipe = SelectExistingRecipe(RichConsoleTexts.SelectRecipeToDelete, out bool returnedToMainMenu);

        if (selectedRecipe == null)
        {
            return !returnedToMainMenu;
        }

        RecipeSummary? recipe = _recipeLibraryService.GetRecipeSummaries()
            .FirstOrDefault(recipe => recipe.Id == selectedRecipe.Id);

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

        RecipeSaveResult result = _recipeLibraryService.DeleteRecipe(recipe.Id);

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

    private RecipeSummary? SelectExistingRecipe(string title, out bool returnedToMainMenu)
    {
        returnedToMainMenu = false;
        List<RecipeSummary> recipes = _recipeLibraryService.GetRecipeSummaries();

        if (recipes.Count == 0)
        {
            _display.ShowInfo(AppTexts.NoRecipes);
            return null;
        }

        RecipeSummary? selectedRecipe = _reader.SelectRecipe(recipes, title);

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
