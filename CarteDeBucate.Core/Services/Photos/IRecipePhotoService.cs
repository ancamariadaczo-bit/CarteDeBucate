public interface IRecipePhotoService
{
    Task<RecipePhotoResult> AddPhotoAsync(
        int recipeId,
        Stream content,
        string originalFileName,
        string declaredContentType,
        long? declaredLength,
        RecipePhotoOrigin origin,
        CancellationToken cancellationToken = default);

    RecipePhotosResult GetPhotosForRecipe(int recipeId);

    RecipePhotoContentResult GetPhotoContent(int photoId);

    RecipePhotoResult DeletePhoto(int photoId);
}
