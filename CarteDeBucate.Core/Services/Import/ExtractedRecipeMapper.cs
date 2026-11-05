using RecipeExtraction;

public static class ExtractedRecipeMapper
{
    public static Recipe ToRecipe(ExtractedRecipe extractedRecipe)
    {
        ArgumentNullException.ThrowIfNull(extractedRecipe);

        return new Recipe
        {
            Name = extractedRecipe.Name,
            SourceUrl = extractedRecipe.SourceUrl?.Trim() ?? "",
            SavedAt = DateTime.Now,
            Ingredients = extractedRecipe.Ingredients is null
                ? new List<string>()
                : new List<string>(extractedRecipe.Ingredients),
            Steps = extractedRecipe.Steps is null
                ? new List<string>()
                : new List<string>(extractedRecipe.Steps),
            Status = RecipeStatus.Saved
        };
    }
}
