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

    private static Recipe CreateValidRecipe()
    {
        return new Recipe
        {
            Name = "Banana bread",
            SourceUrl = "https://example.com/banana-bread",
            SavedAt = DateTime.Now,
            Notes = "Test notes"
        };
    }
}