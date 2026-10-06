public class RecipePhotoFormatsTests
{
    [Theory]
    [InlineData(RecipePhotoFormat.Jpeg, "image/jpeg")]
    [InlineData(RecipePhotoFormat.Png, "image/png")]
    [InlineData(RecipePhotoFormat.WebP, "image/webp")]
    public void GetContentType_WithKnownFormat_ShouldReturnNormalizedContentType(
        RecipePhotoFormat format,
        string expectedContentType)
    {
        string? contentType = RecipePhotoFormats.GetContentType(format);

        Assert.Equal(expectedContentType, contentType);
    }

    [Theory]
    [InlineData(RecipePhotoFormat.Jpeg, ".jpg")]
    [InlineData(RecipePhotoFormat.Png, ".png")]
    [InlineData(RecipePhotoFormat.WebP, ".webp")]
    public void GetCanonicalExtension_WithKnownFormat_ShouldReturnExtension(
        RecipePhotoFormat format,
        string expectedExtension)
    {
        string? extension = RecipePhotoFormats.GetCanonicalExtension(format);

        Assert.Equal(expectedExtension, extension);
    }

    [Theory]
    [MemberData(nameof(KnownSignatures))]
    public void DetectFormat_WithKnownSignature_ShouldReturnFormat(
        byte[] content,
        RecipePhotoFormat expectedFormat)
    {
        RecipePhotoFormat format = RecipePhotoFormats.DetectFormat(content);

        Assert.Equal(expectedFormat, format);
    }

    [Fact]
    public void GetMappings_WithUnknownFormat_ShouldReturnNull()
    {
        Assert.Null(RecipePhotoFormats.GetContentType(RecipePhotoFormat.Unknown));
        Assert.Null(RecipePhotoFormats.GetCanonicalExtension(RecipePhotoFormat.Unknown));
    }

    [Theory]
    [MemberData(nameof(UnknownOrIncompleteSignatures))]
    public void DetectFormat_WithUnknownOrIncompleteSignature_ShouldReturnUnknown(
        byte[] content)
    {
        RecipePhotoFormat format = RecipePhotoFormats.DetectFormat(content);

        Assert.Equal(RecipePhotoFormat.Unknown, format);
    }

    public static TheoryData<byte[], RecipePhotoFormat> KnownSignatures => new()
    {
        {
            [0xFF, 0xD8, 0xFF, 0xE0],
            RecipePhotoFormat.Jpeg
        },
        {
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            RecipePhotoFormat.Png
        },
        {
            [
                0x52, 0x49, 0x46, 0x46,
                0x24, 0x00, 0x00, 0x00,
                0x57, 0x45, 0x42, 0x50
            ],
            RecipePhotoFormat.WebP
        }
    };

    public static TheoryData<byte[]> UnknownOrIncompleteSignatures => new()
    {
        Array.Empty<byte>(),
        new byte[] { 0x89, 0x50, 0x4E },
        new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }
    };
}
