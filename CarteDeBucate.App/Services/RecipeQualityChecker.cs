public class RecipeQualityChecker
{
    public bool HasRequiredElements(Recipe? recipe)
    {
        return HasValidName(recipe) &&
            HasIngredients(recipe) &&
            HasSteps(recipe);
    }

    private static bool HasValidName(Recipe? recipe)
    {
        return recipe != null &&
            !string.IsNullOrWhiteSpace(recipe.Name) &&
            recipe.Name != AppTexts.ImportedTitleNotFound;
    }

    private static bool HasIngredients(Recipe? recipe)
    {
        return recipe?.Ingredients.Any(ingredient =>
            !string.IsNullOrWhiteSpace(ingredient)) == true;
    }

    private static bool HasSteps(Recipe? recipe)
    {
        return recipe?.Steps.Any(step =>
            !string.IsNullOrWhiteSpace(step)) == true;
    }
}
