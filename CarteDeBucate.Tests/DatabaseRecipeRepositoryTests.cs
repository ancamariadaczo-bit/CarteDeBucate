using Microsoft.Data.Sqlite;

public class DatabaseRecipeRepositoryTests
{
    [Fact]
    public void GetAllRecipes_WhenDatabaseIsEmpty_ShouldReturnEmptyList()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            List<Recipe> recipes = repository.GetAllRecipes();

            Assert.Empty(recipes);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void AddRecipe_ShouldSaveRecipeWithIngredientsAndSteps()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe();

            repository.AddRecipe(recipe);

            List<Recipe> recipes = repository.GetAllRecipes();

            Assert.Single(recipes);

            Recipe savedRecipe = recipes[0];

            Assert.True(savedRecipe.Id > 0);
            Assert.Equal("Test recipe", savedRecipe.Name);
            Assert.Equal("https://example.com/test-recipe", savedRecipe.SourceUrl);
            Assert.Equal("Test notes", savedRecipe.Notes);
            Assert.Null(savedRecipe.UserId);

            Assert.Equal(2, savedRecipe.Ingredients.Count);
            Assert.Equal("200 g flour", savedRecipe.Ingredients[0]);
            Assert.Equal("2 eggs", savedRecipe.Ingredients[1]);

            Assert.Equal(2, savedRecipe.Steps.Count);
            Assert.Equal("Mix ingredients.", savedRecipe.Steps[0]);
            Assert.Equal("Bake for 30 minutes.", savedRecipe.Steps[1]);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void GetRecipeById_WhenRecipeExists_ShouldReturnRecipeWithIngredientsAndSteps()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe();

            repository.AddRecipe(recipe);

            Recipe? savedRecipe = repository.GetRecipeById(recipe.Id);

            Assert.NotNull(savedRecipe);
            Assert.Equal(recipe.Id, savedRecipe.Id);
            Assert.Equal("Test recipe", savedRecipe.Name);
            Assert.Equal("https://example.com/test-recipe", savedRecipe.SourceUrl);
            Assert.Equal("Test notes", savedRecipe.Notes);
            Assert.Null(savedRecipe.UserId);

            Assert.Equal(recipe.Ingredients, savedRecipe.Ingredients);
            Assert.Equal(recipe.Steps, savedRecipe.Steps);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void GetRecipeById_WhenRecipeDoesNotExist_ShouldReturnNull()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe? recipe = repository.GetRecipeById(999);

            Assert.Null(recipe);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void GetRecipesByUserId_ShouldReturnOnlyRecipesForUser()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe firstUserRecipe = CreateTestRecipe(userId: 1);
            Recipe secondUserRecipe = CreateTestRecipe(
                sourceUrl: "https://example.com/other-user-recipe",
                userId: 2);

            AddUser(databasePath, 1);
            AddUser(databasePath, 2);
            repository.AddRecipe(firstUserRecipe);
            repository.AddRecipe(secondUserRecipe);

            List<Recipe> recipes = repository.GetRecipesByUserId(1);

            Assert.Single(recipes);
            Assert.Equal(firstUserRecipe.Id, recipes[0].Id);
            Assert.Equal(1, recipes[0].UserId);
            Assert.Equal(firstUserRecipe.Ingredients, recipes[0].Ingredients);
            Assert.Equal(firstUserRecipe.Steps, recipes[0].Steps);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void GetAllRecipeSummaries_ShouldReturnSummariesWithoutFullRecipeData()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe();

            repository.AddRecipe(recipe);

            List<RecipeSummary> recipes = repository.GetAllRecipeSummaries();

            Assert.Single(recipes);

            RecipeSummary summary = recipes[0];

            Assert.Equal(recipe.Id, summary.Id);
            Assert.Equal(recipe.Name, summary.Name);
            Assert.Equal(recipe.SourceUrl, summary.SourceUrl);
            Assert.Equal(recipe.SavedAt, summary.SavedAt);
            Assert.Equal(recipe.Status, summary.Status);
            Assert.Equal(recipe.UserId, summary.UserId);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void GetRecipeSummariesByUserId_ShouldReturnOnlyCurrentUserSummaries()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe firstUserRecipe = CreateTestRecipe(userId: 1);
            Recipe secondUserRecipe = CreateTestRecipe(
                sourceUrl: "https://example.com/other-user-recipe",
                userId: 2);

