
namespace RecipeExtraction;

/// <summary>
/// Represents recipe data extracted from a source document without application-specific fields.
/// </summary>
public class ExtractedRecipe
{
    /// <summary>
    /// Gets or sets the extracted recipe name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the source URL associated with the extracted recipe.
    /// </summary>
    public string SourceUrl { get; set; } = "";

    /// <summary>
    /// Gets or sets the extracted ingredient descriptions.
    /// </summary>
    public List<string> Ingredients { get; set; } = new();

    /// <summary>
    /// Gets or sets the extracted preparation steps.
    /// </summary>
    public List<string> Steps { get; set; } = new();
}
