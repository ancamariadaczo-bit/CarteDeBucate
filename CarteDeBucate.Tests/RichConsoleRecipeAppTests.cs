public class RichConsoleRecipeAppTests
{
    [Fact]
    public async Task Run_WithShowAllRecipesOption_ShouldRenderRecipes()
    {
        List<RecipeSummary> recipes =
        [
            new RecipeSummary { Id = 1, Name = "Banana Bread" },
            new RecipeSummary { Id = 2, Name = "Pancakes" }
        ];

        FakeRecipeImporterService importerService = new();
        FakeRecipeLibraryService libraryService = new()
        {
            RecipeSummariesPageToReturn =
                new PagedResult<RecipeSummary>(recipes, 1, 10, recipes.Count)
        };
        FakeRecipeBackupService backupService = new();
        FakeRichConsoleMenu menu = CreateMenu(MainMenuOption.ShowRecipes);
        FakeRichConsoleDisplay display = new();
        FakeRichConsoleReader reader = new();

        RichConsoleRecipeApp app = CreateApp(importerService, libraryService, backupService, menu, display, reader);

        await app.RunAsync();

        Assert.True(libraryService.GetRecipeSummariesPageWasCalled);
        Assert.Equal(1, libraryService.PageNumberPassedToGetRecipeSummariesPage);
        Assert.Equal(10, libraryService.PageSizePassedToGetRecipeSummariesPage);
        Assert.False(libraryService.GetRecipeSummariesWasCalled);
        Assert.True(reader.ReadPaginationActionWasCalled);
        Assert.True(display.ShowRecipesWasCalled);
        Assert.Equal(recipes, display.RecipesPassedToShowRecipes);
    }

    [Fact]
    public async Task Run_WithShowAllRecipesOption_ShouldNavigateToNextPage()
    {
        List<RecipeSummary> firstPageRecipes =
        [
            new RecipeSummary { Id = 1, Name = "Banana Bread" }
        ];
        List<RecipeSummary> secondPageRecipes =
        [
            new RecipeSummary { Id = 2, Name = "Pancakes" }
        ];

        FakeRecipeImporterService importerService = new();
        FakeRecipeLibraryService libraryService = new();
        libraryService.RecipeSummariesPagesToReturn.Enqueue(
            new PagedResult<RecipeSummary>(firstPageRecipes, 1, 10, 20));
        libraryService.RecipeSummariesPagesToReturn.Enqueue(
            new PagedResult<RecipeSummary>(secondPageRecipes, 2, 10, 20));

        FakeRecipeBackupService backupService = new();
        FakeRichConsoleMenu menu = CreateMenu(MainMenuOption.ShowRecipes);
        FakeRichConsoleDisplay display = new();
        FakeRichConsoleReader reader = new();
        reader.PaginationActionsToReturn.Enqueue(PaginationAction.NextPage);
        reader.PaginationActionsToReturn.Enqueue(PaginationAction.BackToMenu);

        RichConsoleRecipeApp app = CreateApp(importerService, libraryService, backupService, menu, display, reader);

        await app.RunAsync();

        Assert.Equal(
            new List<int> { 1, 2 },
            libraryService.PageNumbersPassedToGetRecipeSummariesPage);
        Assert.True(reader.ReadPaginationActionWasCalled);
        Assert.True(display.ShowRecipesWasCalled);
        Assert.Equal(2, display.ClearCallCount);
    }

    [Fact]
    public async Task Run_WithShowAllRecipesOption_ShouldStayOnCurrentPageWhenPaginationBoundaryIsReached()
    {
        List<RecipeSummary> firstPageRecipes =
        [
            new RecipeSummary { Id = 1, Name = "Banana Bread" }
        ];
        List<RecipeSummary> secondPageRecipes =
        [
            new RecipeSummary { Id = 2, Name = "Pancakes" }
        ];

        FakeRecipeImporterService importerService = new();
        FakeRecipeLibraryService libraryService = new();
        libraryService.RecipeSummariesPagesToReturn.Enqueue(
            new PagedResult<RecipeSummary>(firstPageRecipes, 1, 10, 20));
        libraryService.RecipeSummariesPagesToReturn.Enqueue(
            new PagedResult<RecipeSummary>(firstPageRecipes, 1, 10, 20));
        libraryService.RecipeSummariesPagesToReturn.Enqueue(
            new PagedResult<RecipeSummary>(secondPageRecipes, 2, 10, 20));
        libraryService.RecipeSummariesPagesToReturn.Enqueue(
            new PagedResult<RecipeSummary>(secondPageRecipes, 2, 10, 20));

        FakeRecipeBackupService backupService = new();
        FakeRichConsoleMenu menu = CreateMenu(MainMenuOption.ShowRecipes);
        FakeRichConsoleDisplay display = new();
        FakeRichConsoleReader reader = new();
        reader.PaginationActionsToReturn.Enqueue(PaginationAction.PreviousPage);
        reader.PaginationActionsToReturn.Enqueue(PaginationAction.NextPage);
        reader.PaginationActionsToReturn.Enqueue(PaginationAction.NextPage);
        reader.PaginationActionsToReturn.Enqueue(PaginationAction.BackToMenu);

        RichConsoleRecipeApp app = CreateApp(importerService, libraryService, backupService, menu, display, reader);

        await app.RunAsync();

        Assert.Equal(
            new List<int> { 1, 1, 2, 2 },
            libraryService.PageNumbersPassedToGetRecipeSummariesPage);
        Assert.Equal(4, display.ClearCallCount);
    }

    [Fact]
    public async Task Run_WithShowAllRecipesOptionAndNoRecipes_ShouldShowEmptyListWithoutPaginationPrompt()
    {
        FakeRecipeImporterService importerService = new();
        FakeRecipeLibraryService libraryService = new()
        {
            RecipeSummariesPageToReturn =
                new PagedResult<RecipeSummary>(new List<RecipeSummary>(), 1, 10, 0)
        };

        FakeRecipeBackupService backupService = new();
        FakeRichConsoleMenu menu = CreateMenu(MainMenuOption.ShowRecipes);
        FakeRichConsoleDisplay display = new();
        FakeRichConsoleReader reader = new();

        RichConsoleRecipeApp app = CreateApp(importerService, libraryService, backupService, menu, display, reader);

        await app.RunAsync();

        Assert.False(display.ShowRecipesWasCalled);
        Assert.True(display.ShowInfoWasCalled);
        Assert.Contains(AppTexts.NoRecipes, display.Messages);
        Assert.False(reader.ReadPaginationActionWasCalled);
        Assert.True(reader.WaitForContinueWasCalled);
    }

    [Fact]
    public async Task Run_WithAddRecipeOption_ShouldSaveRecipe()
    {
        Recipe recipe = new Recipe { Name = "New Recipe" };
        FakeRecipeImporterService importerService = new();

        FakeRecipeLibraryService libraryService = new()
        {
            SaveResultToReturn = new RecipeSaveResult { IsSuccess = true, Message = "Saved." }
        };
        FakeRichConsoleReader reader = new() { RecipeToReturn = recipe };

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            libraryService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.AddRecipe),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeWasCalled);
        Assert.True(libraryService.SaveRecipeWasCalled);
        Assert.Equal(recipe, libraryService.RecipePassedToSaveRecipe);
    }

    [Fact]
    public async Task Run_WithSearchRecipeOption_ShouldSearchRecipes()
    {
        List<RecipeSummary> recipes = [new RecipeSummary { Id = 1, Name = "Cake" }];
        List<RecipeSummary> searchResults = [new RecipeSummary { Id = 1, Name = "Cake" }];
        FakeRecipeImporterService importerService = new();
        FakeRecipeLibraryService libraryService = new()
        {
            RecipeSummariesToReturn = recipes,
            SearchResultsToReturn = searchResults
        };

        FakeRichConsoleReader reader = new() { SearchText = "cake" };
        FakeRichConsoleDisplay display = new();

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            libraryService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.SearchRecipe),
            display,
            reader);

        await app.RunAsync();

        Assert.True(reader.ReadSearchTextWasCalled);
        Assert.True(libraryService.SearchRecipesWasCalled);
        Assert.Equal("cake", libraryService.SearchTextPassedToSearchRecipes);
        Assert.Equal(searchResults, display.RecipesPassedToShowRecipes);
    }

    [Fact]
    public async Task Run_WithViewRecipeDetailsOption_ShouldFetchSelectedRecipe()
    {
        RecipeSummary selectedRecipe = new RecipeSummary { Id = 7, Name = "Soup" };
        Recipe recipe = new Recipe { Id = 7, Name = "Soup" };
        FakeRecipeImporterService importerService = new();
        FakeRecipeLibraryService libraryService = new()
        {
            RecipeSummariesToReturn = [selectedRecipe],
            RecipeToReturn = recipe
        };

        FakeRichConsoleReader reader = new() { SelectedRecipe = selectedRecipe };
        FakeRichConsoleDisplay display = new();

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            libraryService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.ViewRecipeDetails),
            display,
            reader);

        await app.RunAsync();

        Assert.True(reader.SelectRecipeWasCalled);
        Assert.True(libraryService.GetRecipeByIdWasCalled);
        Assert.Equal(7, libraryService.RecipeIdPassedToGetRecipeById);
        Assert.Equal(recipe, display.RecipePassedToShowRecipeDetails);
    }

    [Fact]
    public async Task Run_WithEditRecipeOption_ShouldFetchSelectedRecipeAndUpdate()
    {
        RecipeSummary selectedRecipe = new RecipeSummary { Id = 4, Name = "Old" };
        Recipe recipe = new Recipe { Id = 4, Name = "Old" };
        Recipe editedRecipe = new Recipe { Id = 4, Name = "New" };
        FakeRecipeImporterService importerService = new();
        FakeRecipeLibraryService libraryService = new()
        {
            RecipeSummariesToReturn = [selectedRecipe],
            RecipeToReturn = recipe,
            UpdateResultToReturn = new RecipeSaveResult { IsSuccess = true, Message = "Updated." }
        };

        FakeRichConsoleReader reader = new()
        {
            SelectedRecipe = selectedRecipe,
            EditedRecipe = editedRecipe
        };

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            libraryService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.EditRecipe),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.Equal(4, libraryService.RecipeIdPassedToGetRecipeById);
        Assert.True(reader.ReadRecipeEditsWasCalled);
        Assert.Equal(recipe, reader.RecipePassedToReadRecipeEdits);
        Assert.True(libraryService.UpdateRecipeWasCalled);
        Assert.Equal(editedRecipe, libraryService.RecipePassedToUpdateRecipe);
    }

    [Fact]
    public async Task Run_WithDeleteRecipeOption_ShouldDeleteSelectedRecipe()
    {
        RecipeSummary selectedRecipe = new RecipeSummary { Id = 9, Name = "Toast" };
        FakeRecipeImporterService importerService = new();
        FakeRecipeLibraryService libraryService = new()
        {
            RecipeSummariesToReturn = [selectedRecipe],
            DeleteResultToReturn = new RecipeSaveResult { IsSuccess = true, Message = "Deleted." }
        };

        FakeRichConsoleReader reader = new() { SelectedRecipe = selectedRecipe };

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            libraryService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.DeleteRecipe),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.True(reader.ConfirmDeleteRecipeWasCalled);
        Assert.Equal(selectedRecipe, reader.RecipePassedToConfirmDeleteRecipe);
        Assert.True(libraryService.DeleteRecipeWasCalled);
        Assert.Equal(9, libraryService.RecipeIdPassedToDeleteRecipe);
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
            }
        };
        FakeRecipeLibraryService libraryService = new()
        {
            SaveResultToReturn = new RecipeSaveResult { IsSuccess = true, Message = "Saved." }
        };

        FakeRichConsoleReader reader = new()
        {
            RecipeUrlToImport = "https://example.com/recipe",
            SaveRecipeConfirmation = true
        };

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            libraryService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.ImportRecipeFromUrl),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.True(importerService.ImportRecipeFromUrlAsyncWasCalled);
        Assert.Equal("https://example.com/recipe", importerService.UrlPassedToImportRecipeFromUrlAsync);
        Assert.True(libraryService.SaveRecipeWasCalled);
        Assert.Equal(importedRecipe, libraryService.RecipePassedToSaveRecipe);
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
        FakeRecipeLibraryService libraryService = new();

        RichConsoleRecipeApp app = CreateApp(
            importerService,
            libraryService,
            new FakeRecipeBackupService(),
            CreateMenu(MainMenuOption.ImportRecipeFromUrl),
            new FakeRichConsoleDisplay(),
            reader);

        await app.RunAsync();

        Assert.True(importerService.ImportRecipeFromUrlAsyncWasCalled);
        Assert.False(libraryService.SaveRecipeWasCalled);
    }

    [Fact]
    public async Task Run_WithExportBackupOption_ShouldCallBackupService()
    {
        FakeRecipeBackupService backupService = new();
        FakeRichConsoleReader reader = new() { BackupFilePath = "/tmp/recipes.json" };

        RichConsoleRecipeApp app = CreateApp(
            new FakeRecipeImporterService(),
            new FakeRecipeLibraryService(),
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
            new FakeRecipeLibraryService(),
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
        IRecipeLibraryService recipeLibraryService,
        IRecipeBackupService backupService,
        IRichConsoleMenu menu,
        IRichConsoleDisplay display,
        IRichConsoleReader reader)
    {
        return new RichConsoleRecipeApp(
            importerService,
            recipeLibraryService,
            backupService,
            menu,
            display,
            reader);
    }
}
