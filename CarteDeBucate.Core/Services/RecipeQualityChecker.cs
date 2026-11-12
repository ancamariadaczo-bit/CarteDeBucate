public static class RecipeQualityChecker
{
    public static bool HasRequiredElements(Recipe? recipe)
    {
        return HasValidName(recipe) && HasIngredients(recipe);
    }

    public static bool HasValidName(Recipe? recipe)
    {
        return recipe != null &&
            !string.IsNullOrWhiteSpace(recipe.Name) &&
            recipe.Name != AppTexts.ImportedTitleNotFound;
    }

    public static bool HasIngredients(Recipe? recipe)
    {
        return recipe?.Ingredients?.Any(ingredient =>
            !string.IsNullOrWhiteSpace(ingredient)) == true;
    }

}
