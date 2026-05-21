using System.Linq;

public class RecipeConsoleApp
{
    private readonly IRecipeConsoleReader _reader;
    private readonly IRecipeConsoleWriter _writer;
    private readonly IRecipeService _recipeService;

    public RecipeConsoleApp(
        IRecipeConsoleReader reader,
        IRecipeConsoleWriter writer,
        IRecipeService recipeService)
    {
        _reader = reader;
        _writer = writer;
        _recipeService = recipeService;
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
            new MenuOption { Key = MenuKeys.Exit, Text = AppTexts.MenuExit }
        };
    }

    private void AddRecipe()
    {
        Recipe recipe = _reader.ReadRecipeFromConsole();

        RecipeSaveResult result = _recipeService.SaveRecipe(recipe);

        _writer.DisplayMessage(result.Message);
    }

    private async Task ImportRecipeFromUrlAsync()
    {
        string url = _reader.ReadRecipeUrlToImport();

        if (string.IsNullOrWhiteSpace(url))
        {
            _writer.DisplayMessage(AppTexts.EmptyUrl);
            return;
        }

        _writer.DisplayMessage(AppTexts.ImportingRecipe);

        RecipeImportResult importResult = await _recipeService.ImportRecipeFromUrlAsync(url);

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
            RecipeSaveResult saveResult = _recipeService.SaveRecipe(importedRecipe);

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
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _writer.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _writer.DisplayRecipeList(recipes);
    }

    private void SearchRecipes()
    {
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _writer.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        string searchText = _reader.ReadSearchText();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            _writer.DisplayMessage(AppTexts.InvalidOption);
            return;
        }

        List<Recipe> foundRecipes = _recipeService.SearchRecipes(searchText);

        _writer.DisplaySearchResults(foundRecipes);
    }

    private void ViewRecipeDetails()
    {
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _writer.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _writer.DisplayRecipeList(recipes);

        int recipeId = _reader.ReadRecipeIdToView();

        if (recipeId <= 0)
        {
            _writer.DisplayMessage(AppTexts.InvalidRecipeId);
            return;
        }

        Recipe? recipe = _recipeService.GetRecipeById(recipeId);

        if (recipe == null)
        {
            _writer.DisplayMessage(AppTexts.RecipeNotFound);
            return;
        }

        _writer.DisplayRecipeDetails(recipe);
    }

    private void EditRecipe()
    {
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _writer.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _writer.DisplayRecipeList(recipes);

        int recipeId = _reader.ReadRecipeIdToEdit();

        if (recipeId <= 0)
        {
            _writer.DisplayMessage(AppTexts.InvalidRecipeId);
            return;
        }

        Recipe? recipe = _recipeService.GetRecipeById(recipeId);

        if (recipe == null)
        {
            _writer.DisplayMessage(AppTexts.RecipeNotFound);
            return;
        }

        _writer.DisplayRecipeDetails(recipe);

        Recipe editedRecipe = _reader.ReadRecipeEditsFromConsole(recipe);

        RecipeSaveResult result = _recipeService.UpdateRecipe(editedRecipe);

        _writer.DisplayMessage(result.Message);
    }

    private void DeleteRecipe()
    {
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _writer.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _writer.DisplayRecipeList(recipes);

        int recipeId = _reader.ReadRecipeIdToDelete();

        if (recipeId <= 0)
        {
            _writer.DisplayMessage(AppTexts.InvalidRecipeId);
            return;
        }

        RecipeSaveResult result = _recipeService.DeleteRecipe(recipeId);

        _writer.DisplayMessage(result.Message);
    }
}