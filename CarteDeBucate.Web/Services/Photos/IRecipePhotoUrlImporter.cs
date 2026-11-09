using CarteDeBucate.Web.Models;

namespace CarteDeBucate.Web.Services.Photos;

public interface IRecipePhotoUrlImporter
{
    Task<RecipePhotoUrlImportResult> ImportAsync(
        int recipeId,
        string imageUrl,
        CancellationToken cancellationToken = default);
}
