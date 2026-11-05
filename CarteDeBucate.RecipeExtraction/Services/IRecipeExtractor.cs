namespace RecipeExtraction;

public interface IRecipeExtractor
{
    RecipeExtractionResult Extract(string html, string url, bool allowTitleOnly = false);
}
