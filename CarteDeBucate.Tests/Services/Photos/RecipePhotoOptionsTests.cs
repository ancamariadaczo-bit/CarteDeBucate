public class RecipePhotoOptionsTests
{
    [Fact]
    public void Defaults_ShouldMatchPhotoServiceLimits()
    {
        RecipePhotoOptions options = new RecipePhotoOptions();

        Assert.Equal(10L * 1024 * 1024, options.MaxFileSizeBytes);
        Assert.Equal(20, options.MaxPhotosPerRecipe);
        Assert.Equal(TimeSpan.FromHours(24), options.ReconciliationMinimumAge);
    }
}
