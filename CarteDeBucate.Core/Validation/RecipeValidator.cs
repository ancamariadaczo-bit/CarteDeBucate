public static class RecipeValidator
{
    public static RecipeValidationResult ValidateForSave(Recipe recipe)
    {
        RecipeValidationResult result = new RecipeValidationResult();

        if (!RecipeQualityChecker.HasValidName(recipe))
        {
            result.Errors.Add(AppTexts.RecipeNameRequired);
        }

        if (!string.IsNullOrWhiteSpace(recipe.SourceUrl) &&
            !UrlValidator.IsValidHttpUrl(recipe.SourceUrl))
        {
            result.Errors.Add(AppTexts.RecipeSourceUrlInvalid);
        }

        if (recipe.SavedAt == default)
        {
            result.Errors.Add(AppTexts.RecipeSavedDateRequired);
        }

        if (!RecipeQualityChecker.HasIngredients(recipe))
        {
            result.Errors.Add(AppTexts.RecipeIngredientsRequired);
        }

        return result;
    }
}
