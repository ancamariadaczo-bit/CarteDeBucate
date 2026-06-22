public class FakeRecipeImporterService : IRecipeImporterService
{
    public List<Recipe> RecipesToReturn { get; set; } = new();
    public Recipe? RecipeToReturn { get; set; }
    public List<Recipe> SearchResultsToReturn { get; set; } = new();

    public RecipeImportResult ImportResultToReturn { get; set; } = new();
    public RecipeSaveResult ImportAndSaveResultToReturn { get; set; } = new();
    public RecipeSaveResult SaveResultToReturn { get; set; } = new();
    public RecipeSaveResult UpdateResultToReturn { get; set; } = new();
    public RecipeSaveResult DeleteResultToReturn { get; set; } = new();

    public bool GetAllRecipesWasCalled { get; private set; }
    public bool HasRecipesInCurrentContextWasCalled { get; private set; }
    public bool GetRecipeByIdWasCalled { get; private set; }
    public bool SearchRecipesWasCalled { get; private set; }
    public bool ImportRecipeFromUrlAsyncWasCalled { get; private set; }
    public bool ImportFromUrlAndSaveAsyncWasCalled { get; private set; }
    public bool SaveRecipeWasCalled { get; private set; }
    public bool UpdateRecipeWasCalled { get; private set; }
    public bool DeleteRecipeWasCalled { get; private set; }

    public int? RecipeIdPassedToGetRecipeById { get; private set; }
    public int? RecipeIdPassedToDeleteRecipe { get; private set; }

    public string? SearchTextPassedToSearchRecipes { get; private set; }
    public string? UrlPassedToImportRecipeFromUrlAsync { get; private set; }
    public string? UrlPassedToImportFromUrlAndSaveAsync { get; private set; }

    public Recipe? RecipePassedToSaveRecipe { get; private set; }
    public Recipe? RecipePassedToUpdateRecipe { get; private set; }

    public List<Recipe> GetAllRecipes()
    {
        GetAllRecipesWasCalled = true;
        return RecipesToReturn;
    }

    public bool HasRecipesInCurrentContext()
    {
        HasRecipesInCurrentContextWasCalled = true;
        return RecipesToReturn.Count > 0;
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        GetRecipeByIdWasCalled = true;
        RecipeIdPassedToGetRecipeById = recipeId;
        return RecipeToReturn;
    }

    public List<Recipe> SearchRecipes(string searchText)
    {
        SearchRecipesWasCalled = true;
        SearchTextPassedToSearchRecipes = searchText;
        return SearchResultsToReturn;
    }

    public Task<RecipeImportResult> ImportRecipeFromUrlAsync(string url)
    {
        ImportRecipeFromUrlAsyncWasCalled = true;
        UrlPassedToImportRecipeFromUrlAsync = url;

        return Task.FromResult(ImportResultToReturn);
    }

    public Task<RecipeSaveResult> ImportFromUrlAndSaveAsync(string url)
    {
        ImportFromUrlAndSaveAsyncWasCalled = true;
        UrlPassedToImportFromUrlAndSaveAsync = url;

        return Task.FromResult(ImportAndSaveResultToReturn);
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
