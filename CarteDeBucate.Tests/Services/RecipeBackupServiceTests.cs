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

            using JsonDocument jsonDocument = JsonDocument.Parse(json);
            JsonElement root = jsonDocument.RootElement;

            Assert.Equal(RecipeBackupVersions.Current, root.GetProperty("version").GetInt32());
            JsonElement exportedRecipe = Assert.Single(root.GetProperty("recipes").EnumerateArray());
            Assert.Equal(recipe.Name, exportedRecipe.GetProperty("name").GetString());
            Assert.Equal(recipe.SourceUrl, exportedRecipe.GetProperty("sourceUrl").GetString());
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
        recipe.Id = 42;
        recipe.UserId = 7;
        repository.Recipes.Add(recipe);
        repository.Recipes.Add(CreateValidRecipe());

        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupExportResult result = service.ExportToJsonContent();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ExportedCount);
        Assert.Contains(Environment.NewLine, result.Json);

        using JsonDocument jsonDocument = JsonDocument.Parse(result.Json);
        JsonElement root = jsonDocument.RootElement;

        Assert.Equal(
            new[] { "version", "exportedAtUtc", "recipes" },
            root.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(RecipeBackupVersions.Current, root.GetProperty("version").GetInt32());
        Assert.Equal(DateTimeKind.Utc, root.GetProperty("exportedAtUtc").GetDateTime().Kind);

        JsonElement[] exportedRecipes = root.GetProperty("recipes").EnumerateArray().ToArray();
        Assert.Equal(2, exportedRecipes.Length);
        Assert.All(exportedRecipes, exportedRecipe =>
        {
            Assert.False(exportedRecipe.TryGetProperty("id", out _));
            Assert.False(exportedRecipe.TryGetProperty("userId", out _));
        });
        Assert.Equal(recipe.Name, exportedRecipes[0].GetProperty("name").GetString());
        Assert.Equal(recipe.SourceUrl, exportedRecipes[0].GetProperty("sourceUrl").GetString());
        Assert.DoesNotContain("\"format\"", result.Json, StringComparison.OrdinalIgnoreCase);
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

        using JsonDocument jsonDocument = JsonDocument.Parse(result.Json);
        JsonElement root = jsonDocument.RootElement;
        JsonElement exportedRecipe = Assert.Single(root.GetProperty("recipes").EnumerateArray());

        Assert.False(exportedRecipe.TryGetProperty("id", out _));
        Assert.False(exportedRecipe.TryGetProperty("userId", out _));
        Assert.Equal(
            "https://example.com/current-user",
            exportedRecipe.GetProperty("sourceUrl").GetString());
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
    public void ImportFromJsonContent_WithLegacyArray_ShouldIgnoreLocalIdentity()
    {
        Recipe legacyRecipe = CreateValidRecipe(99, "https://example.com/legacy");
        legacyRecipe.Id = 42;
        string json = JsonSerializer.Serialize(new List<Recipe> { legacyRecipe });
        FakeRecipeRepository repository = new FakeRecipeRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });
        RecipeBackupService service = new RecipeBackupService(repository, currentUserContext);

        RecipeBackupResult result = service.ImportFromJsonContent(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ImportedCount);
        Recipe savedRecipe = Assert.Single(repository.Recipes);
        Assert.Equal(0, savedRecipe.Id);
        Assert.Equal(7, savedRecipe.UserId);
        Assert.Equal(legacyRecipe.SourceUrl, savedRecipe.SourceUrl);
    }

    [Fact]
    public void ImportFromJsonContent_WithEmptyLegacyArray_ShouldSucceedWithoutImports()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ImportFromJsonContent("[]");

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(repository.Recipes);
    }

    [Fact]
    public void ImportFromJsonContent_WithInvalidLegacyArray_ShouldFail()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ImportFromJsonContent("[null]");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(repository.Recipes);
    }

    [Fact]
    public void ImportFromJsonContent_WithVersionTwoDocument_ShouldSaveRecipeForCurrentUser()
    {
        const string json = """
            {
              "version": 2,
              "exportedAtUtc": "2026-09-02T12:00:00Z",
              "recipes": [
                {
                  "name": "Banana bread",
                  "sourceUrl": "https://example.com/version-two",
                  "savedAt": "2026-09-02T10:00:00Z",
                  "ingredients": ["Banane"],
                  "steps": ["Coace"],
                  "notes": "Test notes",
                  "status": 3
                }
              ]
            }
            """;
        FakeRecipeRepository repository = new FakeRecipeRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        currentUserContext.SetCurrentUser(new User { Id = 7, Username = "ana" });
        RecipeBackupService service = new RecipeBackupService(repository, currentUserContext);

        RecipeBackupResult result = service.ImportFromJsonContent(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Recipe savedRecipe = Assert.Single(repository.Recipes);
        Assert.Equal(0, savedRecipe.Id);
        Assert.Equal(7, savedRecipe.UserId);
        Assert.Equal("https://example.com/version-two", savedRecipe.SourceUrl);
    }

    [Fact]
    public void ImportFromJsonContent_WithoutCurrentUser_ShouldClearImportedUserId()
    {
        Recipe recipe = CreateValidRecipe(99, "https://example.com/imported-user");
        string json = JsonSerializer.Serialize(new List<Recipe> { recipe });

        FakeRecipeRepository repository = new FakeRecipeRepository();
        RecipeBackupService service = new RecipeBackupService(repository);

        RecipeBackupResult result = service.ImportFromJsonContent(json);

        Assert.True(result.IsSuccess);

        Recipe savedRecipe = Assert.Single(repository.Recipes);
        Assert.Null(savedRecipe.UserId);
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
