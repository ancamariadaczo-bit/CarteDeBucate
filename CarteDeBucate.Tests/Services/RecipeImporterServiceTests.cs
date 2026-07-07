public class RecipeServiceTests
{
    [Fact]
    public async Task ImportRecipeFromUrlAsync_WithSuccessfulImport_ShouldReturnImportedRecipe()
    {
        Recipe recipe = CreateValidRecipe();

        FakeRecipeImporter importer = new FakeRecipeImporter
        {
            ImportResult = new RecipeImportResult
            {
                Success = true,
                Recipe = recipe,
                Message = "Import successful."
            }
        };

        RecipeImporterService service = new RecipeImporterService(importer);

        RecipeImportResult result = await service.ImportRecipeFromUrlAsync(recipe.SourceUrl);

        Assert.True(result.Success);
        Assert.Equal(recipe, result.Recipe);
        Assert.Equal("Import successful.", result.Message);
    }

    [Fact]
    public async Task ImportRecipeFromUrlAsync_WithEmptyUrl_ShouldReturnFailedImport()
    {
        FakeRecipeImporter importer = new FakeRecipeImporter();
        RecipeImporterService service = new RecipeImporterService(importer);

        RecipeImportResult result = await service.ImportRecipeFromUrlAsync("");

        Assert.False(result.Success);
        Assert.Equal(AppTexts.EmptyUrl, result.Message);
    }

    private static Recipe CreateValidRecipe()
    {
        return CreateValidRecipe(userId: null);
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
