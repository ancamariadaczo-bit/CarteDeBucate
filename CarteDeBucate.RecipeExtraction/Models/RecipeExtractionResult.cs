
namespace RecipeExtraction;

/// <summary>
/// Represents the outcome of a recipe extraction attempt.
/// </summary>
public class RecipeExtractionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether extraction succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the extracted recipe when extraction succeeds.
    /// </summary>
    public ExtractedRecipe? Recipe { get; set; }

    /// <summary>
    /// Gets or sets the strategy that produced the recipe.
    /// </summary>
    public ExtractionMethod ExtractionMethod { get; set; }

    /// <summary>
    /// Gets or sets the reason extraction failed.
    /// </summary>
    public FailureReason FailureReason { get; set; }
}
