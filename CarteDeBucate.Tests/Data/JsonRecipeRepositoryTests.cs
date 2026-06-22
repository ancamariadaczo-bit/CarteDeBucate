public class JsonRecipeRepositoryTests
{
    [Fact]
    public void GetAllRecipes_WhenFileDoesNotExist_ShouldReturnEmptyList()
    {
        string filePath = CreateTemporaryFilePath();

        JsonRecipeRepository repository = new JsonRecipeRepository(filePath);

        List<Recipe> recipes = repository.GetAllRecipes();

        Assert.Empty(recipes);
    }

    [Fact]
    public void AddRecipe_ShouldAssignIdAndPersistRecipe()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            Recipe recipe = CreateRecipe();

            repository.AddRecipe(recipe);

            List<Recipe> recipes = repository.GetAllRecipes();

            Assert.Single(recipes);
            Assert.Equal(1, recipe.Id);
            Assert.Equal(recipe.Name, recipes[0].Name);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void UpdateRecipe_WhenRecipeExists_ShouldPersistChanges()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            Recipe recipe = CreateRecipe();
            repository.AddRecipe(recipe);

            recipe.Name = "Updated";
            repository.UpdateRecipe(recipe);

            Recipe? updatedRecipe = repository.GetRecipeById(recipe.Id);

            Assert.NotNull(updatedRecipe);
            Assert.Equal("Updated", updatedRecipe.Name);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void UpdateRecipe_WhenRecipeDoesNotExist_ShouldNotChangeFile()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            Recipe recipe = CreateRecipe();
            repository.AddRecipe(recipe);

            repository.UpdateRecipe(new Recipe { Id = 99, Name = "Missing" });

            List<Recipe> recipes = repository.GetAllRecipes();

            Assert.Single(recipes);
            Assert.Equal(recipe.Name, recipes[0].Name);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void DeleteRecipe_WhenRecipeExists_ShouldRemoveRecipe()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            Recipe recipe = CreateRecipe();
            repository.AddRecipe(recipe);

            repository.DeleteRecipe(recipe.Id);

            Assert.Empty(repository.GetAllRecipes());
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void RecipeExistsBySourceUrl_ShouldMatchIgnoringCaseAndSpaces()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            repository.AddRecipe(CreateRecipe());

            Assert.True(repository.RecipeExistsBySourceUrl(" HTTPS://EXAMPLE.COM/RECIPE "));
            Assert.False(repository.RecipeExistsBySourceUrl(""));
            Assert.False(repository.RecipeExistsBySourceUrl("https://example.com/missing"));
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void GetRecipesByUserId_ShouldReturnOnlyRecipesForUser()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            Recipe firstUserRecipe = CreateRecipe(userId: 1);
            Recipe secondUserRecipe = CreateRecipe(
                sourceUrl: "https://example.com/other-recipe",
                userId: 2);

            repository.AddRecipe(firstUserRecipe);
            repository.AddRecipe(secondUserRecipe);

            List<Recipe> recipes = repository.GetRecipesByUserId(1);

            Assert.Single(recipes);
            Assert.Equal(firstUserRecipe.Id, recipes[0].Id);
            Assert.Equal(1, recipes[0].UserId);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void HasRecipes_ShouldReturnWhetherAnyRecipeExists()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);

            Assert.False(repository.HasRecipes());

            repository.AddRecipe(CreateRecipe());

            Assert.True(repository.HasRecipes());
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void HasRecipesForUser_ShouldReturnWhetherUserHasRecipes()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            repository.AddRecipe(CreateRecipe(userId: 2));

            Assert.True(repository.HasRecipesForUser(2));
            Assert.False(repository.HasRecipesForUser(3));
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void GetRecipeByIdAndUserId_ShouldReturnRecipeOnlyForOwner()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            Recipe recipe = CreateRecipe(userId: 3);
            repository.AddRecipe(recipe);

            Recipe? recipeForOwner = repository.GetRecipeByIdAndUserId(recipe.Id, 3);
            Recipe? recipeForOtherUser = repository.GetRecipeByIdAndUserId(recipe.Id, 4);

            Assert.NotNull(recipeForOwner);
            Assert.Equal(recipe.Id, recipeForOwner.Id);
            Assert.Equal(3, recipeForOwner.UserId);
            Assert.Null(recipeForOtherUser);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void RecipeExistsBySourceUrlForUser_ShouldMatchOnlyForUser()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            repository.AddRecipe(CreateRecipe(userId: 5));

            Assert.True(repository.RecipeExistsBySourceUrlForUser(" HTTPS://EXAMPLE.COM/RECIPE ", 5));
            Assert.False(repository.RecipeExistsBySourceUrlForUser("https://example.com/recipe", 6));
            Assert.False(repository.RecipeExistsBySourceUrlForUser("", 5));
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void UpdateRecipeForUser_WhenRecipeBelongsToUser_ShouldPersistChanges()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            Recipe recipe = CreateRecipe(userId: 7);
            repository.AddRecipe(recipe);

            recipe.Name = "Updated for user";
            recipe.SourceUrl = "https://example.com/updated";
            repository.UpdateRecipeForUser(recipe, 7);

            Recipe? updatedRecipe = repository.GetRecipeByIdAndUserId(recipe.Id, 7);

            Assert.NotNull(updatedRecipe);
            Assert.Equal("Updated for user", updatedRecipe.Name);
            Assert.Equal("https://example.com/updated", updatedRecipe.SourceUrl);
            Assert.Equal(7, updatedRecipe.UserId);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void UpdateRecipeForUser_WhenRecipeBelongsToAnotherUser_ShouldNotChangeRecipe()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            Recipe recipe = CreateRecipe(userId: 8);
            repository.AddRecipe(recipe);

            Recipe changedRecipe = CreateRecipe(
                name: "Should not be saved",
                sourceUrl: "https://example.com/changed",
                userId: 8);
            changedRecipe.Id = recipe.Id;

            repository.UpdateRecipeForUser(changedRecipe, 9);

            Recipe? unchangedRecipe = repository.GetRecipeByIdAndUserId(recipe.Id, 8);

            Assert.NotNull(unchangedRecipe);
            Assert.Equal("Recipe", unchangedRecipe.Name);
            Assert.Equal("https://example.com/recipe", unchangedRecipe.SourceUrl);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void DeleteRecipeForUser_ShouldDeleteOnlyRecipeOwnedByUser()
    {
        string filePath = CreateTemporaryFilePath();

        try
        {
            JsonRecipeRepository repository = new JsonRecipeRepository(filePath);
            Recipe firstUserRecipe = CreateRecipe(userId: 10);
            Recipe secondUserRecipe = CreateRecipe(
                sourceUrl: "https://example.com/second-user-recipe",
                userId: 11);

            repository.AddRecipe(firstUserRecipe);
            repository.AddRecipe(secondUserRecipe);

            repository.DeleteRecipeForUser(firstUserRecipe.Id, 10);
            repository.DeleteRecipeForUser(secondUserRecipe.Id, 10);

            Assert.Null(repository.GetRecipeByIdAndUserId(firstUserRecipe.Id, 10));
            Assert.NotNull(repository.GetRecipeByIdAndUserId(secondUserRecipe.Id, 11));
            Assert.Single(repository.GetAllRecipes());
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    private static Recipe CreateRecipe(
        string name = "Recipe",
        string sourceUrl = "https://example.com/recipe",
        int? userId = null)
    {
        return new Recipe
        {
            Name = name,
            SourceUrl = sourceUrl,
            SavedAt = new DateTime(2026, 6, 8, 10, 0, 0),
            UserId = userId,
            Ingredients = ["Flour"],
            Steps = ["Mix"]
        };
    }

    private static string CreateTemporaryFilePath()
    {
        return Path.Combine(Path.GetTempPath(), $"recipes-{Guid.NewGuid()}.json");
    }

    private static void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
