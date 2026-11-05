
namespace RecipeExtraction;

/// <summary>
/// Identifies the strategy that produced an extraction result.
/// </summary>
public enum ExtractionMethod
{
    /// <summary>
    /// No extraction strategy produced a result.
    /// </summary>
    None,

    /// <summary>
    /// The recipe was extracted from JSON-LD structured data.
    /// </summary>
    JsonLd,

    /// <summary>
    /// The recipe was extracted from visible HTML content.
    /// </summary>
    VisibleHtml,

    /// <summary>
    /// Only the page title was extracted.
    /// </summary>
    TitleOnly
}
