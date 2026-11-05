
namespace RecipeExtraction;

/// <summary>
/// Identifies why recipe extraction did not produce a result.
/// </summary>
public enum FailureReason
{
    /// <summary>
    /// No failure occurred.
    /// </summary>
    None,

    /// <summary>
    /// The supplied HTML was empty or contained only whitespace.
    /// </summary>
    EmptyHtml,

    /// <summary>
    /// The content resembled an automated verification or blocked page.
    /// </summary>
    BlockedPage,

    /// <summary>
    /// The content did not appear to contain an extractable recipe.
    /// </summary>
    NotRecipePage,

    /// <summary>
    /// The supplied content could not be processed as valid input.
    /// </summary>
    InvalidContent
}
