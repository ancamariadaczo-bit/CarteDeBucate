using System.Text;

public sealed class RecipePhotoFileNameNormalizer
{
    private const int MaximumFileNameLength = 255;
    private const string DefaultFileName = "photo";

    /// <summary>
    /// Normalizes display metadata only. The result must not be used to build
    /// a physical storage path.
    /// </summary>
    public string Normalize(
        string? originalFileName,
        RecipePhotoFormat detectedFormat)
    {
        string canonicalExtension =
            RecipePhotoFormats.GetCanonicalExtension(detectedFormat)
            ?? throw new ArgumentOutOfRangeException(
                nameof(detectedFormat),
                detectedFormat,
                "The photo format does not have a canonical extension.");

        string fileName = RemoveDirectoryComponents(originalFileName ?? string.Empty);
        fileName = RemoveControlCharacters(fileName).Trim();
        fileName = RemoveDeclaredExtension(fileName).Trim();

        if (fileName.Length == 0)
        {
            fileName = DefaultFileName;
        }

        int maximumBaseNameLength =
            MaximumFileNameLength - canonicalExtension.Length;
        fileName = TruncateWithoutSplittingSurrogatePair(
            fileName,
            maximumBaseNameLength);

        return fileName + canonicalExtension;
    }

    private static string RemoveDirectoryComponents(string fileName)
    {
        int lastSeparatorIndex = Math.Max(
            fileName.LastIndexOf('/'),
            fileName.LastIndexOf('\\'));

        return lastSeparatorIndex >= 0
            ? fileName[(lastSeparatorIndex + 1)..]
            : fileName;
    }

    private static string RemoveControlCharacters(string fileName)
    {
        StringBuilder sanitizedFileName = new StringBuilder(fileName.Length);

        foreach (char character in fileName)
        {
            if (!char.IsControl(character))
            {
                sanitizedFileName.Append(character);
            }
        }

        return sanitizedFileName.ToString();
    }

    private static string RemoveDeclaredExtension(string fileName)
    {
        int extensionSeparatorIndex = fileName.LastIndexOf('.');

        return extensionSeparatorIndex >= 0
            ? fileName[..extensionSeparatorIndex]
            : fileName;
    }

    private static string TruncateWithoutSplittingSurrogatePair(
        string value,
        int maximumLength)
    {
        if (value.Length <= maximumLength)
        {
            return value;
        }

        int truncatedLength = maximumLength;

        if (char.IsHighSurrogate(value[truncatedLength - 1]))
        {
            truncatedLength--;
        }

        return value[..truncatedLength];
    }
}
