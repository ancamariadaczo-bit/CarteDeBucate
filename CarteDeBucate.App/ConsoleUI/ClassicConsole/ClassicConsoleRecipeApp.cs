using System.Linq;

public class ClassicConsoleRecipeApp : IRecipeApp
{
    private readonly IRecipeConsoleReader _reader;
    private readonly IRecipeConsoleWriter _writer;
    private readonly IRecipeImporterService _importerService;
    private readonly IRecipeBackupService _backupService;

    public ClassicConsoleRecipeApp(
        IRecipeConsoleReader reader,
        IRecipeConsoleWriter writer,
        IRecipeImporterService importerService,
        IRecipeBackupService backupService)
    {
        _reader = reader;
        _writer = writer;
        _importerService = importerService;
        _backupService = backupService;
    }

    public async Task RunAsync()
    {
        List<MenuOption> menuOptions = CreateMenuOptions();

        bool isRunning = true;

        while (isRunning)
        {
            _writer.ShowMenu(menuOptions);

            string selectedOption = _reader.ReadMenuOption();

            _writer.DisplayEmptyLine();

            switch (selectedOption)
            {
                case MenuKeys.AddRecipe:
                    AddRecipe();
                    break;

                case MenuKeys.ImportRecipeFromUrl:
                    await ImportRecipeFromUrlAsync();
                    break;

                case MenuKeys.ShowRecipes:
                    ShowRecipes();
                    break;

                case MenuKeys.SearchRecipe:
                    SearchRecipes();
                    break;

                case MenuKeys.ViewRecipeDetails:
                    ViewRecipeDetails();
                    break;

                case MenuKeys.EditRecipe:
                    EditRecipe();
                    break;

                case MenuKeys.DeleteRecipe:
                    DeleteRecipe();
                    break;

                case MenuKeys.ExportBackup:
                    ExportBackup();
                    break;

                case MenuKeys.ImportBackup:
                    ImportBackup();
                    break;

                case MenuKeys.Exit:
                    isRunning = false;
                    _writer.DisplayMessage(AppTexts.AppClosed);
                    break;

                default:
                    _writer.DisplayMessage(AppTexts.InvalidOption);
                    break;
            }
        }
    }

    private List<MenuOption> CreateMenuOptions()
    {
        return new List<MenuOption>
        {
            new MenuOption { Key = MenuKeys.AddRecipe, Text = AppTexts.MenuAddRecipe },
            new MenuOption { Key = MenuKeys.ImportRecipeFromUrl, Text = AppTexts.MenuImportRecipeFromUrl },
            new MenuOption { Key = MenuKeys.ShowRecipes, Text = AppTexts.MenuShowRecipes },
            new MenuOption { Key = MenuKeys.SearchRecipe, Text = AppTexts.MenuSearchRecipe },
            new MenuOption { Key = MenuKeys.ViewRecipeDetails, Text = AppTexts.MenuViewRecipeDetails },
            new MenuOption { Key = MenuKeys.EditRecipe, Text = AppTexts.MenuEditRecipe },
            new MenuOption { Key = MenuKeys.DeleteRecipe, Text = AppTexts.MenuDeleteRecipe },
            new MenuOption { Key = MenuKeys.ExportBackup, Text = AppTexts.MenuExportBackup },
            new MenuOption { Key = MenuKeys.ImportBackup, Text = AppTexts.MenuImportBackup },
            new MenuOption { Key = MenuKeys.Exit, Text = AppTexts.MenuExit }
        };
    }

    private void AddRecipe()
    {
        Recipe? recipe = _reader.ReadRecipeFromConsole();

        if (recipe == null)
        {
            return;
        }

        RecipeSaveResult result = _importerService.SaveRecipe(recipe);

        _writer.DisplayMessage(result.Message);
    }

