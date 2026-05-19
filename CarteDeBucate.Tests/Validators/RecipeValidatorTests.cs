public class RecipeValidatorTests
{
    [Fact]
    public void ValidateForSave_WithValidRecipe_ShouldReturnValidResult()
    {
        Recipe recipe = CreateValidRecipe();

        RecipeValidationResult result = RecipeValidator.ValidateForSave(recipe);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateForSave_WithMissingName_ShouldReturnInvalidResult()
    {
        Recipe recipe = CreateValidRecipe();
        recipe.Name = "";

        RecipeValidationResult result = RecipeValidator.ValidateForSave(recipe);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateForSave_WithWhitespaceName_ShouldReturnInvalidResult()
    {
        Recipe recipe = CreateValidRecipe();
        recipe.Name = "   ";

        RecipeValidationResult result = RecipeValidator.ValidateForSave(recipe);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateForSave_WithMissingSourceUrl_ShouldReturnInvalidResult()
    {
        Recipe recipe = CreateValidRecipe();
        recipe.SourceUrl = "";

        RecipeValidationResult result = RecipeValidator.ValidateForSave(recipe);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateForSave_WithMissingSavedAt_ShouldReturnInvalidResult()
    {
        Recipe recipe = CreateValidRecipe();
        recipe.SavedAt = default;

        RecipeValidationResult result = RecipeValidator.ValidateForSave(recipe);

        Assert.False(result.IsValid);
        Assert.Contains(AppTexts.RecipeSavedDateRequired, result.Errors);
    }

    [Fact]
    public void ValidateForSave_WithNoIngredients_ShouldReturnInvalidResult()
    {
        Recipe recipe = CreateValidRecipe();
        recipe.Ingredients.Clear();

        RecipeValidationResult result = RecipeValidator.ValidateForSave(recipe);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateForSave_WithNoSteps_ShouldReturnInvalidResult()
    {
        Recipe recipe = CreateValidRecipe();
        recipe.Steps.Clear();

        RecipeValidationResult result = RecipeValidator.ValidateForSave(recipe);

        Assert.False(result.IsValid);
    }

    private static Recipe CreateValidRecipe()
    {
        return new Recipe
        {
            Name = "Banana bread",
            SourceUrl = "https://example.com/banana-bread",
            SavedAt = DateTime.Now,
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