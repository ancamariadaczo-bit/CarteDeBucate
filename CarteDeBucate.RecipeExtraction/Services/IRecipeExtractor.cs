namespace RecipeExtraction;

/// <summary>
/// Defines the public recipe extraction pipeline.
/// </summary>
public interface IRecipeExtractor
{
    /// <summary>
    /// Extracts neutral recipe data from supplied HTML content.
    /// </summary>
    /// <param name="html">The HTML content to inspect.</param>
    /// <param name="url">The source URL associated with the HTML content.</param>
    /// <param name="allowTitleOnly">
    /// <see langword="true"/> to allow a title-only result when the page still resembles a recipe;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>The extraction result, including the strategy or failure reason.</returns>
    RecipeExtractionResult Extract(string html, string url, bool allowTitleOnly = false);
}
