public class RecipePhotosResultTests
{
    [Fact]
    public void Success_WithEmptyList_ShouldCreateSuccessfulResult()
    {
        RecipePhotosResult result = RecipePhotosResult.Success(
            Array.Empty<RecipePhotoInfo>(),
            "No photos.");

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.Success, result.Code);
        Assert.Equal("No photos.", result.Message);
        Assert.Empty(result.Photos);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResultWithEmptyList()
    {
        RecipePhotosResult result = RecipePhotosResult.Failure(
            RecipePhotoResultCode.RecipeNotFound,
            "Recipe not found.");

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.RecipeNotFound, result.Code);
        Assert.Equal("Recipe not found.", result.Message);
        Assert.Empty(result.Photos);
    }
}
