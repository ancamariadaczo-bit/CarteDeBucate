public class FakeRichConsoleDisplay : IRichConsoleDisplay
{
    public bool ClearWasCalled { get; private set; }
    public bool ShowTitleWasCalled { get; private set; }
    public bool ShowRecipesWasCalled { get; private set; }
    public bool ShowRecipeDetailsWasCalled { get; private set; }
    public bool ShowImportedRecipeWasCalled { get; private set; }
    public bool ShowSuccessWasCalled { get; private set; }
    public bool ShowSpacedSuccessWasCalled { get; private set; }
    public bool ShowErrorWasCalled { get; private set; }
    public bool ShowInfoWasCalled { get; private set; }

    public List<Recipe>? RecipesPassedToShowRecipes { get; private set; }
    public Recipe? RecipePassedToShowRecipeDetails { get; private set; }
    public Recipe? RecipePassedToShowImportedRecipe { get; private set; }
    public List<string> Messages { get; } = new();

    public void Clear()
    {
        ClearWasCalled = true;
    }

    public void ShowTitle()
    {
        ShowTitleWasCalled = true;
    }

    public void ShowRecipes(List<Recipe> recipes, string? title = null, string? emptyMessage = null)
    {
        ShowRecipesWasCalled = true;
        RecipesPassedToShowRecipes = recipes;
    }

    public void ShowRecipeDetails(Recipe recipe, string? title = null)
    {
        ShowRecipeDetailsWasCalled = true;
        RecipePassedToShowRecipeDetails = recipe;
    }

    public void ShowImportedRecipe(Recipe recipe)
    {
        ShowImportedRecipeWasCalled = true;
        RecipePassedToShowImportedRecipe = recipe;
    }

    public void ShowSuccess(string message)
    {
        ShowSuccessWasCalled = true;
        Messages.Add(message);
    }

    public void ShowSpacedSuccess(string message)
    {
        ShowSpacedSuccessWasCalled = true;
        Messages.Add(message);
    }

    public void ShowError(string message)
    {
        ShowErrorWasCalled = true;
        Messages.Add(message);
    }

    public void ShowInfo(string message)
    {
        ShowInfoWasCalled = true;
        Messages.Add(message);
    }
}
