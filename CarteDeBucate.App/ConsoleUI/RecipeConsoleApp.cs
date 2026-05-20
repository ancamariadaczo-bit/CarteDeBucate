using System.Linq;

public class RecipeConsoleApp
{
    private readonly IRecipeService _recipeService;

    private readonly RecipeConsoleReader _reader;
    private readonly RecipeConsoleDisplay _display;

    public RecipeConsoleApp(IRecipeService recipeService)
    {
        _recipeService = recipeService;
        _reader = new RecipeConsoleReader();
        _display = new RecipeConsoleDisplay();
    }

    public async Task RunAsync()
    {
        List<MenuOption> menuOptions = CreateMenuOptions();

        bool isRunning = true;

        while (isRunning)
        {
            _display.ShowMenu(menuOptions);

            string selectedOption = Console.ReadLine() ?? "";

            Console.WriteLine();

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
                    _display.DisplayMessage(AppTexts.AppClosed);
                    break;

                default:
                    _display.DisplayMessage(AppTexts.InvalidOption);
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

        _display.DisplayMessage(result.Message);
    }

    private async Task ImportRecipeFromUrlAsync()
    {
        string url = _reader.ReadRecipeUrlToImport();

        if (string.IsNullOrWhiteSpace(url))
        {
            _display.DisplayMessage(AppTexts.EmptyUrl);
            return;
        }

        _display.DisplayMessage(AppTexts.ImportingRecipe);

        RecipeImportResult importResult = await _recipeService.ImportRecipeFromUrlAsync(url);

        _display.DisplayMessage(importResult.Message);

        if (!importResult.Success || importResult.Recipe == null)
        {
            return;
        }

        Recipe importedRecipe = importResult.Recipe;

        _display.DisplayImportedRecipe(importedRecipe);

        _reader.CompleteImportedRecipeFromConsole(importedRecipe);

        RecipeSaveResult saveResult = _recipeService.SaveRecipe(importedRecipe);

        _display.DisplayMessage(saveResult.Message);

        if (!saveResult.IsSuccess)
        {
            _display.DisplayMessage(AppTexts.RecipeNotSaved);
        }
    }

    private void ShowRecipes()
    {
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _display.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _display.DisplayRecipeList(recipes);
    }

    private void SearchRecipes()
    {
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _display.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        string searchText = _reader.ReadSearchText();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            _display.DisplayMessage(AppTexts.InvalidOption);
            return;
        }

        List<Recipe> foundRecipes = _recipeService.SearchRecipes(searchText);

        _display.DisplaySearchResults(foundRecipes);
    }

    private void ViewRecipeDetails()
    {
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _display.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _display.DisplayRecipeList(recipes);

        int recipeId = _reader.ReadRecipeIdToView();

        if (recipeId <= 0)
        {
            _display.DisplayMessage(AppTexts.InvalidRecipeId);
            return;
        }

        Recipe? recipe = _recipeService.GetRecipeById(recipeId);

        if (recipe == null)
        {
            _display.DisplayMessage(AppTexts.RecipeNotFound);
            return;
        }

        _display.DisplayRecipeDetails(recipe);
    }

    private void EditRecipe()
    {
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _display.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _display.DisplayRecipeList(recipes);

        int recipeId = _reader.ReadRecipeIdToEdit();

        if (recipeId <= 0)
        {
            _display.DisplayMessage(AppTexts.InvalidRecipeId);
            return;
        }

        Recipe? recipe = _recipeService.GetRecipeById(recipeId);

        if (recipe == null)
        {
            _display.DisplayMessage(AppTexts.RecipeNotFound);
            return;
        }

        _display.DisplayRecipeDetails(recipe);

        Recipe editedRecipe = _reader.ReadRecipeEditsFromConsole(recipe);

        RecipeSaveResult result = _recipeService.UpdateRecipe(editedRecipe);

        _display.DisplayMessage(result.Message);
    }

    private void DeleteRecipe()
    {
        List<Recipe> recipes = _recipeService.GetAllRecipes();

        if (recipes.Count == 0)
        {
            _display.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _display.DisplayRecipeList(recipes);

        int recipeId = _reader.ReadRecipeIdToDelete();

        if (recipeId <= 0)
        {
            _display.DisplayMessage(AppTexts.InvalidRecipeId);
            return;
        }

        RecipeSaveResult result = _recipeService.DeleteRecipe(recipeId);

        _display.DisplayMessage(result.Message);
    }
}