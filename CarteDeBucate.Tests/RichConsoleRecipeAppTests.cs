public class RichConsoleRecipeAppTests
{
    [Fact]
    public async Task Run_WithShowAllRecipesOption_ShouldRenderRecipes()
    {
        List<Recipe> recipes =
        [
            new Recipe { Id = 1, Name = "Banana Bread" },
            new Recipe { Id = 2, Name = "Pancakes" }
        ];

        FakeRecipeImporterService importerService = new() { RecipesToReturn = recipes };
        FakeRecipeBackupService backupService = new();
        FakeRichConsoleMenu menu = CreateMenu(MainMenuOption.ShowRecipes);
        FakeRichConsoleDisplay display = new();
        FakeRichConsoleReader reader = new();

        RichConsoleRecipeApp app = CreateApp(importerService, backupService, menu, display, reader);

        await app.RunAsync();

        Assert.True(importerService.GetAllRecipesWasCalled);
        Assert.True(display.ShowRecipesWasCalled);
        Assert.Equal(recipes, display.RecipesPassedToShowRecipes);
    }

    [Fact]
    public async Task Run_WithAddRecipeOption_ShouldSaveRecipe()
    {
        Recipe recipe = new Recipe { Name = "New Recipe" };
        FakeRecipeImporterService importerService = new()
        {
            SaveResultToReturn = new RecipeSaveResult { IsSuccess = true, Message = "Saved." }
        };

        FakeRichConsoleReader reader = new() { RecipeToReturn = recipe };

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.AddRecipe),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeWasCalled);
        Assert.True(importerService.SaveRecipeWasCalled);
        Assert.Equal(recipe, importerService.RecipePassedToSaveRecipe);
    }

    [Fact]
    public async Task Run_WithSearchRecipeOption_ShouldSearchRecipes()
    {
        List<Recipe> recipes = [new Recipe { Id = 1, Name = "Cake" }];
        List<Recipe> searchResults = [new Recipe { Id = 1, Name = "Cake" }];
        FakeRecipeImporterService importerService = new()
        {
            RecipesToReturn = recipes,
            SearchResultsToReturn = searchResults
        };

        FakeRichConsoleReader reader = new() { SearchText = "cake" };
        FakeRichConsoleDisplay display = new();

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.SearchRecipe),
            display,
            reader);

        await app.RunAsync();

        Assert.True(reader.ReadSearchTextWasCalled);
        Assert.True(importerService.SearchRecipesWasCalled);
        Assert.Equal("cake", importerService.SearchTextPassedToSearchRecipes);
        Assert.Equal(searchResults, display.RecipesPassedToShowRecipes);
    }

    [Fact]
    public async Task Run_WithViewRecipeDetailsOption_ShouldFetchSelectedRecipe()
    {
        Recipe selectedRecipe = new Recipe { Id = 7, Name = "Soup" };
        FakeRecipeImporterService importerService = new()
        {
            RecipesToReturn = [selectedRecipe],
            RecipeToReturn = selectedRecipe
        };

        FakeRichConsoleReader reader = new() { SelectedRecipe = selectedRecipe };
        FakeRichConsoleDisplay display = new();

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.ViewRecipeDetails),
            display,
            reader);

        await app.RunAsync();

        Assert.True(reader.SelectRecipeWasCalled);
        Assert.True(importerService.GetRecipeByIdWasCalled);
        Assert.Equal(7, importerService.RecipeIdPassedToGetRecipeById);
        Assert.Equal(selectedRecipe, display.RecipePassedToShowRecipeDetails);
    }

    [Fact]
    public async Task Run_WithEditRecipeOption_ShouldFetchSelectedRecipeAndUpdate()
    {
        Recipe selectedRecipe = new Recipe { Id = 4, Name = "Old" };
        Recipe editedRecipe = new Recipe { Id = 4, Name = "New" };
        FakeRecipeImporterService importerService = new()
        {
            RecipesToReturn = [selectedRecipe],
            RecipeToReturn = selectedRecipe,
            UpdateResultToReturn = new RecipeSaveResult { IsSuccess = true, Message = "Updated." }
        };

        FakeRichConsoleReader reader = new()
        {
            SelectedRecipe = selectedRecipe,
            EditedRecipe = editedRecipe
        };

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.EditRecipe),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.Equal(4, importerService.RecipeIdPassedToGetRecipeById);
        Assert.True(reader.ReadRecipeEditsWasCalled);
        Assert.Equal(selectedRecipe, reader.RecipePassedToReadRecipeEdits);
        Assert.True(importerService.UpdateRecipeWasCalled);
        Assert.Equal(editedRecipe, importerService.RecipePassedToUpdateRecipe);
    }

    [Fact]
    public async Task Run_WithDeleteRecipeOption_ShouldFetchSelectedRecipeAndDelete()
    {
        Recipe selectedRecipe = new Recipe { Id = 9, Name = "Toast" };
        FakeRecipeImporterService importerService = new()
        {
            RecipesToReturn = [selectedRecipe],
            RecipeToReturn = selectedRecipe,
            DeleteResultToReturn = new RecipeSaveResult { IsSuccess = true, Message = "Deleted." }
        };

        FakeRichConsoleReader reader = new() { SelectedRecipe = selectedRecipe };

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.DeleteRecipe),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.Equal(9, importerService.RecipeIdPassedToGetRecipeById);
        Assert.True(reader.ConfirmDeleteRecipeWasCalled);
        Assert.True(importerService.DeleteRecipeWasCalled);
        Assert.Equal(9, importerService.RecipeIdPassedToDeleteRecipe);
    }

    [Fact]
    public async Task Run_WithSuccessfulImportAndSave_ShouldImportAndSaveRecipe()
    {
        Recipe importedRecipe = new Recipe { Name = "Imported", SourceUrl = "https://example.com/recipe" };
        FakeRecipeImporterService importerService = new()
        {
            ImportResultToReturn = new RecipeImportResult
            {
                Success = true,
                Recipe = importedRecipe,
                Message = "Imported."
            },
            SaveResultToReturn = new RecipeSaveResult { IsSuccess = true, Message = "Saved." }
        };

        FakeRichConsoleReader reader = new()
        {
            RecipeUrlToImport = "https://example.com/recipe",
            SaveRecipeConfirmation = true
        };

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.ImportRecipeFromUrl),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.True(importerService.ImportRecipeFromUrlAsyncWasCalled);
        Assert.Equal("https://example.com/recipe", importerService.UrlPassedToImportRecipeFromUrlAsync);
        Assert.True(importerService.SaveRecipeWasCalled);
        Assert.Equal(importedRecipe, importerService.RecipePassedToSaveRecipe);
    }

    [Fact]
    public async Task Run_WithFailedImport_ShouldNotSaveRecipe()
    {
        FakeRecipeImporterService importerService = new()
        {
            ImportResultToReturn = new RecipeImportResult
            {
                Success = false,
                Recipe = null,
                Message = "Failed."
            }
        };

        FakeRichConsoleReader reader = new() { RecipeUrlToImport = "https://example.com/nope" };

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.ImportRecipeFromUrl),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.True(importerService.ImportRecipeFromUrlAsyncWasCalled);
        Assert.False(importerService.SaveRecipeWasCalled);
    }

    [Fact]
    public async Task Run_WithExportBackupOption_ShouldCallBackupService()
    {
        FakeRecipeBackupService backupService = new();
        FakeRichConsoleReader reader = new() { BackupFilePath = "/tmp/recipes.json" };

        RichConsoleRecipeApp app = CreateApp(
            new FakeRecipeImporterService(),
            backupService,
            CreateMenu(MainMenuOption.ExportBackup),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.True(reader.ReadBackupFilePathWasCalled);
        Assert.True(backupService.ExportToJsonWasCalled);
        Assert.Equal("/tmp/recipes.json", backupService.FilePathPassedToExportToJson);
    }

    [Fact]
    public async Task Run_WithImportBackupOption_ShouldCallBackupService()
    {
        FakeRecipeBackupService backupService = new();
        FakeRichConsoleReader reader = new() { BackupFilePath = "/tmp/recipes.json" };

        RichConsoleRecipeApp app = CreateApp(
            new FakeRecipeImporterService(),
            backupService,
            CreateMenu(MainMenuOption.ImportBackup),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.True(reader.ReadBackupFilePathWasCalled);
        Assert.True(backupService.ImportFromJsonWasCalled);
        Assert.Equal("/tmp/recipes.json", backupService.FilePathPassedToImportFromJson);
    }

    private static FakeRichConsoleMenu CreateMenu(MainMenuOption option)
    {
        FakeRichConsoleMenu menu = new();
        menu.OptionsToReturn.Enqueue(option);
        menu.OptionsToReturn.Enqueue(MainMenuOption.Exit);

        return menu;
    }

    private static RichConsoleRecipeApp CreateApp(
        IRecipeImporterService importerService,
        IRecipeBackupService backupService,
        IRichConsoleMenu menu,
        IRichConsoleDisplay display,
        IRichConsoleReader reader)
    {
        return new RichConsoleRecipeApp(importerService, backupService, menu, display, reader);
    }
}
