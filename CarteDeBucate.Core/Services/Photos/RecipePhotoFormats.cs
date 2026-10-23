public static class RecipePhotoFormats
{
    private delegate bool SignatureMatcher(ReadOnlySpan<byte> content);

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly RecipePhotoFormatDefinition[] Definitions =
    [
        new(
            RecipePhotoFormat.Jpeg,
            "image/jpeg",
            ".jpg",
            content => content.StartsWith(JpegSignature)),
        new(
            RecipePhotoFormat.Png,
            "image/png",
            ".png",
            content => content.StartsWith(PngSignature)),
        new(
            RecipePhotoFormat.WebP,
            "image/webp",
            ".webp",
            IsWebPSignature)
    ];

    public static string? GetContentType(RecipePhotoFormat format)
    {
        return FindDefinition(format)?.ContentType;
    }

    public static string? GetCanonicalExtension(RecipePhotoFormat format)
    {
        return FindDefinition(format)?.CanonicalExtension;
    }

    public static RecipePhotoFormat GetFormatFromContentType(
        string? contentType)
    {
        string normalizedContentType = contentType?.Trim() ?? string.Empty;

        foreach (RecipePhotoFormatDefinition definition in Definitions)
        {
            if (string.Equals(
                definition.ContentType,
                normalizedContentType,
                StringComparison.OrdinalIgnoreCase))
            {
                return definition.Format;
            }
        }

        return RecipePhotoFormat.Unknown;
    }

    public static RecipePhotoFormat DetectFormat(ReadOnlySpan<byte> content)
    {
        foreach (RecipePhotoFormatDefinition definition in Definitions)
        {
            if (definition.MatchesSignature(content))
            {
                return definition.Format;
            }
        }

        return RecipePhotoFormat.Unknown;
    }

    private static RecipePhotoFormatDefinition? FindDefinition(
        RecipePhotoFormat format)
    {
        foreach (RecipePhotoFormatDefinition definition in Definitions)
        {
            if (definition.Format == format)
            {
                return definition;
            }
        }

        return null;
    }

    private static bool IsWebPSignature(ReadOnlySpan<byte> content)
    {
        return content.Length >= 12
            && content[..4].SequenceEqual("RIFF"u8)
            && content.Slice(8, 4).SequenceEqual("WEBP"u8);
    }

    private sealed record RecipePhotoFormatDefinition(
        RecipePhotoFormat Format,
        string ContentType,
        string CanonicalExtension,
        SignatureMatcher MatchesSignature);
}
