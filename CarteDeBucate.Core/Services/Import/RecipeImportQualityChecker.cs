public class RecipeImportQualityChecker
{
    public string? GetWarning(Recipe? recipe)
    {
        return HasSteps(recipe)
            ? null
            : AppTexts.ImportStepsNotExtracted;
    }

    private static bool HasSteps(Recipe? recipe)
    {
        return recipe?.Steps.Any(step => !string.IsNullOrWhiteSpace(step)) == true;
    }
}
