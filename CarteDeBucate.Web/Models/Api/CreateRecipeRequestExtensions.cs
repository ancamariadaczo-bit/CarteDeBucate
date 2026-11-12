
namespace CarteDeBucate.Web.Models.Api;

public static class CreateRecipeRequestExtensions
{
    public static Recipe ToRecipe(this CreateRecipeRequest request)
    {
        return new Recipe
        {
            Name = request.Name,
            SourceUrl = request.SourceUrl?.Trim() ?? string.Empty,
            Ingredients = request.Ingredients,
            Steps = request.Steps ?? []
        };
    }
}
