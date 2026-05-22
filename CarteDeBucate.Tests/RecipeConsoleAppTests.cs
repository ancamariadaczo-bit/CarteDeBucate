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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importerService, backupService);

        await app.RunAsync();

        Assert.True(writer.ShowMenuWasCalled);
        Assert.True(reader.ReadMenuOptionWasCalled);
        Assert.False(importerService.GetAllRecipesWasCalled);
    }

    [Fact]
    public async Task Run_WithShowAllRecipesOption_ShouldDisplayAllRecipes()
    {
        List<Recipe> recipes = new()
    {
        new Recipe { Id = 1, Name = "Banana Bread" },
        new Recipe { Id = 2, Name = "Pancakes" }
    };

        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            RecipesToReturn = recipes
        };

        FakeRecipeBackupService backupService = new FakeRecipeBackupService();

        FakeRecipeConsoleReader reader = new FakeRecipeConsoleReader();
        FakeRecipeConsoleWriter writer = new FakeRecipeConsoleWriter();

        reader.MenuOptionsToReturn.Enqueue(MenuKeys.ShowRecipes);
        reader.MenuOptionsToReturn.Enqueue(MenuKeys.Exit);

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(importService.GetAllRecipesWasCalled);
        Assert.True(writer.DisplayRecipeListWasCalled);
        Assert.Equal(recipes, writer.RecipesPassedToDisplayRecipeList);
    }

    [Fact]
    public async Task Run_WithViewRecipeOption_ShouldDisplayRecipeDetails()
    {
        Recipe recipe = new Recipe { Id = 3, Name = "Keto Cake" };
        List<Recipe> recipes = [recipe];

        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            RecipesToReturn = recipes,
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeIdToViewWasCalled);
        Assert.True(importService.GetRecipeByIdWasCalled);
        Assert.Equal(3, importService.RecipeIdPassedToGetRecipeById);

        Assert.True(writer.DisplayRecipeDetailsWasCalled);
        Assert.Equal(recipe, writer.RecipePassedToDisplayRecipeDetails);
    }

    [Fact]
    public async Task Run_WithViewRecipeOptionAndMissingRecipe_ShouldDisplayMessage()
    {
        List<Recipe> recipes = new()
    {
        new Recipe { Id = 1, Name = "Banana Bread" },
        new Recipe { Id = 2, Name = "Pancakes" }
    };

        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            RecipesToReturn = recipes,
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(importService.GetRecipeByIdWasCalled);
        Assert.True(writer.DisplayMessageWasCalled);
        Assert.False(writer.DisplayRecipeDetailsWasCalled);
    }

    [Fact]
    public async Task Run_WithSearchOption_ShouldDisplaySearchResults()
    {
        List<Recipe> recipes = new()
    {
        new Recipe { Id = 1, Name = "Banana Bread" },
        new Recipe { Id = 2, Name = "Keto Pancakes" }
    };

        List<Recipe> searchResults = new()
    {
        new Recipe { Id = 2, Name = "Keto Pancakes" }
    };

        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            RecipesToReturn = recipes,
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadSearchTextWasCalled);
        Assert.True(importService.SearchRecipesWasCalled);
        Assert.Equal("keto", importService.SearchTextPassedToSearchRecipes);

        Assert.True(writer.DisplaySearchResultsWasCalled);
        Assert.Equal(searchResults, writer.RecipesPassedToDisplaySearchResults);
    }

    [Fact]
    public async Task Run_WithDeleteRecipeOption_ShouldDeleteRecipe()
    {
        List<Recipe> recipes = new()
        {
            new Recipe { Id = 1, Name = "Banana Bread" },
            new Recipe { Id = 2, Name = "Keto Pancakes" }
        };

        RecipeSaveResult deleteResult = new RecipeSaveResult
        {
            IsSuccess = true,
            Message = "Recipe deleted."
        };

        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            RecipesToReturn = recipes,
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeIdToDeleteWasCalled);
        Assert.True(importService.DeleteRecipeWasCalled);
        Assert.Equal(5, importService.RecipeIdPassedToDeleteRecipe);

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

        FakeRecipeImporterService importService = new FakeRecipeImporterService
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeFromConsoleWasCalled);
        Assert.True(importService.SaveRecipeWasCalled);
        Assert.Equal(recipe, importService.RecipePassedToSaveRecipe);

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

        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            RecipesToReturn = [existingRecipe],
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeIdToEditWasCalled);
        Assert.True(importService.GetRecipeByIdWasCalled);
        Assert.Equal(4, importService.RecipeIdPassedToGetRecipeById);

        Assert.True(reader.ReadRecipeEditsFromConsoleWasCalled);
        Assert.Equal(existingRecipe, reader.RecipePassedToReadEdits);

        Assert.True(importService.UpdateRecipeWasCalled);
        Assert.Equal(editedRecipe, importService.RecipePassedToUpdateRecipe);

        Assert.True(writer.DisplayMessageWasCalled);
        Assert.Contains(updateResult.Message, writer.DisplayedMessages);
    }

    [Fact]
    public async Task Run_WithEditRecipeOptionAndMissingRecipe_ShouldNotUpdateRecipe()
    {
        List<Recipe> recipes = new()
        {
            new Recipe { Id = 1, Name = "Banana Bread" },
            new Recipe { Id = 2, Name = "Keto Pancakes" }
        };

        FakeRecipeImporterService importService = new FakeRecipeImporterService
        {
            RecipesToReturn = recipes,
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(importService.GetRecipeByIdWasCalled);
        Assert.False(reader.ReadRecipeEditsFromConsoleWasCalled);
        Assert.False(importService.UpdateRecipeWasCalled);

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
            RecipesToReturn = [],
            ImportResultToReturn = importResult,
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(reader.ReadRecipeUrlToImportWasCalled);
        Assert.True(importService.ImportRecipeFromUrlAsyncWasCalled);
        Assert.Equal("https://example.com/recipe", importService.UrlPassedToImportRecipeFromUrlAsync);

        Assert.True(writer.DisplayImportedRecipeWasCalled);
        Assert.Equal(importedRecipe, writer.RecipePassedToDisplayImportedRecipe);

        Assert.True(reader.CompleteImportedRecipeFromConsoleWasCalled);
        Assert.Equal(importedRecipe, reader.RecipePassedToCompleteImportedRecipe);

        Assert.True(reader.AskForSaveConfirmationWasCalled);

        Assert.True(importService.SaveRecipeWasCalled);
        Assert.Equal(importedRecipe, importService.RecipePassedToSaveRecipe);

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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(importService.ImportRecipeFromUrlAsyncWasCalled);
        Assert.True(writer.DisplayImportedRecipeWasCalled);
        Assert.True(reader.AskForSaveConfirmationWasCalled);

        Assert.False(importService.SaveRecipeWasCalled);
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();

        Assert.True(importService.ImportRecipeFromUrlAsyncWasCalled);

        Assert.True(writer.DisplayMessageWasCalled);
        Assert.Contains("Pagina nu pare să fie o rețetă.", writer.DisplayedMessages);

        Assert.False(writer.DisplayImportedRecipeWasCalled);
        Assert.False(reader.CompleteImportedRecipeFromConsoleWasCalled);
        Assert.False(reader.AskForSaveConfirmationWasCalled);
        Assert.False(importService.SaveRecipeWasCalled);
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

        RecipeConsoleApp app = new RecipeConsoleApp(reader, writer, importService, backupService);

        await app.RunAsync();
        Assert.True(writer.DisplayMessageWasCalled);
    }
}