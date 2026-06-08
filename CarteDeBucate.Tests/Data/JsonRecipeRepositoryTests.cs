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

    private static Recipe CreateRecipe()
    {
        return new Recipe
        {
            Name = "Recipe",
            SourceUrl = "https://example.com/recipe",
            SavedAt = new DateTime(2026, 6, 8, 10, 0, 0),
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
