public class FakeRecipeLibraryService : IRecipeLibraryService
{
    public Recipe? RecipeToReturn { get; set; }
    public List<RecipeSummary> RecipeSummariesToReturn { get; set; } = new();
    public List<RecipeSummary> SearchResultsToReturn { get; set; } = new();

    public RecipeSaveResult SaveResultToReturn { get; set; } = new();
    public RecipeSaveResult UpdateResultToReturn { get; set; } = new();
    public RecipeSaveResult DeleteResultToReturn { get; set; } = new();

    public bool GetRecipeSummariesWasCalled { get; private set; }
    public bool HasRecipesInCurrentContextWasCalled { get; private set; }
    public bool GetRecipeByIdWasCalled { get; private set; }
    public bool SearchRecipesWasCalled { get; private set; }
    public bool SaveRecipeWasCalled { get; private set; }
    public bool UpdateRecipeWasCalled { get; private set; }
    public bool DeleteRecipeWasCalled { get; private set; }

    public int? RecipeIdPassedToGetRecipeById { get; private set; }
    public int? RecipeIdPassedToDeleteRecipe { get; private set; }

    public string? SearchTextPassedToSearchRecipes { get; private set; }

    public Recipe? RecipePassedToSaveRecipe { get; private set; }
    public Recipe? RecipePassedToUpdateRecipe { get; private set; }

    public List<RecipeSummary> GetRecipeSummaries()
    {
        GetRecipeSummariesWasCalled = true;
        return RecipeSummariesToReturn;
    }

    public bool HasRecipesInCurrentContext()
    {
        HasRecipesInCurrentContextWasCalled = true;
        return RecipeSummariesToReturn.Count > 0;
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        GetRecipeByIdWasCalled = true;
        RecipeIdPassedToGetRecipeById = recipeId;
        return RecipeToReturn;
    }

    public List<RecipeSummary> SearchRecipes(string searchText)
    {
        SearchRecipesWasCalled = true;
        SearchTextPassedToSearchRecipes = searchText;
        return SearchResultsToReturn;
    }

    public RecipeSaveResult SaveRecipe(Recipe recipe)
    {
        SaveRecipeWasCalled = true;
        RecipePassedToSaveRecipe = recipe;

        return SaveResultToReturn;
    }

    public RecipeSaveResult UpdateRecipe(Recipe recipe)
    {
        UpdateRecipeWasCalled = true;
        RecipePassedToUpdateRecipe = recipe;

        return UpdateResultToReturn;
    }

    public RecipeSaveResult DeleteRecipe(int recipeId)
    {
        DeleteRecipeWasCalled = true;
        RecipeIdPassedToDeleteRecipe = recipeId;

        return DeleteResultToReturn;
    }
}
