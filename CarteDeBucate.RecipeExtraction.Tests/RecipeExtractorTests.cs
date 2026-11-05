using Xunit;
using RecipeExtraction;

public class RecipeExtractorTests
{

    [Fact]
    public void FirstTest()
    {
        RecipeExtractor extractor = new RecipeExtractor();

        RecipeExtractionResult result = extractor.Extract();

        Assert.NotNull(result);
    }
}