    private async Task ImportRecipeFromUrlAsync()
    {
        string url = _reader.ReadRecipeUrlToImport();

        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        _writer.DisplayMessage(AppTexts.ImportingRecipe);

        RecipeImportResult importResult = await _importerService.ImportRecipeFromUrlAsync(url);

        _writer.DisplayMessage(importResult.Message);

        if (!importResult.Success || importResult.Recipe == null)
        {
            return;
        }

        Recipe importedRecipe = importResult.Recipe;

        _writer.DisplayImportedRecipe(importedRecipe);

        _reader.CompleteImportedRecipeFromConsole(importedRecipe);

        bool shouldSave = _reader.AskForSaveConfirmation();

        if (shouldSave)
        {
            RecipeSaveResult saveResult = _importerService.SaveRecipe(importedRecipe);

            _writer.DisplayMessage(saveResult.Message);

            if (!saveResult.IsSuccess)
            {
                _writer.DisplayMessage(AppTexts.RecipeNotSaved);
            }
        }
        else
        {
            _writer.DisplayMessage(AppTexts.RecipeNotSaved);
        }
        ;
    }

    private void ShowRecipes()
    {
        List<Recipe> recipes = _importerService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _writer.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _writer.DisplayRecipeList(recipes);
    }

    private void SearchRecipes()
    {
        if (!_importerService.HasRecipesInCurrentContext())
        {
            _writer.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        string searchText = _reader.ReadSearchText();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return;
        }

        List<Recipe> foundRecipes = _importerService.SearchRecipes(searchText);

        _writer.DisplaySearchResults(foundRecipes);
    }

    private void ViewRecipeDetails()
    {
        Recipe? recipe = SelectExistingRecipe(_reader.ReadRecipeIdToView);
        if (recipe == null)
        {
            return;
        }

        _writer.DisplayRecipeDetails(recipe);
    }

    private void EditRecipe()
    {
        Recipe? recipe = SelectExistingRecipe(_reader.ReadRecipeIdToEdit);
        if (recipe == null)
        {
            return;
        }

        _writer.DisplayRecipeDetails(recipe);

        Recipe? editedRecipe = _reader.ReadRecipeEditsFromConsole(recipe);

        if (editedRecipe == null)
        {
            return;
        }

        RecipeSaveResult result = _importerService.UpdateRecipe(editedRecipe);

        _writer.DisplayMessage(result.Message);
    }

    private void DeleteRecipe()
    {
        int? recipeId = SelectRecipeId(_reader.ReadRecipeIdToDelete);

        if (recipeId == null)
        {
            return;
        }

        RecipeSaveResult result = _importerService.DeleteRecipe(recipeId.Value);

        _writer.DisplayMessage(result.Message);
    }

    private void ExportBackup()
    {
        _writer.DisplayMessage(AppTexts.EnterExportBackupFilePath);

        string backupFilePath = _reader.ReadBackupFilePath();

        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            return;
        }

        RecipeBackupResult result = _backupService.ExportToJson(backupFilePath);

        _writer.DisplayMessage(result.Message);
    }

    private void ImportBackup()
    {
        _writer.DisplayMessage(AppTexts.EnterImportBackupFilePath);

        string backupFilePath = _reader.ReadBackupFilePath();

        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            return;
        }

        RecipeBackupResult result = _backupService.ImportFromJson(backupFilePath);

        _writer.DisplayMessage(result.Message);
    }

    private Recipe? SelectExistingRecipe(Func<int?> readRecipeId)
    {
        int? recipeId = SelectRecipeId(readRecipeId);

        if (recipeId == null)
        {
            return null;
        }

        Recipe? recipe = _importerService.GetRecipeById(recipeId.Value);

        if (recipe == null)
        {
            _writer.DisplayMessage(AppTexts.RecipeNotFound);
        }

        return recipe;
    }

    private int? SelectRecipeId(Func<int?> readRecipeId)
    {
        List<Recipe> recipes = _importerService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _writer.DisplayMessage(AppTexts.NoRecipes);
            return null;
        }

        _writer.DisplayRecipeList(recipes);

        int? recipeId = readRecipeId();

        if (recipeId == null)
        {
            return null;
        }

        if (recipeId <= 0)
        {
            _writer.DisplayMessage(AppTexts.InvalidRecipeId);
            return null;
        }

        return recipeId;
    }
}