            AddUser(databasePath, 1);
            AddUser(databasePath, 2);
            repository.AddRecipe(firstUserRecipe);
            repository.AddRecipe(secondUserRecipe);

            List<RecipeSummary> recipes = repository.GetRecipeSummariesByUserId(1);

            Assert.Single(recipes);
            Assert.Equal(firstUserRecipe.Id, recipes[0].Id);
            Assert.Equal(1, recipes[0].UserId);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void SearchRecipes_ShouldSearchInNotesIngredientsAndSteps()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe notesRecipe = CreateTestRecipe(
                name: "Chocolate cake",
                sourceUrl: "https://example.com/chocolate-cake");
            notesRecipe.Notes = "Serve with raspberries.";

            Recipe ingredientRecipe = CreateTestRecipe(
                name: "Breakfast bowl",
                sourceUrl: "https://example.com/breakfast-bowl");
            ingredientRecipe.Ingredients = new List<string> { "Greek yogurt", "Honey" };

            Recipe stepRecipe = CreateTestRecipe(
                name: "Simple salad",
                sourceUrl: "https://example.com/simple-salad");
            stepRecipe.Steps = new List<string> { "Toast the walnuts.", "Mix everything." };

            repository.AddRecipe(notesRecipe);
            repository.AddRecipe(ingredientRecipe);
            repository.AddRecipe(stepRecipe);

            List<RecipeSummary> noteResults = repository.SearchRecipes("raspberries", null);
            List<RecipeSummary> ingredientResults = repository.SearchRecipes("yogurt", null);
            List<RecipeSummary> stepResults = repository.SearchRecipes("walnuts", null);

            Assert.Single(noteResults);
            Assert.Equal(notesRecipe.Id, noteResults[0].Id);

            Assert.Single(ingredientResults);
            Assert.Equal(ingredientRecipe.Id, ingredientResults[0].Id);

            Assert.Single(stepResults);
            Assert.Equal(stepRecipe.Id, stepResults[0].Id);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void SearchRecipes_WhenUserIdIsProvided_ShouldReturnOnlyCurrentUserSummaries()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe firstUserRecipe = CreateTestRecipe(
                name: "Banana bread",
                sourceUrl: "https://example.com/banana-bread",
                userId: 1);
            Recipe secondUserRecipe = CreateTestRecipe(
                name: "Banana pancakes",
                sourceUrl: "https://example.com/banana-pancakes",
                userId: 2);

            AddUser(databasePath, 1);
            AddUser(databasePath, 2);
            repository.AddRecipe(firstUserRecipe);
            repository.AddRecipe(secondUserRecipe);

            List<RecipeSummary> recipes = repository.SearchRecipes("banana", 1);

            Assert.Single(recipes);
            Assert.Equal(firstUserRecipe.Id, recipes[0].Id);
            Assert.Equal(1, recipes[0].UserId);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void HasRecipes_ShouldReturnWhetherAnyRecipeExists()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Assert.False(repository.HasRecipes());

            repository.AddRecipe(CreateTestRecipe());

            Assert.True(repository.HasRecipes());
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void HasRecipesForUser_ShouldReturnWhetherUserHasRecipes()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            AddUser(databasePath, 1);
            AddUser(databasePath, 2);
            repository.AddRecipe(CreateTestRecipe(userId: 1));

            Assert.True(repository.HasRecipesForUser(1));
            Assert.False(repository.HasRecipesForUser(2));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void GetRecipeByIdAndUserId_WhenRecipeBelongsToUser_ShouldReturnRecipe()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe(userId: 7);
            AddUser(databasePath, 7);
            repository.AddRecipe(recipe);

            Recipe? savedRecipe = repository.GetRecipeByIdAndUserId(recipe.Id, 7);

            Assert.NotNull(savedRecipe);
            Assert.Equal(recipe.Id, savedRecipe.Id);
            Assert.Equal(7, savedRecipe.UserId);
            Assert.Equal(recipe.Ingredients, savedRecipe.Ingredients);
            Assert.Equal(recipe.Steps, savedRecipe.Steps);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void GetRecipeByIdAndUserId_WhenRecipeBelongsToAnotherUser_ShouldReturnNull()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe(userId: 7);
            AddUser(databasePath, 7);
            repository.AddRecipe(recipe);

