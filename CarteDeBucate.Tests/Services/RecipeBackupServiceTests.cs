using System.Text.Json;

public class RecipeBackupServiceTests
{
    [Fact]
    public void ExportBackup_ShouldReturnAllRecipesAsJson()
    {
        string tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"recipes-backup-{Guid.NewGuid()}.json");

        try
        {
            FakeRecipeRepository repository = new FakeRecipeRepository();

            Recipe recipe = CreateValidRecipe();
            repository.Recipes.Add(recipe);

            RecipeBackupService service = new RecipeBackupService(repository);
            RecipeBackupResult result = service.ExportToJson(tempFilePath);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.ExportedCount);
            Assert.True(File.Exists(tempFilePath));

            string json = File.ReadAllText(tempFilePath);

            Assert.Contains(recipe.Name, json);
            Assert.Contains(recipe.SourceUrl, json);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public void ExportToJsonContent_ShouldReturnValidJsonAndExportedCount()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();

        Recipe recipe = CreateValidRecipe();
        repository.Recipes.Add(recipe);

        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupExportResult result = service.ExportToJsonContent();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ExportedCount);

        List<Recipe>? exportedRecipes = JsonSerializer.Deserialize<List<Recipe>>(result.Json);

        Assert.NotNull(exportedRecipes);
        Recipe exportedRecipe = Assert.Single(exportedRecipes);
        Assert.Equal(recipe.Name, exportedRecipe.Name);
        Assert.Equal(recipe.SourceUrl, exportedRecipe.SourceUrl);
    }

    [Fact]
    public void ExportToJsonContent_WhenCurrentUserExists_ShouldExportOnlyCurrentUserRecipes()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        repository.Recipes.Add(CreateValidRecipe(1, "https://example.com/current-user"));
        repository.Recipes.Add(CreateValidRecipe(2, "https://example.com/other-user"));

        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User
        {
            Id = 1,
            Username = "ana"
        });

        RecipeBackupService service = new RecipeBackupService(repository, currentUserContext);

        RecipeBackupExportResult result = service.ExportToJsonContent();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ExportedCount);

        List<Recipe>? exportedRecipes = JsonSerializer.Deserialize<List<Recipe>>(result.Json);

        Assert.NotNull(exportedRecipes);
        Recipe exportedRecipe = Assert.Single(exportedRecipes);
        Assert.Equal(1, exportedRecipe.UserId);
        Assert.Equal("https://example.com/current-user", exportedRecipe.SourceUrl);
    }

    [Fact]
    public void ImportBackup_WithNewRecipe_ShouldSaveRecipe()
    {
        string tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"recipes-backup-{Guid.NewGuid()}.json");

        try
        {
            Recipe recipe = CreateValidRecipe();

            string json = JsonSerializer.Serialize(new List<Recipe> { recipe });

            File.WriteAllText(tempFilePath, json);

            FakeRecipeRepository repository = new FakeRecipeRepository();

            RecipeBackupService service = new RecipeBackupService(repository);

            RecipeBackupResult result = service.ImportFromJson(tempFilePath);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.ImportedCount);
            Assert.Equal(0, result.SkippedCount);

            Assert.Single(repository.Recipes);
            Assert.Equal(recipe.Name, repository.Recipes[0].Name);
            Assert.Equal(recipe.SourceUrl, repository.Recipes[0].SourceUrl);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public void ImportFromJsonContent_WithNewRecipe_ShouldSaveRecipe()
    {
        Recipe recipe = CreateValidRecipe();
        recipe.Id = 42;
        string json = JsonSerializer.Serialize(new List<Recipe> { recipe });

        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ImportFromJsonContent(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);

        Recipe savedRecipe = Assert.Single(repository.Recipes);
        Assert.Equal(0, savedRecipe.Id);
        Assert.Equal(recipe.Name, savedRecipe.Name);
        Assert.Equal(recipe.SourceUrl, savedRecipe.SourceUrl);
    }

    [Fact]
    public void ImportFromJsonContent_WithExistingRecipe_ShouldSkipRecipe()
    {
        Recipe existingRecipe = CreateValidRecipe();
        string json = JsonSerializer.Serialize(new List<Recipe> { existingRecipe });

        FakeRecipeRepository repository = new FakeRecipeRepository();
        repository.Recipes.Add(existingRecipe);
        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ImportFromJsonContent(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);

        Recipe savedRecipe = Assert.Single(repository.Recipes);
        Assert.Equal(existingRecipe.Name, savedRecipe.Name);
        Assert.Equal(existingRecipe.SourceUrl, savedRecipe.SourceUrl);
    }

    [Fact]
    public void ImportFromJsonContent_WithInvalidJson_ShouldFail()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ImportFromJsonContent("not valid json");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(repository.Recipes);
    }

    [Fact]
    public void ImportFromJsonContent_WithJsonThatIsNotRecipeList_ShouldFail()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ImportFromJsonContent("{}");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(repository.Recipes);
    }

    [Fact]
    public void ImportBackup_WithExistingRecipe_ShouldSkipRecipe()
    {
        string tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"recipes-backup-{Guid.NewGuid()}.json");

        try
        {
            Recipe existingRecipe = CreateValidRecipe();

            string json = JsonSerializer.Serialize(new List<Recipe> { existingRecipe });
            File.WriteAllText(tempFilePath, json);

            FakeRecipeRepository repository = new FakeRecipeRepository();
            repository.Recipes.Add(existingRecipe);

            RecipeBackupService service = new RecipeBackupService(repository);

            RecipeBackupResult result = service.ImportFromJson(tempFilePath);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.ImportedCount);
            Assert.Equal(1, result.SkippedCount);

            Assert.Single(repository.Recipes);
            Assert.Equal(existingRecipe.Name, repository.Recipes[0].Name);
            Assert.Equal(existingRecipe.SourceUrl, repository.Recipes[0].SourceUrl);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public void ImportBackup_WithInvalidJson_ShouldFail()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();

        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ImportFromJson("not valid json");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
    }

    [Fact]
    public void ExportToJson_WithEmptyPath_ShouldFail()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();

        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ExportToJson("");

        Assert.False(result.IsSuccess);
        Assert.Equal(AppTexts.InvalidBackupFile, result.Message);
        Assert.Equal(0, result.ExportedCount);
    }

    [Fact]
    public void ExportToJson_WithInvalidDirectory_ShouldFail()
    {
        string invalidFilePath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString(),
            "recipes-backup.json");

        FakeRecipeRepository repository = new FakeRecipeRepository();

        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ExportToJson(invalidFilePath);

        Assert.False(result.IsSuccess);
        Assert.Equal(AppTexts.InvalidBackupFile, result.Message);
        Assert.Equal(0, result.ExportedCount);
    }

    private static Recipe CreateValidRecipe()
    {
        return CreateValidRecipe(null, "https://example.com/banana-bread");
    }

    private static Recipe CreateValidRecipe(int? userId, string sourceUrl)
    {
        return new Recipe
        {
            Name = "Banana bread",
            SourceUrl = sourceUrl,
            SavedAt = DateTime.Now,
            Notes = "Test notes",
            UserId = userId
        };
    }
}
