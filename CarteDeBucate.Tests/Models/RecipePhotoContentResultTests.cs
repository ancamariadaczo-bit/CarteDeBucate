public class RecipePhotoContentResultTests
{
    [Fact]
    public void Success_ShouldReturnCallerOwnedContentAndMetadata()
    {
        using MemoryStream content = new MemoryStream([1, 2, 3]);

        RecipePhotoContentResult result = RecipePhotoContentResult.Success(
            content,
            "image/jpeg",
            "photo.jpg",
            "Loaded.");

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.Success, result.Code);
        Assert.Equal("Loaded.", result.Message);
        Assert.Same(content, result.Content);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal("photo.jpg", result.OriginalFileName);
    }

    [Fact]
    public void Failure_ShouldHaveNullContentAndEmptyContentMetadata()
    {
        RecipePhotoContentResult result = RecipePhotoContentResult.Failure(
            RecipePhotoResultCode.ContentFileMissing,
            "Content file is missing.");

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.ContentFileMissing, result.Code);
        Assert.Equal("Content file is missing.", result.Message);
        Assert.Null(result.Content);
        Assert.Equal(string.Empty, result.ContentType);
        Assert.Equal(string.Empty, result.OriginalFileName);
    }
}
