public class RecipeConsoleAppTests
{
    [Fact]
    public async Task Run_WithExitOption_ShouldShowMenuAndStop()
    {
        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader();
        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();
        FakeRecipeImporterService importerService = new FakeRecipeImporterService();
        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importerService, new FakeRecipeLibraryService(), backupService);

        await app.RunAsync();

        Assert.True(writer.ShowMenuWasCalled);
        Assert.True(reader.ReadMenuOptionWasCalled);
        Assert.False(importerService.ImportRecipeFromUrlAsyncWasCalled);
    }

    [Fact]
    public async Task Run_WithShowAllRecipesOption_ShouldDisplayAllRecipes()
    {
        List<RecipeSummary> recipes = new()
    {
        new RecipeSummary { Id = 1, Name = "Banana Bread" },
        new RecipeSummary { Id = 2, Name = "Pancakes" }
    };

        FakeRecipeImporterService importService = new FakeRecipeImporterService();
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            RecipeSummariesToReturn = recipes
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader();
        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.ShowRecipes);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(libraryService.GetRecipeSummariesWasCalled);
        Assert.True(writer.DisplayRecipeListWasCalled);
        Assert.Equal(recipes, writer.RecipesPassedToDisplayRecipeList);
    }

    [Fact]
    public async Task Run_WithViewRecipeOption_ShouldDisplayRecipeDetails()
    {
        Recipe recipe = new Recipe { Id = 3, Name = "Keto Cake" };
        List<RecipeSummary> recipes = [new RecipeSummary { Id = 3, Name = "Keto Cake" }];

        FakeRecipeImporterService importService = new FakeRecipeImporterService();
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            RecipeSummariesToReturn = recipes,
            RecipeToReturn = recipe
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            RecipeIdToView = 3
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.ViewRecipeDetails);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeIdToViewWasCalled);
        Assert.True(libraryService.GetRecipeByIdWasCalled);
        Assert.Equal(3, libraryService.RecipeIdPassedToGetRecipeById);

        Assert.True(writer.DisplayRecipeDetailsWasCalled);
        Assert.Equal(recipe, writer.RecipePassedToDisplayRecipeDetails);
    }

    [Fact]
    public async Task Run_WithViewRecipeOptionAndMissingRecipe_ShouldDisplayMessage()
    {
        List<RecipeSummary> recipes = new()
    {
        new RecipeSummary { Id = 1, Name = "Banana Bread" },
        new RecipeSummary { Id = 2, Name = "Pancakes" }
    };

        FakeRecipeImporterService importService = new FakeRecipeImporterService();
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            RecipeSummariesToReturn = recipes,
            RecipeToReturn = null
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            RecipeIdToView = 99
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.ViewRecipeDetails);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(libraryService.GetRecipeByIdWasCalled);
        Assert.True(writer.DisplayMessageWasCalled);
        Assert.False(writer.DisplayRecipeDetailsWasCalled);
    }

    [Fact]
    public async Task Run_WithSearchOption_ShouldDisplaySearchResults()
    {
        List<RecipeSummary> recipes = new()
    {
        new RecipeSummary { Id = 1, Name = "Banana Bread" },
        new RecipeSummary { Id = 2, Name = "Keto Pancakes" }
    };

        List<RecipeSummary> searchResults = new()
    {
        new RecipeSummary { Id = 2, Name = "Keto Pancakes" }
    };

        FakeRecipeImporterService importService = new FakeRecipeImporterService();
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            RecipeSummariesToReturn = recipes,
            SearchResultsToReturn = searchResults
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            SearchText = "keto"
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.SearchRecipe);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadSearchTextWasCalled);
        Assert.True(libraryService.SearchRecipesWasCalled);
        Assert.Equal("keto", libraryService.SearchTextPassedToSearchRecipes);

        Assert.True(writer.DisplaySearchResultsWasCalled);
        Assert.Equal(searchResults, writer.RecipesPassedToDisplaySearchResults);
    }

    [Fact]
    public async Task Run_WithDeleteRecipeOption_ShouldDeleteRecipe()
    {
        List<RecipeSummary> recipes = new()
        {
            new RecipeSummary { Id = 1, Name = "Banana Bread" },
            new RecipeSummary { Id = 2, Name = "Keto Pancakes" }
        };

        RecipeSaveResult deleteResult = new RecipeSaveResult
        {
            IsSuccess = true,
            Message = "Recipe deleted."
        };

        FakeRecipeImporterService importService = new FakeRecipeImporterService();
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            RecipeSummariesToReturn = recipes,
            DeleteResultToReturn = deleteResult
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            RecipeIdToDelete = 5
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.DeleteRecipe);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeIdToDeleteWasCalled);
        Assert.True(libraryService.DeleteRecipeWasCalled);
        Assert.Equal(5, libraryService.RecipeIdPassedToDeleteRecipe);

        Assert.True(writer.DisplayMessageWasCalled);
        Assert.Contains(deleteResult.Message, writer.DisplayedMessages);
    }

    [Fact]
    public async Task Run_WithAddRecipeOption_ShouldSaveRecipe()
    {
        Recipe recipe = new Recipe
        {
            Name = "New Recipe"
        };

        RecipeSaveResult saveResult = new RecipeSaveResult
        {
            IsSuccess = true,
            Message = "Recipe saved."
        };

        FakeRecipeImporterService importService = new FakeRecipeImporterService();
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = saveResult
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            RecipeToReturnFromRead = recipe
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.AddRecipe);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeFromConsoleWasCalled);
        Assert.True(libraryService.SaveRecipeWasCalled);
        Assert.Equal(recipe, libraryService.RecipePassedToSaveRecipe);

        Assert.True(writer.DisplayMessageWasCalled);
        Assert.Contains(saveResult.Message, writer.DisplayedMessages);
    }

    [Fact]
    public async Task Run_WithEditRecipeOption_ShouldUpdateRecipe()
    {
        Recipe existingRecipe = new Recipe
        {
            Id = 4,
            Name = "Old Name"
        };

        Recipe editedRecipe = new Recipe
        {
            Id = 4,
            Name = "New Name"
        };

        RecipeSaveResult updateResult = new RecipeSaveResult
        {
            IsSuccess = true,
            Message = "Recipe updated."
        };

        FakeRecipeImporterService importService = new FakeRecipeImporterService();
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            RecipeSummariesToReturn = [new RecipeSummary { Id = 4, Name = "Old Name" }],
            RecipeToReturn = existingRecipe,
            UpdateResultToReturn = updateResult
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            RecipeIdToEdit = 4,
            RecipeToReturnFromEdit = editedRecipe
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.EditRecipe);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeIdToEditWasCalled);
        Assert.True(libraryService.GetRecipeByIdWasCalled);
        Assert.Equal(4, libraryService.RecipeIdPassedToGetRecipeById);

        Assert.True(reader.ReadRecipeEditsFromConsoleWasCalled);
        Assert.Equal(existingRecipe, reader.RecipePassedToReadEdits);

        Assert.True(libraryService.UpdateRecipeWasCalled);
        Assert.Equal(editedRecipe, libraryService.RecipePassedToUpdateRecipe);

        Assert.True(writer.DisplayMessageWasCalled);
        Assert.Contains(updateResult.Message, writer.DisplayedMessages);
    }

    [Fact]
    public async Task Run_WithEditRecipeOptionAndMissingRecipe_ShouldNotUpdateRecipe()
    {
        List<RecipeSummary> recipes = new()
        {
            new RecipeSummary { Id = 1, Name = "Banana Bread" },
            new RecipeSummary { Id = 2, Name = "Keto Pancakes" }
        };

        FakeRecipeImporterService importService = new FakeRecipeImporterService();
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            RecipeSummariesToReturn = recipes,
            RecipeToReturn = null
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            RecipeIdToEdit = 99
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.EditRecipe);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(libraryService.GetRecipeByIdWasCalled);
        Assert.False(reader.ReadRecipeEditsFromConsoleWasCalled);
        Assert.False(libraryService.UpdateRecipeWasCalled);

        Assert.True(writer.DisplayMessageWasCalled);
    }

    [Fact]
    public async Task RunAsync_WithSuccessfulImportAndSaveConfirmation_ShouldSaveImportedRecipe()
    {
        Recipe importedRecipe = new Recipe
        {
            Name = "Imported Recipe",
            SourceUrl = "https://example.com/recipe"
        };

        RecipeImportResult importResult = new RecipeImportResult
        {
            Success = true,
            Recipe = importedRecipe,
            Message = "Import successful."
        };

        RecipeSaveResult saveResult = new RecipeSaveResult
        {
            IsSuccess = true,
            Message = "Recipe saved."
        };

        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            ImportResultToReturn = importResult
        };
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = saveResult
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            RecipeUrlToImport = "https://example.com/recipe",
            SaveConfirmationResult = true
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.ImportRecipeFromUrl);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeUrlToImportWasCalled);
        Assert.True(importService.ImportRecipeFromUrlAsyncWasCalled);
        Assert.Equal("https://example.com/recipe", importService.UrlPassedToImportRecipeFromUrlAsync);

        Assert.True(writer.DisplayImportedRecipeWasCalled);
        Assert.Equal(importedRecipe, writer.RecipePassedToDisplayImportedRecipe);

        Assert.True(reader.CompleteImportedRecipeFromConsoleWasCalled);
        Assert.Equal(importedRecipe, reader.RecipePassedToCompleteImportedRecipe);

        Assert.True(reader.AskForSaveConfirmationWasCalled);

        Assert.True(libraryService.SaveRecipeWasCalled);
        Assert.Equal(importedRecipe, libraryService.RecipePassedToSaveRecipe);

        Assert.True(writer.DisplayMessageWasCalled);
        Assert.Contains(saveResult.Message, writer.DisplayedMessages);
    }

    [Fact]
    public async Task RunAsync_WithSuccessfulImportAndNoSaveConfirmation_ShouldNotSaveImportedRecipe()
    {
        Recipe importedRecipe = new Recipe
        {
            Name = "Imported Recipe",
            SourceUrl = "https://example.com/recipe"
        };

        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            ImportResultToReturn = new RecipeImportResult
            {
                Success = true,
                Recipe = importedRecipe,
                Message = "Import successful."
            }
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            RecipeUrlToImport = "https://example.com/recipe",
            SaveConfirmationResult = false
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.ImportRecipeFromUrl);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService();
        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(importService.ImportRecipeFromUrlAsyncWasCalled);
        Assert.True(writer.DisplayImportedRecipeWasCalled);
        Assert.True(reader.AskForSaveConfirmationWasCalled);

        Assert.False(libraryService.SaveRecipeWasCalled);
    }

    [Fact]
    public async Task RunAsync_WithFailedImport_ShouldDisplayErrorAndNotSaveRecipe()
    {
        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            ImportResultToReturn = new RecipeImportResult
            {
                Success = false,
                Recipe = null,
                Message = "Pagina nu pare să fie o rețetă."
            }
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader
        {
            RecipeUrlToImport = "https://example.com/not-a-recipe"
        };

        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.ImportRecipeFromUrl);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService();
        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, libraryService, backupService);

        await app.RunAsync();

        Assert.True(importService.ImportRecipeFromUrlAsyncWasCalled);

        Assert.True(writer.DisplayMessageWasCalled);
        Assert.Contains("Pagina nu pare să fie o rețetă.", writer.DisplayedMessages);

        Assert.False(writer.DisplayImportedRecipeWasCalled);
        Assert.False(reader.CompleteImportedRecipeFromConsoleWasCalled);
        Assert.False(reader.AskForSaveConfirmationWasCalled);
        Assert.False(libraryService.SaveRecipeWasCalled);
    }

    [Fact]
    public async Task Run_WithInvalidMenuOption_ShouldDisplayInvalidOptionMessage()
    {
        FakeRecipeImporterService importService = new FakeRecipeImporterService();
        FakeRecipeBackupService backupService = new FakeRecipeBackupService();
        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader();
        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue("abc");
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        ClassicConsoleRecipeApp app = new ClassicConsoleRecipeApp(reader, writer, importService, new FakeRecipeLibraryService(), backupService);

        await app.RunAsync();
        Assert.True(writer.DisplayMessageWasCalled);
    }
}
