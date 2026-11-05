namespace RecipeExtraction;

/// <summary>
/// Provides text normalization used by recipe extraction heuristics.
/// </summary>
public static class RecipeExtractionTextHelper
{
    /// <summary>
    /// Normalizes text for case-insensitive searches and Romanian diacritic matching.
    /// </summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized text, or an empty string for blank input.</returns>
    public static string NormalizeForSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        return text
            .ToLowerInvariant()
            .Replace("ă", "a")
            .Replace("â", "a")
            .Replace("î", "i")
            .Replace("ș", "s")
            .Replace("ş", "s")
            .Replace("ț", "t")
            .Replace("ţ", "t");
    }
}
