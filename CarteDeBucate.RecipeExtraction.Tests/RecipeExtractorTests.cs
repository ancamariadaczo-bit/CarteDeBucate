using RecipeExtraction;
using Xunit;

public class RecipeExtractorTests
{
    private const string SourceUrl = "https://example.com/recipes/pipeline-recipe";

    [Fact]
    public void Extract_WhenHtmlIsEmpty_ReturnsEmptyHtmlFailure()
    {
        RecipeExtractor extractor = new RecipeExtractor();

        RecipeExtractionResult result = extractor.Extract("   ", SourceUrl);

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.Equal(ExtractionMethod.None, result.ExtractionMethod);
        Assert.Equal(FailureReason.EmptyHtml, result.FailureReason);
    }

    [Fact]
    public void Extract_WhenPageIsBlocked_ReturnsBlockedPageBeforeTryingJsonLd()
    {
        string html = """
            <p>Verify you are human</p>
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "Recipe Hidden Behind Block"
            }
            </script>
            """;
        RecipeExtractor extractor = new RecipeExtractor();

        RecipeExtractionResult result = extractor.Extract(html, SourceUrl);

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.Equal(ExtractionMethod.None, result.ExtractionMethod);
        Assert.Equal(FailureReason.BlockedPage, result.FailureReason);
    }

    [Fact]
    public void Extract_WhenJsonLdRecipeExists_ReturnsJsonLdBeforeVisibleHtml()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "JSON-LD Recipe"
            }
            </script>
            <h1>Visible HTML Recipe</h1>
            <h2>Ingredients</h2>
            <ul>
                <li>One visible ingredient</li>
            </ul>
            """;
        RecipeExtractor extractor = new RecipeExtractor();

        RecipeExtractionResult result = extractor.Extract(html, SourceUrl);

        Assert.True(result.Success);
        Assert.NotNull(result.Recipe);
        Assert.Equal("JSON-LD Recipe", result.Recipe.Name);
        Assert.Equal(ExtractionMethod.JsonLd, result.ExtractionMethod);
        Assert.Equal(FailureReason.None, result.FailureReason);
    }

    [Fact]
    public void Extract_WhenOnlyVisibleRecipeExists_ReturnsVisibleHtml()
    {
        string html = """
            <h1>Visible HTML Recipe</h1>
            <h2>Ingredients</h2>
            <ul>
                <li>One visible ingredient</li>
            </ul>
            """;
        RecipeExtractor extractor = new RecipeExtractor();

        RecipeExtractionResult result = extractor.Extract(html, SourceUrl);

        Assert.True(result.Success);
        Assert.NotNull(result.Recipe);
        Assert.Equal("Visible HTML Recipe", result.Recipe.Name);
        Assert.Equal(ExtractionMethod.VisibleHtml, result.ExtractionMethod);
        Assert.Equal(FailureReason.None, result.FailureReason);
    }

    [Fact]
    public void Extract_WhenTitleOnlyIsAllowedAndPageLooksLikeRecipe_ReturnsTitleOnly()
    {
        string html = """
            <title>Title-Only Recipe</title>
            <p>Ingredients</p>
            """;
        RecipeExtractor extractor = new RecipeExtractor();

        RecipeExtractionResult result = extractor.Extract(
            html,
            SourceUrl,
            allowTitleOnly: true);

        Assert.True(result.Success);
        Assert.NotNull(result.Recipe);
        Assert.Equal("Title-Only Recipe", result.Recipe.Name);
        Assert.Empty(result.Recipe.Ingredients);
        Assert.Empty(result.Recipe.Steps);
        Assert.Equal(ExtractionMethod.TitleOnly, result.ExtractionMethod);
        Assert.Equal(FailureReason.None, result.FailureReason);
    }

    [Fact]
    public void Extract_WhenTitleOnlyIsNotAllowed_ReturnsNotRecipePage()
    {
        string html = """
            <title>Title-Only Recipe</title>
            <p>Ingredients</p>
            """;
        RecipeExtractor extractor = new RecipeExtractor();

        RecipeExtractionResult result = extractor.Extract(html, SourceUrl);

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.Equal(ExtractionMethod.None, result.ExtractionMethod);
        Assert.Equal(FailureReason.NotRecipePage, result.FailureReason);
    }

    [Fact]
    public void Extract_WhenNoRecipeCanBeExtracted_ReturnsNotRecipePage()
    {
        string html = """
            <title>Ordinary Article</title>
            <h1>Ordinary Article</h1>
            <p>This page does not contain a recipe.</p>
            """;
        RecipeExtractor extractor = new RecipeExtractor();

        RecipeExtractionResult result = extractor.Extract(
            html,
            "https://example.com/articles/ordinary-article",
            allowTitleOnly: true);

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.Equal(ExtractionMethod.None, result.ExtractionMethod);
        Assert.Equal(FailureReason.NotRecipePage, result.FailureReason);
    }
}