            Recipe? savedRecipe = repository.GetRecipeByIdAndUserId(recipe.Id, 8);

            Assert.Null(savedRecipe);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void RecipeExistsBySourceUrl_WhenUrlExists_ShouldReturnTrue()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe();

            repository.AddRecipe(recipe);

            bool exists = repository.RecipeExistsBySourceUrl("https://example.com/test-recipe");

            Assert.True(exists);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void RecipeExistsBySourceUrl_ShouldIgnoreCaseAndExtraSpaces()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe();

            repository.AddRecipe(recipe);

            bool exists = repository.RecipeExistsBySourceUrl("  HTTPS://EXAMPLE.COM/TEST-RECIPE  ");

            Assert.True(exists);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void RecipeExistsBySourceUrl_WhenUrlDoesNotExist_ShouldReturnFalse()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            bool exists = repository.RecipeExistsBySourceUrl("https://example.com/missing-recipe");

            Assert.False(exists);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void RecipeExistsBySourceUrlForUser_ShouldMatchOnlyForUser()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            AddUser(databasePath, 1);
            repository.AddRecipe(CreateTestRecipe(userId: 1));

            Assert.True(repository.RecipeExistsBySourceUrlForUser(" HTTPS://EXAMPLE.COM/TEST-RECIPE ", 1));
            Assert.False(repository.RecipeExistsBySourceUrlForUser("https://example.com/test-recipe", 2));
            Assert.False(repository.RecipeExistsBySourceUrlForUser("", 1));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void UpdateRecipe_ShouldUpdateRecipeMainFieldsIngredientsAndSteps()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe();

            repository.AddRecipe(recipe);

            recipe.Name = "Updated recipe";
            recipe.SourceUrl = "https://example.com/updated-recipe";
            recipe.Notes = "Updated notes";
            recipe.SavedAt = new DateTime(2026, 5, 19, 10, 30, 0);

            recipe.Ingredients = new List<string>
            {
                "300 g almond flour",
                "3 eggs",
                "1 pinch of salt"
            };

            recipe.Steps = new List<string>
            {
                "Mix everything.",
                "Bake until golden."
            };

            repository.UpdateRecipe(recipe);

            Recipe? updatedRecipe = repository.GetRecipeById(recipe.Id);

            Assert.NotNull(updatedRecipe);

            Assert.Equal("Updated recipe", updatedRecipe.Name);
            Assert.Equal("https://example.com/updated-recipe", updatedRecipe.SourceUrl);
            Assert.Equal("Updated notes", updatedRecipe.Notes);

            Assert.Equal(3, updatedRecipe.Ingredients.Count);
            Assert.Equal("300 g almond flour", updatedRecipe.Ingredients[0]);
            Assert.Equal("3 eggs", updatedRecipe.Ingredients[1]);
            Assert.Equal("1 pinch of salt", updatedRecipe.Ingredients[2]);

            Assert.Equal(2, updatedRecipe.Steps.Count);
            Assert.Equal("Mix everything.", updatedRecipe.Steps[0]);
            Assert.Equal("Bake until golden.", updatedRecipe.Steps[1]);
            Assert.Null(updatedRecipe.UserId);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void UpdateRecipeForUser_WhenRecipeBelongsToUser_ShouldUpdateRecipe()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe(userId: 3);
            AddUser(databasePath, 3);
            repository.AddRecipe(recipe);

            recipe.Name = "Updated user recipe";
            recipe.SourceUrl = "https://example.com/updated-user-recipe";
            recipe.Notes = "Updated user notes";
            recipe.Ingredients = new List<string> { "1 cup milk" };
            recipe.Steps = new List<string> { "Stir." };

            repository.UpdateRecipeForUser(recipe, 3);

            Recipe? updatedRecipe = repository.GetRecipeByIdAndUserId(recipe.Id, 3);

