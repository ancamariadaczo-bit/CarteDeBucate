public class RecipePhotoResultTests
{
    [Fact]
    public void Success_WithPhoto_ShouldCreateSuccessfulResult()
    {
        RecipePhotoInfo photo = new RecipePhotoInfo { Id = 7 };

        RecipePhotoResult result = RecipePhotoResult.Success("Added.", photo);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.Success, result.Code);
        Assert.Equal("Added.", result.Message);
        Assert.Same(photo, result.Photo);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResultWithoutPhoto()
    {
        RecipePhotoResult result = RecipePhotoResult.Failure(
            RecipePhotoResultCode.PhotoNotFound,
            "Photo not found.");

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PhotoNotFound, result.Code);
        Assert.Equal("Photo not found.", result.Message);
        Assert.Null(result.Photo);
    }

    [Theory]
    [InlineData(RecipePhotoResultCode.DeletedMetadataFileWasMissing)]
    [InlineData(RecipePhotoResultCode.CleanupPending)]
    public void SuccessWithWarning_WithWarningCode_ShouldRemainSuccessful(
        RecipePhotoResultCode code)
    {
        RecipePhotoResult result = RecipePhotoResult.SuccessWithWarning(
            code,
            "Completed with warning.");

        Assert.True(result.IsSuccess);
        Assert.Equal(code, result.Code);
        Assert.Equal("Completed with warning.", result.Message);
    }

    [Fact]
    public void Failure_WithSuccessCode_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipePhotoResult.Failure(
                RecipePhotoResultCode.Success,
                "Invalid combination."));
    }
}
