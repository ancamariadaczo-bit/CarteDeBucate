
namespace RecipeExtraction;

public class RecipeExtractionResult
{
    public bool Success { get; set; }
    public ExtractedRecipe? Recipe { get; set; }
    public ExtractionMethod ExtractionMethod { get; set; }
    public FailureReason FailureReason { get; set; }

}