public class RecipePhotoFileNameNormalizerTests
{
    private readonly RecipePhotoFileNameNormalizer _normalizer =
        new RecipePhotoFileNameNormalizer();

    [Fact]
    public void Normalize_WithWindowsPath_ShouldKeepOnlyFileName()
    {
        string result = _normalizer.Normalize(
            @"C:\Users\Anca\Pictures\cake.jpeg",
            RecipePhotoFormat.Jpeg);

        Assert.Equal("cake.jpg", result);
    }

    [Fact]
    public void Normalize_WithUnixPath_ShouldKeepOnlyFileName()
    {
        string result = _normalizer.Normalize(
            "/tmp/photos/soup.PNG",
            RecipePhotoFormat.Png);

        Assert.Equal("soup.png", result);
    }

    [Fact]
    public void Normalize_WithControlCharactersAndOuterSpaces_ShouldRemoveThem()
    {
        string result = _normalizer.Normalize(
            "  reci\0pe\r\n.jpeg  ",
            RecipePhotoFormat.Jpeg);

        Assert.Equal("recipe.jpg", result);
    }

    [Fact]
    public void Normalize_WithEmptyName_ShouldUseDefaultName()
    {
        string result = _normalizer.Normalize(
            string.Empty,
            RecipePhotoFormat.WebP);

        Assert.Equal("photo.webp", result);
    }

    [Fact]
    public void Normalize_WithLongName_ShouldLimitCompleteNameTo255Characters()
    {
        string originalFileName = new string('a', 300) + ".jpeg";

        string result = _normalizer.Normalize(
            originalFileName,
            RecipePhotoFormat.Jpeg);

        Assert.Equal(255, result.Length);
        Assert.EndsWith(".jpg", result);
        Assert.Equal(new string('a', 251) + ".jpg", result);
    }

    [Fact]
    public void Normalize_WithFalseExtension_ShouldUseDetectedFormatExtension()
    {
        string result = _normalizer.Normalize(
            "cake.exe",
            RecipePhotoFormat.Png);

        Assert.Equal("cake.png", result);
    }

    [Fact]
    public void Normalize_WithUnknownFormat_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _normalizer.Normalize("cake.jpg", RecipePhotoFormat.Unknown));
    }
}
