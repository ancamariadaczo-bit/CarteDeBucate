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

    private static DatabaseRecipeRepository CreateRepository(string databasePath)
    {
        DatabaseInitializer databaseInitializer = new DatabaseInitializer(databasePath);
        databaseInitializer.Initialize();

        return new DatabaseRecipeRepository(databasePath);
    }

    private static Recipe CreateTestRecipe()
    {
        return new Recipe
        {
            Name = "Test recipe",
            SourceUrl = "https://example.com/test-recipe",
            SavedAt = new DateTime(2026, 5, 19, 10, 0, 0),
            Notes = "Test notes",
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