            Assert.NotNull(updatedRecipe);
            Assert.Equal("Updated user recipe", updatedRecipe.Name);
            Assert.Equal("https://example.com/updated-user-recipe", updatedRecipe.SourceUrl);
            Assert.Equal("Updated user notes", updatedRecipe.Notes);
            Assert.Equal(3, updatedRecipe.UserId);
            Assert.Equal(recipe.Ingredients, updatedRecipe.Ingredients);
            Assert.Equal(recipe.Steps, updatedRecipe.Steps);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void UpdateRecipeForUser_WhenRecipeBelongsToAnotherUser_ShouldNotUpdateRecipe()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe(userId: 3);
            AddUser(databasePath, 3);
            repository.AddRecipe(recipe);

            Recipe changedRecipe = CreateTestRecipe(
                name: "Should not be saved",
                sourceUrl: "https://example.com/changed",
                userId: 3);
            changedRecipe.Id = recipe.Id;

            repository.UpdateRecipeForUser(changedRecipe, 4);

            Recipe? unchangedRecipe = repository.GetRecipeByIdAndUserId(recipe.Id, 3);

            Assert.NotNull(unchangedRecipe);
            Assert.Equal("Test recipe", unchangedRecipe.Name);
            Assert.Equal("https://example.com/test-recipe", unchangedRecipe.SourceUrl);
            Assert.Equal(recipe.Ingredients, unchangedRecipe.Ingredients);
            Assert.Equal(recipe.Steps, unchangedRecipe.Steps);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void DeleteRecipe_ShouldDeleteRecipeIngredientsAndSteps()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe();

            repository.AddRecipe(recipe);

            repository.DeleteRecipe(recipe.Id);

            Recipe? deletedRecipe = repository.GetRecipeById(recipe.Id);
            List<Recipe> recipes = repository.GetAllRecipes();

            Assert.Null(deletedRecipe);
            Assert.Empty(recipes);
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void DeleteRecipeForUser_WhenRecipeBelongsToUser_ShouldDeleteRecipe()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe(userId: 5);
            AddUser(databasePath, 5);
            repository.AddRecipe(recipe);

            repository.DeleteRecipeForUser(recipe.Id, 5);

            Assert.Null(repository.GetRecipeById(recipe.Id));
            Assert.Empty(repository.GetRecipesByUserId(5));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    [Fact]
    public void DeleteRecipeForUser_WhenRecipeBelongsToAnotherUser_ShouldNotDeleteRecipe()
    {
        string databasePath = CreateTemporaryDatabasePath();

        try
        {
            DatabaseRecipeRepository repository = CreateRepository(databasePath);

            Recipe recipe = CreateTestRecipe(userId: 5);
            AddUser(databasePath, 5);
            repository.AddRecipe(recipe);

            repository.DeleteRecipeForUser(recipe.Id, 6);

            Assert.NotNull(repository.GetRecipeById(recipe.Id));
            Assert.Single(repository.GetRecipesByUserId(5));
        }
        finally
        {
            DeleteDatabaseFile(databasePath);
        }
    }

    private static DatabaseRecipeRepository CreateRepository(string databasePath)
    {
        DatabaseInitializer databaseInitializer = new DatabaseInitializer(databasePath);
        databaseInitializer.Initialize();

        return new DatabaseRecipeRepository(databasePath);
    }

    private static void AddUser(string databasePath, int userId)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Id, Username, PasswordHash, PasswordSalt, CreatedAt)
            VALUES (@Id, @Username, @PasswordHash, @PasswordSalt, @CreatedAt);
            """;

        command.Parameters.AddWithValue("@Id", userId);
        command.Parameters.AddWithValue("@Username", $"user-{userId}");
        command.Parameters.AddWithValue("@PasswordHash", "hash");
        command.Parameters.AddWithValue("@PasswordSalt", "salt");
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));

        command.ExecuteNonQuery();
    }

    private static Recipe CreateTestRecipe(
        string name = "Test recipe",
        string sourceUrl = "https://example.com/test-recipe",
        int? userId = null)
    {
        return new Recipe
        {
            Name = name,
            SourceUrl = sourceUrl,
            SavedAt = new DateTime(2026, 5, 19, 10, 0, 0),
            Notes = "Test notes",
            UserId = userId,
            Ingredients = new List<string>
            {
                "200 g flour",
                "2 eggs"
            },
            Steps = new List<string>
            {
                "Mix ingredients.",
                "Bake for 30 minutes."
            }
        };
    }

    private static string CreateTemporaryDatabasePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.db");
    }

    private static void DeleteDatabaseFile(string databasePath)
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
