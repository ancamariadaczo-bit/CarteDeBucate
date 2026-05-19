using System.Linq;

public class RecipeConsoleApp
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly RecipeImporter _recipeImporter;
    private readonly RecipeConsoleReader _reader;
    private readonly RecipeConsoleDisplay _display;
    private readonly List<Recipe> _recipes;

    public RecipeConsoleApp(
        IRecipeRepository recipeRepository,
        RecipeImporter recipeImporter)
    {
        _recipeRepository = recipeRepository;
        _recipeImporter = recipeImporter;
        _reader = new RecipeConsoleReader();
        _display = new RecipeConsoleDisplay();
        _recipes = _recipeRepository.GetAllRecipes();
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
                    _display.DisplayRecipeList(_recipes);
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

        if (_recipeRepository.RecipeExistsBySourceUrl(recipe.SourceUrl))
        {
            _display.DisplayMessage(AppTexts.RecipeAlreadyExists);
            _display.DisplayMessage(AppTexts.RecipeNotSaved);
            return;
        }

        _recipeRepository.AddRecipe(recipe);
        _recipes.Add(recipe);

        _display.DisplayMessage(AppTexts.RecipeAdded);
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

        RecipeImportResult importResult = await _recipeImporter.ImportFromUrlAsync(url);

        _display.DisplayMessage(importResult.Message);

        if (!importResult.Success || importResult.Recipe == null)
        {
            return;
        }

        Recipe importedRecipe = importResult.Recipe;

        _display.DisplayImportedRecipe(importedRecipe);

        if (_recipeRepository.RecipeExistsBySourceUrl(importedRecipe.SourceUrl))
        {
            _display.DisplayMessage(AppTexts.RecipeAlreadyExists);
            _display.DisplayMessage(AppTexts.RecipeNotSaved);
            return;
        }

        _reader.CompleteImportedRecipeFromConsole(importedRecipe);

        _recipeRepository.AddRecipe(importedRecipe);
        _recipes.Add(importedRecipe);

        _display.DisplayMessage(AppTexts.RecipeAdded);
    }

    private void SearchRecipes()
    {
        if (_recipes.Count == 0)
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

        List<Recipe> foundRecipes = _recipes
            .Where(recipe =>
                recipe.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                recipe.SourceUrl.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (recipe.Notes ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                recipe.Ingredients.Any(ingredient =>
                    ingredient.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                recipe.Steps.Any(step =>
                    step.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        _display.DisplaySearchResults(foundRecipes);
    }

    private void DeleteRecipe()
    {
        if (_recipes.Count == 0)
        {
            _display.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _display.DisplayRecipeList(_recipes);

        int recipeId = _reader.ReadRecipeIdToDelete();

        if (recipeId <= 0)
        {
            _display.DisplayMessage(AppTexts.InvalidRecipeId);
            return;
        }

        Recipe? recipeToDelete = _recipeRepository.GetRecipeById(recipeId);

        if (recipeToDelete == null)
        {
            _display.DisplayMessage(AppTexts.RecipeNotFound);
            return;
        }

        _recipeRepository.DeleteRecipe(recipeId);

        Recipe? recipeFromMemory = _recipes
            .FirstOrDefault(recipe => recipe.Id == recipeId);

        if (recipeFromMemory != null)
        {
            _recipes.Remove(recipeFromMemory);
        }

        _display.DisplayMessage(AppTexts.RecipeDeleted);
    }

    private void ViewRecipeDetails()
    {
        if (_recipes.Count == 0)
        {
            _display.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _display.DisplayRecipeList(_recipes);

        int recipeId = _reader.ReadRecipeIdToView();

        if (recipeId <= 0)
        {
            _display.DisplayMessage(AppTexts.InvalidRecipeId);
            return;
        }

        Recipe? recipe = _recipeRepository.GetRecipeById(recipeId);

        if (recipe == null)
        {
            _display.DisplayMessage(AppTexts.RecipeNotFound);
            return;
        }

        _display.DisplayRecipeDetails(recipe);
    }

    private void EditRecipe()
    {
        if (_recipes.Count == 0)
        {
            _display.DisplayMessage(AppTexts.NoRecipes);
            return;
        }

        _display.DisplayRecipeList(_recipes);

        int recipeId = _reader.ReadRecipeIdToEdit();

        if (recipeId <= 0)
        {
            _display.DisplayMessage(AppTexts.InvalidRecipeId);
            return;
        }

        Recipe? recipe = _recipeRepository.GetRecipeById(recipeId);

        if (recipe == null)
        {
            _display.DisplayMessage(AppTexts.RecipeNotFound);
            return;
        }

        _display.DisplayRecipeDetails(recipe);

        Recipe editedRecipe = _reader.ReadRecipeEditsFromConsole(recipe);

        _recipeRepository.UpdateRecipe(editedRecipe);

        RefreshRecipeInMemory(editedRecipe);

        _display.DisplayMessage(AppTexts.RecipeUpdated);
    }

    private void RefreshRecipeInMemory(Recipe updatedRecipe)
    {
        int recipeIndex = _recipes.FindIndex(recipe => recipe.Id == updatedRecipe.Id);

        if (recipeIndex == -1)
        {
            _recipes.Add(updatedRecipe);
            return;
        }

        _recipes[recipeIndex] = updatedRecipe;
    }
}