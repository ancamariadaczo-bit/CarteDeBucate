using RecipeExtraction;

public class RecipeExtractionResultMapperTests
{
    [Fact]
    public void ToImportResult_WithMissingIngredients_ShouldKeepRecipeAvailableForManualCompletion()
    {
        RecipeExtractionResult extractionResult = new()
        {
            Success = true,
            ExtractionMethod = ExtractionMethod.JsonLd,
            Recipe = new ExtractedRecipe
            {
                Name = "Recipe",
                SourceUrl = "https://example.com/recipe"
            }
        };

        RecipeImportResult result =
            RecipeExtractionResultMapper.ToImportResult(extractionResult);

        Assert.True(result.Success);
        Assert.NotNull(result.Recipe);
        Assert.Empty(result.Recipe.Ingredients);
        Assert.Equal(AppTexts.ImportStepsNotExtracted, result.WarningMessage);
    }

    [Fact]
    public void ToImportResult_WithNoExtractedSteps_ShouldReturnAWarningWithoutFailingImport()
    {
        RecipeExtractionResult extractionResult = new()
        {
            Success = true,
            ExtractionMethod = ExtractionMethod.JsonLd,
            Recipe = new ExtractedRecipe
            {
                Name = "Recipe",
                SourceUrl = "https://example.com/recipe",
                Ingredients = ["Flour"]
            }
        };

        RecipeImportResult result =
            RecipeExtractionResultMapper.ToImportResult(extractionResult);

        Assert.True(result.Success);
        Assert.Equal(AppTexts.ImportStepsNotExtracted, result.WarningMessage);
    }

    [Fact]
    public void ToImportResult_ShouldTransportImageUrlWithoutPersistingItInRecipe()
    {
        RecipeExtractionResult extractionResult = new()
        {
            Success = true,
            ExtractionMethod = ExtractionMethod.JsonLd,
            Recipe = new ExtractedRecipe
            {
                Name = "Recipe",
                SourceUrl = "https://example.com/recipe",
                Ingredients = ["Flour"],
                ImageUrl = "https://example.com/recipe.jpg"
            }
        };

        RecipeImportResult result =
            RecipeExtractionResultMapper.ToImportResult(extractionResult);

        Assert.True(result.Success);
        Assert.NotNull(result.Recipe);
        Assert.Equal(
            "https://example.com/recipe.jpg",
            result.ImageUrl);
        Assert.DoesNotContain(
            result.Recipe.GetType().GetProperties(),
            property => property.Name == "ImageUrl");
    }
}
