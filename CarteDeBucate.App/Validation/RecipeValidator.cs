public static class RecipeValidator
{
    public static RecipeValidationResult ValidateForSave(Recipe recipe)
    {
        RecipeValidationResult result = new RecipeValidationResult();

        if (string.IsNullOrWhiteSpace(recipe.Name))
        {
            result.Errors.Add(AppTexts.RecipeNameRequired);
        }

        if (string.IsNullOrWhiteSpace(recipe.SourceUrl))
        {
            result.Errors.Add(AppTexts.RecipeSourceUrlRequired);
        }

        if (recipe.SavedAt == default)
        {
            result.Errors.Add(AppTexts.RecipeSavedDateRequired);
        }

        if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
        {
            result.Errors.Add(AppTexts.RecipeIngredientsRequired);
        }

        if (recipe.Steps == null || recipe.Steps.Count == 0)
        {
            result.Errors.Add(AppTexts.RecipeStepsRequired);
        }

        return result;
    }
}