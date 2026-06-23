public class FakeRecipeConsoleWriter : IRecipeConsoleWriter
{
    public bool DisplayEmptyLineWasCalled { get; private set; }
    public int DisplayEmptyLineCallCount { get; private set; }
    public bool ShowMenuWasCalled { get; private set; }
    public bool DisplayMessageWasCalled { get; private set; }
    public bool DisplayRecipesWasCalled { get; private set; }
    public bool DisplaySearchResultsWasCalled { get; private set; }
    public bool DisplayRecipeWasCalled { get; private set; }
    public bool DisplayImportedRecipeWasCalled { get; private set; }
    public bool DisplayRecipeListWasCalled { get; private set; }
    public bool DisplayRecipeDetailsWasCalled { get; private set; }

    public List<MenuOption>? MenuOptionsPassedToShowMenu { get; private set; }

    public List<string> DisplayedMessages { get; } = new();

    public List<RecipeSummary>? RecipesPassedToDisplayRecipes { get; private set; }
    public List<RecipeSummary>? RecipesPassedToDisplaySearchResults { get; private set; }
    public List<RecipeSummary>? RecipesPassedToDisplayRecipeList { get; private set; }

    public RecipeSummary? RecipePassedToDisplayRecipe { get; private set; }
    public int? IndexPassedToDisplayRecipe { get; private set; }

    public Recipe? RecipePassedToDisplayImportedRecipe { get; private set; }
    public Recipe? RecipePassedToDisplayRecipeDetails { get; private set; }

    public void DisplayEmptyLine()
    {
        DisplayEmptyLineWasCalled = true;
        DisplayEmptyLineCallCount++;
    }

    public void ShowMenu(List<MenuOption> options)
    {
        ShowMenuWasCalled = true;
        MenuOptionsPassedToShowMenu = options;
    }

    public void DisplayMessage(string message)
    {
        DisplayMessageWasCalled = true;
        DisplayedMessages.Add(message);
    }

    public void DisplayRecipes(List<RecipeSummary> recipesToDisplay)
    {
        DisplayRecipesWasCalled = true;
        RecipesPassedToDisplayRecipes = recipesToDisplay;
    }

    public void DisplaySearchResults(List<RecipeSummary> foundRecipes)
    {
        DisplaySearchResultsWasCalled = true;
        RecipesPassedToDisplaySearchResults = foundRecipes;
    }

    public void DisplayRecipe(RecipeSummary recipe, int index)
    {
        DisplayRecipeWasCalled = true;
        RecipePassedToDisplayRecipe = recipe;
        IndexPassedToDisplayRecipe = index;
    }

    public void DisplayImportedRecipe(Recipe recipe)
    {
        DisplayImportedRecipeWasCalled = true;
        RecipePassedToDisplayImportedRecipe = recipe;
    }

    public void DisplayRecipeList(List<RecipeSummary> recipesToDisplay)
    {
        DisplayRecipeListWasCalled = true;
        RecipesPassedToDisplayRecipeList = recipesToDisplay;
    }

    public void DisplayRecipeDetails(Recipe recipe)
    {
        DisplayRecipeDetailsWasCalled = true;
        RecipePassedToDisplayRecipeDetails = recipe;
    }
}
