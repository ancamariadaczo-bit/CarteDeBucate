public class RecipeLibraryServiceTests
{
    [Fact]
    public void GetRecipeSummaries_WhenNoUserIsLoggedIn_ShouldReturnAllSummaries()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateValidRecipe(userId: 7),
                CreateValidRecipe(userId: 9)
            }
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        List<RecipeSummary> recipes = service.GetRecipeSummaries();

        Assert.Equal(2, recipes.Count);
        Assert.True(repository.GetAllRecipeSummariesWasCalled);
        Assert.False(repository.GetRecipeSummariesByUserIdWasCalled);
    }

    [Fact]
    public void GetRecipeSummaries_WhenUserIsLoggedIn_ShouldReturnCurrentUserSummaries()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateValidRecipe(userId: 7),
                CreateValidRecipe(userId: 9)
            }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        List<RecipeSummary> recipes = service.GetRecipeSummaries();

        Assert.Single(recipes);
        Assert.False(repository.GetAllRecipeSummariesWasCalled);
        Assert.True(repository.GetRecipeSummariesByUserIdWasCalled);
        Assert.Equal(7, repository.UserIdPassedToGetRecipeSummariesByUserId);
    }

    [Fact]
    public void GetRecipeSummariesPage_WhenNoUserIsLoggedIn_ShouldRequestGlobalPage()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateValidRecipe(userId: 7),
                CreateValidRecipe(userId: 9)
            }
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        PagedResult<RecipeSummary> result = service.GetRecipeSummariesPage(2, 1);

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalItems);
        Assert.True(repository.GetRecipeSummariesPageWasCalled);
        Assert.False(repository.GetRecipeSummariesPageByUserIdWasCalled);
        Assert.Equal(2, repository.PageNumberPassedToGetRecipeSummariesPage);
        Assert.Equal(1, repository.PageSizePassedToGetRecipeSummariesPage);
    }

    [Fact]
    public void GetRecipeSummariesPage_WhenUserIsLoggedIn_ShouldRequestCurrentUserPage()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateValidRecipe(userId: 7),
                CreateValidRecipe(userId: 9)
            }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(repository, currentUserContext);

        PagedResult<RecipeSummary> result = service.GetRecipeSummariesPage(1, 10);

        Assert.Single(result.Items);
        Assert.False(repository.GetRecipeSummariesPageWasCalled);
        Assert.True(repository.GetRecipeSummariesPageByUserIdWasCalled);
        Assert.Equal(7, repository.UserIdPassedToGetRecipeSummariesPageByUserId);
    }

    [Fact]
    public void HasRecipesInCurrentContext_WhenNoUserIsLoggedIn_ShouldCheckAllRecipes()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { CreateValidRecipe(userId: null) }
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        bool hasRecipes = service.HasRecipesInCurrentContext();

        Assert.True(hasRecipes);
        Assert.True(repository.HasRecipesWasCalled);
        Assert.False(repository.HasRecipesForUserWasCalled);
    }

    [Fact]
    public void HasRecipesInCurrentContext_WhenUserIsLoggedIn_ShouldCheckCurrentUserRecipes()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateValidRecipe(userId: 7),
                CreateValidRecipe(userId: 9)
            }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        bool hasRecipes = service.HasRecipesInCurrentContext();

        Assert.True(hasRecipes);
        Assert.False(repository.HasRecipesWasCalled);
        Assert.True(repository.HasRecipesForUserWasCalled);
        Assert.Equal(7, repository.UserIdPassedToHasRecipesForUser);
    }

    [Fact]
    public void GetRecipeById_WithInvalidId_ShouldReturnNullWithoutCallingRepository()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeLibraryService service = new RecipeLibraryService(repository);

        Recipe? recipe = service.GetRecipeById(0);

        Assert.Null(recipe);
        Assert.False(repository.GetRecipeByIdWasCalled);
        Assert.False(repository.GetRecipeByIdAndUserIdWasCalled);
    }

    [Fact]
    public void GetRecipeById_WhenNoUserIsLoggedIn_ShouldReturnRecipeById()
    {
        Recipe expectedRecipe = CreateValidRecipe(userId: null);
        expectedRecipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { expectedRecipe }
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        Recipe? recipe = service.GetRecipeById(expectedRecipe.Id);

        Assert.Equal(expectedRecipe, recipe);
        Assert.True(repository.GetRecipeByIdWasCalled);
        Assert.False(repository.GetRecipeByIdAndUserIdWasCalled);
    }

    [Fact]
    public void GetRecipeById_WhenUserIsLoggedIn_ShouldReturnCurrentUserRecipeById()
    {
        Recipe expectedRecipe = CreateValidRecipe(userId: 7);
        expectedRecipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                expectedRecipe,
                CreateValidRecipe(userId: 9)
            }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        Recipe? recipe = service.GetRecipeById(expectedRecipe.Id);

        Assert.Equal(expectedRecipe, recipe);
        Assert.False(repository.GetRecipeByIdWasCalled);
        Assert.True(repository.GetRecipeByIdAndUserIdWasCalled);
    }

    [Fact]
    public void GetRecipeById_WithRecipeFromAnotherUser_ShouldReturnNull()
    {
        Recipe otherUserRecipe = CreateValidRecipe(userId: 9);
        otherUserRecipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { otherUserRecipe }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        Recipe? recipe = service.GetRecipeById(otherUserRecipe.Id);

        Assert.Null(recipe);
        Assert.False(repository.GetRecipeByIdWasCalled);
        Assert.True(repository.GetRecipeByIdAndUserIdWasCalled);
    }

    [Fact]
    public void SearchRecipes_WithEmptyText_ShouldReturnEmptyList()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { CreateValidRecipe(userId: null) }
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        List<RecipeSummary> recipes = service.SearchRecipes("");

        Assert.Empty(recipes);
        Assert.False(repository.SearchRecipesWasCalled);
    }

    [Fact]
    public void SearchRecipes_WhenUserIsLoggedIn_ShouldPassCurrentUserToRepository()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateValidRecipe(userId: 7),
                CreateValidRecipe(userId: 9)
            }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        List<RecipeSummary> recipes = service.SearchRecipes("banana");

        Assert.Single(recipes);
        Assert.True(repository.SearchRecipesWasCalled);
        Assert.Equal(7, repository.UserIdPassedToSearchRecipes);
    }

    [Fact]
    public void SearchRecipes_ShouldPassSearchTextToRepository()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { CreateValidRecipe(userId: null) }
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        service.SearchRecipes("banana");

        Assert.True(repository.SearchRecipesWasCalled);
        Assert.Equal("banana", repository.SearchTextPassedToSearchRecipes);
    }

    [Fact]
    public void SearchRecipesPage_WhenUserIsLoggedIn_ShouldPassPagingAndCurrentUser()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateValidRecipe(userId: 7),
                CreateValidRecipe(userId: 9)
            }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(repository, currentUserContext);

        PagedResult<RecipeSummary> result = service.SearchRecipesPage("banana", 2, 5);

        Assert.True(repository.SearchRecipesPageWasCalled);
        Assert.Equal("banana", repository.SearchTextPassedToSearchRecipesPage);
        Assert.Equal(7, repository.UserIdPassedToSearchRecipesPage);
        Assert.Equal(2, repository.PageNumberPassedToSearchRecipesPage);
        Assert.Equal(5, repository.PageSizePassedToSearchRecipesPage);
    }

    [Fact]
    public void SearchRecipesPage_WithWhitespaceText_ShouldReturnEmptyPageWithoutCallingRepository()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeLibraryService service = new RecipeLibraryService(repository);

        PagedResult<RecipeSummary> result = service.SearchRecipesPage("   ", 2, 5);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.False(repository.SearchRecipesPageWasCalled);
    }

    [Fact]
    public void SaveRecipe_WithValidRecipe_ShouldSaveRecipe()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeLibraryService service = new RecipeLibraryService(repository);

        Recipe recipe = CreateValidRecipe(userId: null);

        RecipeSaveResult result = service.SaveRecipe(recipe);

        Assert.True(result.IsSuccess);
        Assert.True(repository.AddRecipeWasCalled);
        Assert.Equal(recipe, repository.AddedRecipe);
    }

    [Fact]
    public void SaveRecipe_WithInvalidRecipe_ShouldNotSaveRecipe()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeLibraryService service = new RecipeLibraryService(repository);

        Recipe recipe = CreateValidRecipe(userId: null);
        recipe.Name = "";

        RecipeSaveResult result = service.SaveRecipe(recipe);

        Assert.False(result.IsSuccess);
        Assert.False(repository.AddRecipeWasCalled);
    }

    [Fact]
    public void SaveRecipe_WithExistingSourceUrl_ShouldNotSaveRecipe()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            SourceUrlExists = true
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        Recipe recipe = CreateValidRecipe(userId: null);

        RecipeSaveResult result = service.SaveRecipe(recipe);

        Assert.False(result.IsSuccess);
        Assert.False(repository.AddRecipeWasCalled);
        Assert.Equal(AppTexts.RecipeAlreadyExists, result.Message);
    }

    [Fact]
    public void SaveRecipe_WhenUserIsLoggedIn_ShouldSetCurrentUserId()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        Recipe recipe = CreateValidRecipe(userId: null);

        RecipeSaveResult result = service.SaveRecipe(recipe);

        Assert.True(result.IsSuccess);
        Assert.True(repository.AddRecipeWasCalled);
        Assert.Equal(7, repository.AddedRecipe?.UserId);
    }

    [Fact]
    public void UpdateRecipe_WhenRecipeDoesNotExist_ShouldFailWithoutUpdating()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeLibraryService service = new RecipeLibraryService(repository);

        Recipe recipe = CreateValidRecipe(userId: null);
        recipe.Id = 12;

        RecipeSaveResult result = service.UpdateRecipe(recipe);

        Assert.False(result.IsSuccess);
        Assert.Equal(AppTexts.RecipeNotFound, result.Message);
        Assert.False(repository.UpdateRecipeWasCalled);
    }

    [Fact]
    public void UpdateRecipe_WithInvalidRecipe_ShouldFailWithoutUpdating()
    {
        Recipe recipe = CreateValidRecipe(userId: null);
        recipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { recipe }
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        Recipe invalidRecipe = CreateValidRecipe(userId: null);
        invalidRecipe.Id = recipe.Id;
        invalidRecipe.Name = "";

        RecipeSaveResult result = service.UpdateRecipe(invalidRecipe);

        Assert.False(result.IsSuccess);
        Assert.False(repository.UpdateRecipeWasCalled);
    }

    [Fact]
    public void UpdateRecipe_WhenNoUserIsLoggedIn_ShouldUpdateRecipe()
    {
        Recipe recipe = CreateValidRecipe(userId: null);
        recipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { recipe }
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        Recipe updatedRecipe = CreateValidRecipe(userId: null);
        updatedRecipe.Id = recipe.Id;
        updatedRecipe.Name = "Updated banana bread";

        RecipeSaveResult result = service.UpdateRecipe(updatedRecipe);

        Assert.True(result.IsSuccess);
        Assert.True(repository.GetRecipeByIdWasCalled);
        Assert.False(repository.GetRecipeByIdAndUserIdWasCalled);
        Assert.True(repository.UpdateRecipeWasCalled);
        Assert.Equal(updatedRecipe, repository.UpdatedRecipe);
    }

    [Fact]
    public void UpdateRecipe_WhenUserIsLoggedIn_ShouldUpdateRecipeForCurrentUser()
    {
        Recipe recipe = CreateValidRecipe(userId: 7);
        recipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { recipe }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        Recipe updatedRecipe = CreateValidRecipe(userId: null);
        updatedRecipe.Id = recipe.Id;
        updatedRecipe.Name = "Updated banana bread";

        RecipeSaveResult result = service.UpdateRecipe(updatedRecipe);

        Assert.True(result.IsSuccess);
        Assert.False(repository.GetRecipeByIdWasCalled);
        Assert.True(repository.GetRecipeByIdAndUserIdWasCalled);
        Assert.True(repository.UpdateRecipeWasCalled);
        Assert.Equal(7, repository.UpdatedRecipe?.UserId);
    }

    [Fact]
    public void UpdateRecipe_WithRecipeFromAnotherUser_ShouldFailWithoutUpdating()
    {
        Recipe otherUserRecipe = CreateValidRecipe(userId: 9);
        otherUserRecipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { otherUserRecipe }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        Recipe recipe = CreateValidRecipe(userId: 7);
        recipe.Id = otherUserRecipe.Id;

        RecipeSaveResult result = service.UpdateRecipe(recipe);

        Assert.False(result.IsSuccess);
        Assert.Equal(AppTexts.RecipeNotFound, result.Message);
        Assert.False(repository.UpdateRecipeWasCalled);
    }

    [Fact]
    public void DeleteRecipe_WithInvalidId_ShouldFailWithoutDeleting()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeLibraryService service = new RecipeLibraryService(repository);

        RecipeSaveResult result = service.DeleteRecipe(0);

        Assert.False(result.IsSuccess);
        Assert.Equal(AppTexts.InvalidRecipeId, result.Message);
        Assert.False(repository.DeleteRecipeWasCalled);
        Assert.False(repository.DeleteRecipeForUserWasCalled);
    }

    [Fact]
    public void DeleteRecipe_WhenRecipeDoesNotExist_ShouldFailWithoutDeleting()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeLibraryService service = new RecipeLibraryService(repository);

        RecipeSaveResult result = service.DeleteRecipe(12);

        Assert.False(result.IsSuccess);
        Assert.Equal(AppTexts.RecipeNotFound, result.Message);
        Assert.False(repository.DeleteRecipeWasCalled);
        Assert.False(repository.DeleteRecipeForUserWasCalled);
    }

    [Fact]
    public void DeleteRecipe_WhenNoUserIsLoggedIn_ShouldDeleteRecipe()
    {
        Recipe recipe = CreateValidRecipe(userId: null);
        recipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { recipe }
        };

        RecipeLibraryService service = new RecipeLibraryService(repository);

        RecipeSaveResult result = service.DeleteRecipe(recipe.Id);

        Assert.True(result.IsSuccess);
        Assert.True(repository.GetAllRecipeSummariesWasCalled);
        Assert.False(repository.GetRecipeByIdWasCalled);
        Assert.True(repository.DeleteRecipeWasCalled);
        Assert.False(repository.DeleteRecipeForUserWasCalled);
        Assert.Equal(recipe.Id, repository.DeletedRecipeId);
    }

    [Fact]
    public void DeleteRecipe_WhenUserIsLoggedIn_ShouldDeleteRecipeForCurrentUser()
    {
        Recipe recipe = CreateValidRecipe(userId: 7);
        recipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { recipe }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        RecipeSaveResult result = service.DeleteRecipe(recipe.Id);

        Assert.True(result.IsSuccess);
        Assert.True(repository.GetRecipeSummariesByUserIdWasCalled);
        Assert.False(repository.GetRecipeByIdAndUserIdWasCalled);
        Assert.True(repository.DeleteRecipeForUserWasCalled);
        Assert.Equal(recipe.Id, repository.DeletedRecipeId);
        Assert.Equal(7, repository.UserIdPassedToDeleteRecipeForUser);
    }

    [Fact]
    public void DeleteRecipe_WithRecipeFromAnotherUser_ShouldFailWithoutDeleting()
    {
        Recipe otherUserRecipe = CreateValidRecipe(userId: 9);
        otherUserRecipe.Id = 12;

        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe> { otherUserRecipe }
        };

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });

        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        RecipeSaveResult result = service.DeleteRecipe(otherUserRecipe.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(AppTexts.RecipeNotFound, result.Message);
        Assert.False(repository.DeleteRecipeForUserWasCalled);
        Assert.Single(repository.Recipes);
    }

    private static Recipe CreateValidRecipe(int? userId)
    {
        return new Recipe
        {
            Name = "Banana bread",
            SourceUrl = "https://example.com/banana-bread",
            SavedAt = new DateTime(2026, 1, 1),
            UserId = userId,
            Ingredients = new List<string>
            {
                "2 bananas",
                "2 eggs"
            },
            Steps = new List<string>
            {
                "Mix ingredients.",
                "Bake for 30 minutes."
            }
        };
    }
}
