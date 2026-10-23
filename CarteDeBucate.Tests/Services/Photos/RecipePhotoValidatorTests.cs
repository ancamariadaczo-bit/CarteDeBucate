public class RecipePhotoValidatorTests
{
    private readonly RecipePhotoValidator _validator = new RecipePhotoValidator();

    [Theory]
    [MemberData(nameof(ValidPhotoContent))]
    public void Validate_WithSupportedContent_ShouldReturnNormalizedFormat(
        byte[] content,
        string declaredContentType,
        RecipePhotoFormat expectedFormat,
        string expectedContentType)
    {
        using MemoryStream stream = new MemoryStream(content);

        RecipePhotoValidationResult result = _validator.Validate(
            stream,
            declaredContentType);

        Assert.True(result.IsValid);
        Assert.Equal(RecipePhotoResultCode.Success, result.Code);
        Assert.Equal(expectedFormat, result.Format);
        Assert.Equal(expectedContentType, result.ContentType);
    }

    [Fact]
    public void Validate_WithEmptyContent_ShouldReturnEmptyContent()
    {
        using MemoryStream stream = new MemoryStream();

        RecipePhotoValidationResult result = _validator.Validate(
            stream,
            "image/jpeg");

        Assert.False(result.IsValid);
        Assert.Equal(RecipePhotoResultCode.EmptyContent, result.Code);
        Assert.Equal(RecipePhotoFormat.Unknown, result.Format);
        Assert.Equal(string.Empty, result.ContentType);
    }

    [Fact]
    public void Validate_WithMismatchedContentType_ShouldReturnInvalidContent()
    {
        using MemoryStream stream = new MemoryStream(PngContent());

        RecipePhotoValidationResult result = _validator.Validate(
            stream,
            "image/jpeg");

        Assert.False(result.IsValid);
        Assert.Equal(RecipePhotoResultCode.InvalidContent, result.Code);
    }

    [Fact]
    public void Validate_WithFalsifiedContent_ShouldReturnInvalidContent()
    {
        using MemoryStream stream = new MemoryStream("not an image"u8.ToArray());

        RecipePhotoValidationResult result = _validator.Validate(
            stream,
            "image/jpeg");

        Assert.False(result.IsValid);
        Assert.Equal(RecipePhotoResultCode.InvalidContent, result.Code);
    }

    [Fact]
    public void Validate_WithIncompleteSignature_ShouldReturnInvalidContent()
    {
        using MemoryStream stream = new MemoryStream([0xFF, 0xD8]);

        RecipePhotoValidationResult result = _validator.Validate(
            stream,
            "image/jpeg");

        Assert.False(result.IsValid);
        Assert.Equal(RecipePhotoResultCode.InvalidContent, result.Code);
    }

    [Fact]
    public void Validate_WithUnsupportedFormat_ShouldReturnUnsupportedFormat()
    {
        using MemoryStream stream = new MemoryStream(
            [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]);

        RecipePhotoValidationResult result = _validator.Validate(
            stream,
            "image/gif");

        Assert.False(result.IsValid);
        Assert.Equal(RecipePhotoResultCode.UnsupportedFormat, result.Code);
    }

    [Fact]
    public void Validate_ShouldRestoreInitialStreamPositionAndLeaveStreamOpen()
    {
        using MemoryStream stream = new MemoryStream(JpegContent());
        stream.Position = 2;

        RecipePhotoValidationResult result = _validator.Validate(
            stream,
            "image/jpeg");

        Assert.True(result.IsValid);
        Assert.Equal(2, stream.Position);
        Assert.True(stream.CanRead);
    }

    public static TheoryData<
        byte[],
        string,
        RecipePhotoFormat,
        string> ValidPhotoContent => new()
    {
        {
            JpegContent(),
            " IMAGE/JPEG ",
            RecipePhotoFormat.Jpeg,
            "image/jpeg"
        },
        {
            PngContent(),
            "image/png",
            RecipePhotoFormat.Png,
            "image/png"
        },
        {
            WebPContent(),
            "image/webp",
            RecipePhotoFormat.WebP,
            "image/webp"
        }
    };

    private static byte[] JpegContent()
    {
        return [0xFF, 0xD8, 0xFF, 0xE0, 0x01];
    }

    private static byte[] PngContent()
    {
        return [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];
    }

    private static byte[] WebPContent()
    {
        return
        [
            0x52, 0x49, 0x46, 0x46,
            0x24, 0x00, 0x00, 0x00,
            0x57, 0x45, 0x42, 0x50,
            0x01
        ];
    }
}
