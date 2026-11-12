public class RecipeImportQualityCheckerTests
{
    [Fact]
    public void GetWarning_WithNoExtractedSteps_ShouldReturnStepsWarning()
    {
        Recipe recipe = new Recipe { Steps = [] };

        string? warning = new RecipeImportQualityChecker().GetWarning(recipe);

        Assert.Equal(AppTexts.ImportStepsNotExtracted, warning);
    }

    [Fact]
    public void GetWarning_WithExtractedSteps_ShouldReturnNoWarning()
    {
        Recipe recipe = new Recipe
        {
            Name = "Cake",
            Ingredients = ["Flour"],
            Steps = ["Bake for 30 minutes."]
        };

        string? warning = new RecipeImportQualityChecker().GetWarning(recipe);

        Assert.Null(warning);
    }

    [Fact]
    public void GetWarning_WithMissingNameAndIngredientsButExtractedSteps_ShouldReturnNoWarning()
    {
        Recipe recipe = new Recipe { Name = "", Ingredients = [], Steps = ["Bake."] };

        string? warning = new RecipeImportQualityChecker().GetWarning(recipe);

        Assert.Null(warning);
    }
}
