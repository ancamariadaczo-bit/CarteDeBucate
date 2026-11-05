using RecipeExtraction;
using Xunit;

public class RecipeHtmlExtractorTests
{
    private const string SourceUrl = "https://example.com/recipes/visible-recipe";

    [Fact]
    public void TryExtract_WhenH1Exists_UsesH1AsTitle()
    {
        string html = """
            <title>Fallback Title</title>
            <h1>Visible Recipe Title</h1>
            <h2>Ingredients</h2>
            <ul>
                <li>One ingredient</li>
            </ul>
            """;
        RecipeHtmlExtractor extractor = new RecipeHtmlExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal("Visible Recipe Title", recipe.Name);
    }

    [Fact]
    public void TryExtract_WhenH1IsMissing_UsesTitleElementAsFallback()
    {
        string html = """
            <title>Fallback Recipe Title</title>
            <h2>Ingredients</h2>
            <ul>
                <li>One ingredient</li>
            </ul>
            """;
        RecipeHtmlExtractor extractor = new RecipeHtmlExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal("Fallback Recipe Title", recipe.Name);
    }

    [Fact]
    public void TryExtract_WithEnglishIngredientHeading_ExtractsIngredientListInOrder()
    {
        string html = """
            <h1>English Ingredient Recipe</h1>
            <h2>Ingredients</h2>
            <ul>
                <li>200 g flour</li>
                <li>100 ml water</li>
            </ul>
            <h2>Instructions</h2>
            """;
        RecipeHtmlExtractor extractor = new RecipeHtmlExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal(new[] { "200 g flour", "100 ml water" }, recipe.Ingredients);
    }

    [Fact]
    public void TryExtract_WithEnglishInstructionHeading_ExtractsInstructionListInOrder()
    {
        string html = """
            <h1>English Instruction Recipe</h1>
            <h2>Instructions</h2>
            <ol>
                <li>Mix the ingredients.</li>
                <li>Bake the mixture.</li>
            </ol>
            <h2>Notes</h2>
            """;
        RecipeHtmlExtractor extractor = new RecipeHtmlExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal(new[] { "Mix the ingredients.", "Bake the mixture." }, recipe.Steps);
    }

    [Fact]
    public void TryExtract_WithRomanianHeadings_ExtractsIngredientsAndInstructions()
    {
        string html = """
            <h1>Rețetă românească</h1>
            <h2>Ingrediente</h2>
            <ul>
                <li>200 g făină</li>
                <li>100 ml apă</li>
            </ul>
            <h2>Mod de preparare</h2>
            <ol>
                <li>Amestecă ingredientele.</li>
                <li>Coace aluatul.</li>
            </ol>
            <h2>Poftă bună</h2>
            """;
        RecipeHtmlExtractor extractor = new RecipeHtmlExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal(new[] { "200 g făină", "100 ml apă" }, recipe.Ingredients);
        Assert.Equal(new[] { "Amestecă ingredientele.", "Coace aluatul." }, recipe.Steps);
    }

    [Fact]
    public void TryExtract_WithHtmlEntitiesAndExtraWhitespace_DecodesAndNormalizesText()
    {
        string html = """
            <h1>
                Pasta &amp;   Cheese
            </h1>
            <h2>Ingredients</h2>
            <ul>
                <li>  1&nbsp;cup   cheese  </li>
            </ul>
            <h2>Instructions</h2>
            <ol>
                <li>  Mix &amp;   bake.  </li>
            </ol>
            """;
        RecipeHtmlExtractor extractor = new RecipeHtmlExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal("Pasta & Cheese", recipe.Name);
        Assert.Equal(new[] { "1 cup cheese" }, recipe.Ingredients);
        Assert.Equal(new[] { "Mix & bake." }, recipe.Steps);
    }

    [Fact]
    public void TryExtract_WhenPageHasNoExtractableRecipeContent_ReturnsNull()
    {
        string html = """
            <title>Ordinary Article</title>
            <h1>Ordinary Article</h1>
            <p>This page has no ingredient or instruction sections.</p>
            """;
        RecipeHtmlExtractor extractor = new RecipeHtmlExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.Null(recipe);
    }
}
