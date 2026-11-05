
namespace RecipeExtraction;

public class ExtractedRecipe
{
    public string Name { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public List<string> Ingredients { get; set; } = new();
    public List<string> Steps { get; set; } = new();
}